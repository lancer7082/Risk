using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Timers;
using NLog;

namespace Risk
{
    /// <summary>
    ///  Автоматический Margin Call
    /// </summary>
    public class AutoMarginCall
    {
        /// <summary>
        /// Лог
        /// </summary>
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Таймер
        /// </summary>
        private Timer _timer;

        /// <summary>
        /// Время последнего оповещения о MarginCall
        /// </summary>
        private static DateTime _lastAlertTime;

        /// <summary>
        /// Запуск AutoMarginCall
        /// </summary>
        /// <param name="interval"></param>
        public void Start(int interval)
        {
            if (_timer != null || interval < 0)
                return;

            _timer = new Timer(interval)
            {
                AutoReset = true
            };

            _timer.Elapsed += CheckMarginCall;
            _timer.Start();
        }

        /// <summary>
        /// AutoMarginCall
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckMarginCall(object sender, ElapsedEventArgs e)
        {
            // берем все портфели с маржинколом
            var portfolios = Server.Portfolios.Where(p => p.MarginCall).ToList();
            
            if (!portfolios.Any())
            {
                ClearAutoMarginCallsInfo();
                return;
            }

            if (Server.Settings.PlannedCapitalUtilization > Server.Settings.MaxPercentUtilMarginCall)
            {
                Log.Warn("PlannedCapitalUtilization > MaxPercentUtilMarginCall. AutoMarginCall disabled");
                return;
            }

            // и позиции по этим портфелям
            var portfoliosAccountIds = portfolios.Select(s => s.AccountId);
            var positions = Server.Positions.Where(p => portfoliosAccountIds.Contains(p.AccountId)).ToList();

            var portfolioRules = new List<PortfolioRule>();
            var autoMarginCallInfos = new List<AutoMarginCallInfo>();

            _timer.Stop();

            try
            {
                // по каждому портфелю осуществляем AutoMarginCall
                foreach (var portfolio in portfolios)
                {
                    MakeAutoMarginCall(portfolio, positions, portfolioRules, autoMarginCallInfos);
                }
            }
            catch (Exception exception)
            {
                Log.Error("MakeAutoMarginCallException: " + exception);
            }
            finally
            {
                _timer.Start();
            }

            // рассылаем уведомления о проделанных операциях
            if (portfolioRules.Count > 0)
            {
                new CommandMerge
                {
                    Object = Server.PortfolioRules,
                    Data = portfolioRules,
                    KeyFields = "TradeCode,RuleType",
                    Fields = "RuleTime",
                }.ExecuteAsync();
            }

            if (autoMarginCallInfos.Count > 0)
            {
                //Запоминаем, сколько было записей до обновления
                int count = Server.AutoMarginCallInfos.Count();

                new CommandMerge
                {
                    Object = Server.AutoMarginCallInfos,
                    Data = autoMarginCallInfos,
                }.ExecuteAsync();
                LogMarginCallInfo(autoMarginCallInfos);


                //FMD-1626 Уведомление наличии позиций к закрытию (MarginCall)
                // Отправка сообщения на клиента
                var position = autoMarginCallInfos.OrderByDescending(t => t.UpdateTime).First();
                if (position != null)
                {
                    //int alertInterval = Server.Settings.MarginCallAlertInterval <= 0 : 0 ;
                    bool canAlert = (
                            (count == 0) ||
                            (_lastAlertTime == null) ||
                            (Server.Settings.MarginCallAlertInterval <= 0) ||
                            ((DateTime.Now - _lastAlertTime).TotalSeconds >= Server.Settings.MarginCallAlertInterval)
                        ) ? true : false;

                    if (canAlert)
                    {
                        AlertType alertType;
                        string text = "";

                        if (count == 0) //Если список позиций к закрытию был пуст
                        {
                            alertType = AlertType.NewPositionInMarginCall;
                            text = String.Format("Новая позиция к закрытию в списке Margin Calls): TradeCode = {0} SecCode = {1}",
                                position.TradeCode, position.InstrumentCode);
                        }
                        else //в списке позиций к закрытию уже были записи
                        {
                            alertType = AlertType.RemindAboutPositionInMarginCall;
                            text = String.Format("Есть позиции к закрытию в списке Margin Calls): TradeCode = {0}, SecCode = {1}",
                                position.TradeCode, position.InstrumentCode);
                        }

                        var alert = new Alert
                        {
                            DateTime = DateTime.Now,
                            Text = text,
                            AlertType = alertType
                        };

                        new CommandInsert
                        {
                            Object = Server.Alerts,
                            Data = alert,
                        }.ExecuteAsync();

                        _lastAlertTime = DateTime.Now;
                    }
                }
            }
            else  // Нужно очистить таблицу, чтобы там не остались старые записи
            {
                ClearAutoMarginCallsInfo();
            }
        }



