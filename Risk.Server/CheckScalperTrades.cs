using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Web.Services.Description;
using NLog;

namespace Risk
{
    class CheckScalperTrades
    {
        /// <summary>
        /// Лог
        /// </summary>
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static long LastTradeNumber = -1;

        /// <summary>
        /// Таймер
        /// </summary>
        private Timer _timer;

        /// <summary>
        /// Запуск проверки котировок
        /// </summary>
        public void Start()
        {
            if (_timer != null)
                return;



            _timer = new Timer(Server.Settings.CheckingScalperTradesPeriod * 1000)
            {
                AutoReset = true
            };

            _timer.Elapsed += CheckTrades;
            _timer.Start();
        }

        private void InitSclapersHistory()
        {
            if (!Server.Portfolios.Any())
                return;

            var trades = Server.Current.DataBase.LoadTrades(DateTime.Parse("1900-01-01"), DateTime.Now).ToList();
          //  trades.RemoveAll(s => s.TradeCode != "MCE1026"); //(s => s.TradeCode == "MAA1245");
            var trades2 = GetProcessingTrades(trades);
            var orders = GetScalperTrades(trades2, new List<PortfolioRule>());
        }

        /// <summary>
        /// Проверка котировок
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckTrades(object sender, ElapsedEventArgs e)
        {
            _timer.Stop();
            _timer.Interval = Server.Settings.CheckingScalperTradesPeriod * 1000; // обновляем период на тот случай, если он изменился

            try
            {
                //InitSclapersHistory();
                var portfolioRules = new List<PortfolioRule>();
                var trades = GetProcessingTrades(Server.Trades.ToList());
                if (trades.Any())
                {
                    var orders = GetScalperTrades(trades, portfolioRules);
                    trades.ForEach(t =>
                    {
                        if (orders.Contains(t.OrderNo))
                        {
                            t.IsScalper = true;
                        }
                    });
                    trades.RemoveAll(s => !s.IsScalper);
                    if (trades.Any())
                        new CommandUpdate
                        {
                            Object = Server.Trades,
                            Data = trades,
                            Fields = "IsScalper",
                        }.ExecuteAsync();
                }
                if (portfolioRules.Any())
                {
                    // рассылаем уведомления о проделанных операциях
                    new CommandMerge
                    {
                        Object = Server.PortfolioRules,
                        Data = portfolioRules,
                        KeyFields = "TradeCode,RuleType",
                        Fields = "RuleTime,Portfolio",
                    }.ExecuteAsync();
                }
            }
            catch // там всякое бывает при старте обработки, пока все данные не получены
            {
            }
            finally
            {
                _timer.Start();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="trades"></param>
        /// <param name="rules"></param>
        private List<long> GetScalperTrades(List<Trade> trades, List<PortfolioRule> rules)
        {
            var result = new List<long>();
            var result2 = new List<string>();
            var groupedTrades = trades.GroupBy(s => s.TradeCode);  // группировка по трейдеру
            foreach (var groupedTrade in groupedTrades)
            {
                // внутри каждого трейдера группируем по инстурменту
                var groupedSecCode = groupedTrade.GroupBy(s => s.SecCode).ToList();
                if (groupedSecCode.Count() < 2)
                    continue;

                // проходим по каждой группе инструментов
                foreach (var groupSecCode in groupedSecCode)
                {
                    if (groupSecCode.All(s => s.Sell) || groupSecCode.All(s => !s.Sell))
                        continue;

                    var orders = groupSecCode.GroupBy(s => s.OrderNo);

                    var sclaperTrades = new List<Trade>();

                    foreach (var order in orders)
                    {
                        var trade1 = order.OrderBy(s => s.TradeTime).First();
                        sclaperTrades.Add(trade1);
                    }

                    var sellTrades = sclaperTrades.Where(s => s.Sell).ToList();
                    var buyTrades = sclaperTrades.Where(s => !s.Sell).ToList();

                    foreach (var sellTrade in sellTrades)
                    {
                        foreach (var buyTrade in buyTrades)
                        {
                            if (sellTrade.IsScalper && buyTrade.IsScalper)
                                continue;

                            if (Math.Abs((sellTrade.TradeTime - buyTrade.TradeTime).TotalSeconds) < Server.Settings.ScalperTradesDetectionInterval)
                            {
                                // добавить в список скальперскоих сделок, отправить оповещение
                                result.Add(sellTrade.OrderNo);
                                result.Add(buyTrade.OrderNo);
                                var res = ServerBase.Current.DataBase.SaveScalperData(new ScalpersData
                                {
                                    AccountId = sellTrade.AccountId,
                                    TradeCode = sellTrade.TradeCode,
                                    InstrumentCode = sellTrade.SecCode,
                                    BuyOrderId = buyTrade.OrderNo,
                                    SellOrderId = sellTrade.OrderNo,
                                    UpdateDate = sellTrade.TradeTime
                                });
                                if (res >= 0 && rules.All(s => s.Portfolio.TradeCode != sellTrade.TradeCode))
                                {
                                    rules.Add(new PortfolioRule
                                    {
                                        AlwaysSend = true,
                                        RuleType = RuleType.ScalperTrade,
                                        Portfolio = Server.Portfolios.FirstOrDefault(s => s.TradeCode == sellTrade.TradeCode),
                                        RuleTime = ServerBase.Current.ServerTime
                                    });
                                }
                              //  break;
                            }
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private List<Trade> GetProcessingTrades(List<Trade> trades)
        {
            var result = new List<Trade>();


            if (!trades.Any())
                return result;

            // •	Определить тип сделки. Сделка рефандинговая, если выполняется одно из условий:
            //o	Если refund_level инструмента = 2;
            //o	Если refund_level инструмента = 1 «И» для клиента включен refund (=1).

            var instruments = Server.Instruments.Where(s => s.RefundLevel == 2 || s.RefundLevel == 1).ToList();
            var portfolios = Server.Portfolios.Where(s => s.IsRefund).ToList();
            foreach (var trade in trades)
            {
                var instrument = instruments.FirstOrDefault(s => s.SecCode == trade.SecCode);
                var trader = portfolios.FirstOrDefault(s => s.TradeCode == trade.TradeCode);
                if (instrument == null)
                {
                    result.Add(trade);
                    continue;
                }
                if (instrument.RefundLevel == 2 || trader != null)
                {
                    continue;
                }
                result.Add(trade);
            }
            return result.OrderBy(t => t.TradeTime).ToList();
        }
    }
}