        /// <summary>
        /// 
        /// </summary>
        private static void ClearAutoMarginCallsInfo()
        {
            if (Server.AutoMarginCallInfos.Any())
                new CommandDelete
                {
                    Object = Server.AutoMarginCallInfos,
                    Data = Server.AutoMarginCallInfos.ToList()
                }.ExecuteAsync();
        }

        /// <summary>
        /// Логирование autoMarginCallInfos
        /// </summary>
        /// <param name="autoMarginCallInfos"></param>
        private static void LogMarginCallInfo(List<AutoMarginCallInfo> autoMarginCallInfos)
        {
            foreach (var autoMarginCallInfo in autoMarginCallInfos)
            {
                Log.Trace("AutoMarginCallInfo: Id:{0} TradeCode:{1} InstrumentCode:{2} InstrumentGORate:{3} UpdateTime:{4} CurrentCapital:{5:0.##} CapitalUsageOriginal:{6:0.##} PlannedCapitalUsage:{7:0.##} CapitalUsageNew:{8:0.##} QuantityPlanned:{9:0.##} CurrentQuantity:{10:0.##} QuantityForClose:{11:0.##} PositionBalance:{12} PositionBalanceNew:{13} PositionGO:{14:0.##} PositionGoNew:{15:0.##} PositionPrice:{16:0.##} PositionQuote:{17} PositionsCount:{18} OtherPositionsGOSum:{19:0.##} MarginMin:{20:0.##} Client:{21}",
                    autoMarginCallInfo.Id, autoMarginCallInfo.TradeCode, autoMarginCallInfo.InstrumentCode, autoMarginCallInfo.InstrumentGORate,
                    autoMarginCallInfo.UpdateTime, autoMarginCallInfo.CurrentCapital, autoMarginCallInfo.CapitalUsageOriginal,
                    autoMarginCallInfo.PlannedCapitalUsage, autoMarginCallInfo.CapitalUsageNew, autoMarginCallInfo.QuantityPlanned, autoMarginCallInfo.CurrentQuantity,
                    autoMarginCallInfo.QuantityForClose, autoMarginCallInfo.PositionBalance, autoMarginCallInfo.PositionBalanceNew, autoMarginCallInfo.PositionGO,
                    autoMarginCallInfo.PositionGoNew, autoMarginCallInfo.PositionPrice, autoMarginCallInfo.PositionQuote, autoMarginCallInfo.PositionsCount,
                    autoMarginCallInfo.OtherPositionsGOSum, autoMarginCallInfo.MarginMin, autoMarginCallInfo.ClientName);
            }
        }

        /// <summary>
        /// Запускает механизм автоматического маржинколлирования
        /// </summary>
        private static void MakeAutoMarginCall(Portfolio portfolio, List<Position> positionsList, List<PortfolioRule> portfolioRules, List<AutoMarginCallInfo> autoMarginCallInfos)
        {
            // получаем все позиции по портфелю
            var positions = positionsList.Where(s => s.AccountId == portfolio.AccountId).ToList();
            if (!positions.Any())
                return;

            // проверить ставки ГО по всем инструментам в портфеле
            if (!ValidateAllGORates(positions))
            {
                //3.2.	Если ставка ГО по одному из инструментов, входящих в портфель клиента, не соответствует «эталонной», 
                // то закрытие позиций не происходит, создается только оповещение для Пользователя (пишем в лог)
                // Отправка оповещения пользователю
                portfolioRules.Add(new PortfolioRule
                {
                    RuleType = RuleType.IncorrectGORate,
                    Portfolio = portfolio,
                    RuleTime = ServerBase.Current.ServerTime,
                });
                return;
            }

            // Если позиций на счете более одной, то все инструменты  ранжируются по убыванию размера «ГО позиции» в денежном выражении 
            positions = positions.OrderByDescending(p => p.GOPos).ToList();

            var ruleAdded = false;
            foreach (var position in positions)
            {
                var instrument = Server.Instruments.SingleOrDefault(s => s.SecCode == position.SecCode);
                if (instrument == null)
                    continue;

                //Для первого из списка инструмента необходимо выполнить проверку на доступность торгов, 
                //т.е. возможность исполнения заявок по данному инструменту
                if (!IsTradingAvailable(position.SecCode))
                    continue; // если инструмент не доступен, то перейти к следующему инстурменту

                var autoMarginCallInfo = new AutoMarginCallInfo
                {
                    TradeCode = position.TradeCode,
                    InstrumentCode = position.SecCode,
                    PositionGO = position.GOPos,
                    CapitalUsageOriginal = portfolio.UtilizationFact,
                    PositionsCount = positions.Count,
                    MarginMin = portfolio.MarginMin,
                    ClientName = portfolio.Client
                };

                // если портфель отрицательный, то нужно закрывать все позиции
                if (portfolio.UtilizationFact < 0)
                {
                    //CloseClientPosition(portfolio, position, portfolioRules, autoMarginCallInfo);
                    continue;
                }

                var positionPrice = position.Quote * (decimal)Math.Pow(10, instrument.Decimals) * instrument.Bpcost / 100000;

                // если доступен, то создать заявку. Количество инструмента в заявке рассчитывается по определенному алгоритму
                var quantity = CalculateInstrumentQuantity(portfolio.Capital, position, positionPrice, positions, autoMarginCallInfo);
                if (!quantity.HasValue)
                    continue;

                if (Server.Settings.AutoMarginCallEnabled)
                {
                    try
                    {
                        Log.Info("Trying to close {2} position {0} quantity {1}", position.SecCode, quantity,
                            position.TradeCode);

                        //возможные ошибки закрытия  обрабатывать не нужно, но письма не отправляем.
                        if (ClosePosition(position, quantity.Value, true))
                        {
                            if (!ruleAdded)
                            {
                                // Отправка оповещения пользователю
                                portfolioRules.Add(new PortfolioRule
                                {
                                    RuleType = RuleType.MaxPercentUtilMarginCallExceed,
                                    Portfolio = portfolio,
                                    RuleTime = ServerBase.Current.ServerTime,
                                });
                                ruleAdded = true;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Log.ErrorException("Unable to close position", e);
                    }
                }

                // приводим знаки к одному значению
                position.Balance -= (quantity.Value * Math.Sign(position.Balance));

                // расчет ГО позиции
                position.GOPos = Math.Abs(position.Balance) * positionPrice * position.GORate;
                var currentCapitalUsage = (int)((positions.Sum(p => p.GOPos) / portfolio.Capital) * 100);

                autoMarginCallInfo.PositionGoNew = position.GOPos;
                autoMarginCallInfo.CapitalUsageNew = currentCapitalUsage;
                autoMarginCallInfo.PositionBalanceNew = position.Balance;

                autoMarginCallInfos.Add(autoMarginCallInfo);

                if (currentCapitalUsage <= Server.Settings.PlannedCapitalUtilization)
                    break;
            }
        }

        /// <summary>
        /// Закрыть позицию в портфеле полностью
        /// </summary>
        /// <param name="portfolio"></param>
        /// <param name="position"></param>
        /// <param name="portfolioRules"></param>
        /// <param name="autoMarginCallInfo"></param>
        private static void CloseClientPosition(Portfolio portfolio,
            Position position,
            List<PortfolioRule> portfolioRules,
            AutoMarginCallInfo autoMarginCallInfo)
        {
            if (portfolioRules.All(s => s.Portfolio != portfolio))
            {
                // Отправка оповещения пользователю
                portfolioRules.Add(new PortfolioRule
                {
                    RuleType = RuleType.MaxPercentUtilMarginCallExceed,
                    Portfolio = portfolio,
                    RuleTime = ServerBase.Current.ServerTime,
                });

            }

            Log.Info("Closing negative client portfolio {0} {1} quantity {2}", portfolio.TradeCode, position.SecCode, Math.Abs(position.Balance));

            try
            {
                ClosePosition(position, Math.Abs(position.Balance), false);
            }
            catch (Exception e)
            {
                Log.ErrorException("Unable to close position", e);
            }

            autoMarginCallInfo.PositionBalance = position.Balance;
            autoMarginCallInfo.CurrentCapital = portfolio.Capital;
            autoMarginCallInfo.PositionQuote = position.Quote;
            LogMarginCallInfo(new List<AutoMarginCallInfo> { autoMarginCallInfo });
        }

        /// <summary>
        /// Закрыть позицию
        /// </summary>
        /// <param name="position"></param>
        /// <param name="quantity"></param>
        /// <param name="checkAutoClosePositionSetting"></param>
        private static bool ClosePosition(Position position, decimal quantity, bool checkAutoClosePositionSetting)
        {
            if (quantity == 0)
                return false;

            if (checkAutoClosePositionSetting && !Server.Settings.AutoMarginCallEnabled)
            {
                Log.Info("Automatically closing positions disabled for margin call");
                return false;
            }

            ClientOrders.CancelActiveClientOrders(position.TradeCode);

            var order = new Order
            {
                TradeCode = position.TradeCode,
                SecCode = position.SecCode,
                Date = Server.Settings.ServerTime,
                Quantity = (int)quantity,
                Price = 0,
                OrderType = position.Balance > 0 ? OrderType.Sell : OrderType.Buy,
                MarginCall = true,
            };

            ServerBase.Current.AddIns["Risk.Transaq.TransaqAddIn, Risk.Transaq"].Execute
                (
                    new Command
                    {
                        CommandText = "NewOrder",
                        Data = order
                    }
                );
            return true;
        }

        /// <summary>
        /// Вычислить количество инстурмента для закрытия позиции
        /// </summary>
        /// <param name="currentCapital">Текущий капитал</param>
        /// <param name="position">Позиции</param>
        /// <param name="positionPrice"></param>
        /// <param name="positions"></param>
        /// <param name="autoMarginCallInfo"></param>
        /// <returns></returns>
        public static int? CalculateInstrumentQuantity(decimal currentCapital,
                                                        Position position,
                                                        decimal positionPrice,
                                                        List<Position> positions,
                                                        AutoMarginCallInfo autoMarginCallInfo)
        {
            var plannedCapitalUsing = (decimal)Server.Settings.PlannedCapitalUtilization / 100;
            var instrumentGORate = position.GORate;

            // проверка деления на ноль
            if (instrumentGORate == 0 || positionPrice == 0)
            {
                Log.Error("Can't execute CalculateInstrumentQuantity instrumentGORate={0} position.Quote={1}", instrumentGORate, position.Quote);
                return null;
            }

            // не обрабатываем позиции с нулевым балансом
            if (position.Balance == 0)
                return null;

            // Кол-во_План = ((Использование_План * Текущий капитал) - сумма других ГО / Ставка ГО )) / Цена
            var otherPositionsGOSum = positions.Where(p => p.SecCode != position.SecCode).Sum(s => s.GOPos);

            var quantityPlanned = ((plannedCapitalUsing * currentCapital) - otherPositionsGOSum) / (instrumentGORate * positionPrice);

            // Если расчитанное значение - отрицательное, то "Кол-во план" = 0, т.е. закрывается вся позиция
            if (quantityPlanned < 0)
                quantityPlanned = 0;

            // Таким образом, количество в заявке на закрытие должно быть: [Кол-во_Текущее] – [Кол-во_План] 
            // округляем плановое количество в меньшую сторону, чтобы не закрыть слишком мало
            // плановое количество может быть больше количества в позиции, если маржинколл не по ней - берем по модулю

            var currentQuantity = Math.Abs(position.Balance);
            var quantityForClose = Math.Abs(currentQuantity - (int)(quantityPlanned));

            // чтобы не перевернуться нужно проверить что количество не превышает размер позиции
            if (quantityForClose > currentQuantity)
                quantityForClose = currentQuantity;

            autoMarginCallInfo.QuantityForClose = quantityForClose;
            autoMarginCallInfo.QuantityPlanned = quantityPlanned;
            autoMarginCallInfo.CurrentQuantity = currentQuantity;
            autoMarginCallInfo.OtherPositionsGOSum = otherPositionsGOSum;
            autoMarginCallInfo.PositionBalance = position.Balance;
            autoMarginCallInfo.PlannedCapitalUsage = Server.Settings.PlannedCapitalUtilization;
            autoMarginCallInfo.InstrumentGORate = instrumentGORate;
            autoMarginCallInfo.PositionQuote = position.Quote;
            autoMarginCallInfo.PositionPrice = positionPrice;
            autoMarginCallInfo.CurrentCapital = currentCapital;

            return quantityForClose;
        }

        /// <summary>
        /// Доступен ли инструмент для торгов // todo Вынести куда-нибудь
        /// </summary>
        /// <param name="instrumentCode">Код инстурмента</param>
        /// <returns></returns>
        public static bool IsTradingAvailable(string instrumentCode)
        {
            if (!Server.InstrumentsGOInfo.Any())
                return false;

            // получаем информация ГО по инструменту
            var instrumentGOInfo = Server.InstrumentsGOInfo.SingleOrDefault(s => s.SecCode == instrumentCode);
            if (instrumentGOInfo == null)
                return false;

            var currentTime = Server.Settings.ServerTime;

            if (!instrumentGOInfo.TradeDateBegin.HasValue || !instrumentGOInfo.TradeDateEnd.HasValue)
                return true;

            // проверяем расписание торгов
            if (currentTime >= instrumentGOInfo.TradeDateBegin && currentTime < instrumentGOInfo.TradeDateEnd)
            {
                if (string.IsNullOrEmpty(instrumentGOInfo.TradePeriodParams))
                    return true;

                var timeZoneOffset = instrumentGOInfo.Offset + Server.Settings.UtcOffset;
                return CheckTradePeriodParams(instrumentGOInfo.TradePeriodParams, currentTime, new TimeSpan(0, 0, timeZoneOffset, 0));
            }
            return false;
        }

        /// <summary>
        /// Проверка пауз торгов
        /// </summary>
        /// <param name="tradePeriodParams"></param>
        /// <param name="currentTime"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        private static bool CheckTradePeriodParams(string tradePeriodParams, DateTime currentTime, TimeSpan offset)
        {
            if (string.IsNullOrEmpty(tradePeriodParams))
                return true;

            try
            {
                //PAUSE:(10:00 10:05)(14:45 15:10)
                var tradePeriods = tradePeriodParams.Split(new[] { '(', ')' }).ToList();
                tradePeriods.RemoveAt(0);  // убираем PAUSE
                tradePeriods.RemoveAll(s => s == string.Empty); // и пустые строки

                // проходим по периодам
                foreach (var tradePeriod in tradePeriods)
                {
                    // разбиваем на время начала и старта
                    var tradeTimes = tradePeriod.Split(" ".ToCharArray());

                    // добавляем смещение к текущему времени чтобы привести все в один часовой пояс
                    var currentTimeWithOffset = new TimeSpan(currentTime.Date.Ticks);
                    currentTimeWithOffset = currentTimeWithOffset.Add(offset);

                    // парсим время из строки
                    var timeSpanStart = TimeSpan.ParseExact(tradeTimes[0], "hh\\:mm", CultureInfo.InvariantCulture);
                    var timeSpanEnd = TimeSpan.ParseExact(tradeTimes[1], "hh\\:mm", CultureInfo.InvariantCulture);

                    // переходим в нашу дату
                    timeSpanStart = currentTimeWithOffset.Add(timeSpanStart);
                    timeSpanEnd = currentTimeWithOffset.Add(timeSpanEnd);

                    // конвертируем в DateTime
                    var timeStart = new DateTime(timeSpanStart.Ticks);
                    var timeEnd = new DateTime(timeSpanEnd.Ticks);

                    // сравниваем
                    if (currentTime >= timeStart && currentTime <= timeEnd)
                        return false;
                }
            }
            catch (Exception e)
            {
                Log.ErrorException("Unable to parse trade pause parameters", e);
                return true;
            }
            return true;
        }

        /// <summary>
        /// Проверить для каждой позиции ставку ГО на валидность
        /// </summary>
        /// <param name="positions">Список позиций</param>
        /// <returns>false если хотя бы у одной позиции ставка ГО не является валидной</returns>
        private static bool ValidateAllGORates(IEnumerable<Position> positions)
        {
            if (positions == null)
                return false;

            foreach (var position in positions)
            {
                // получаем информацию ГО для выбранного инструмента
                var instrumentGOInfo = Server.InstrumentsGOInfo.SingleOrDefault(s => s.SecCode == position.SecCode);
                if (instrumentGOInfo == null)
                    return false;

                decimal ethalonGORate;

                //Если расписание для рынка или тикера не указано или отсутствует в таблице, то это означает, 
                //что ставка ГО не обновляется вообще, т.е. всегда одинаковая (круглосуточно).
                if (!instrumentGOInfo.TimeDay.HasValue || !instrumentGOInfo.TimeNight.HasValue)
                    ethalonGORate = instrumentGOInfo.GORateNight;
                else
                {
                    // выбираем дневную или ночную ГО
                    var currentTime = Server.Settings.ServerTime;
                    if (currentTime.TimeOfDay >= instrumentGOInfo.TimeDay && currentTime.TimeOfDay < instrumentGOInfo.TimeNight)
                        ethalonGORate = instrumentGOInfo.GORateDay;
                    else
                        ethalonGORate = instrumentGOInfo.GORateNight;
                }

                if (position.InstrumentGORate != ethalonGORate)// && IsTradingAvailable(position.SecCode))
                    return false;
            }
            return true;
        }
    }
}
