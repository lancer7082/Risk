using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using NLog;

namespace Risk
{
    class CheckInstrumentsQuotes
    {
        /// <summary>
        /// Лог
        /// </summary>
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Компаратор котировок
        /// </summary>
        private readonly IEqualityComparer<CheckInstrumentQuotesResult> _quotesComparer = new QuotesComparer();

        /// <summary>
        /// Кэш котировок по инструментам
        /// </summary>
        private static readonly Dictionary<string, List<CheckInstrumentQuotesResult>> QuotesCache
                                = new Dictionary<string, List<CheckInstrumentQuotesResult>>();

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

            _timer = new Timer(Server.Settings.CheckingQuotesPeriod * 1000)
            {
                AutoReset = true
            };

            _timer.Elapsed += CheckQuotes;
            _timer.Start();
        }

        /// <summary>
        /// Проверка котировок
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckQuotes(object sender, ElapsedEventArgs e)
        {
            var portfolio = Server.Portfolios.FirstOrDefault(p => p.Contragent);

            if (portfolio == null)
                return;

            var alerts = new List<Alert>();

            if (string.IsNullOrEmpty(Server.Settings.CheckingQuotesInstruments))
                return;

            var instruments = Server.Settings.CheckingQuotesInstruments.Split(";".ToCharArray());

            if (instruments.Length == 0)
                return;

            _timer.Stop();
            _timer.Interval = Server.Settings.CheckingQuotesPeriod * 1000; // обновляем период на тот случай, если он изменился

            // проходим по всем инструментам из настройки и проверяем его котировки
            for (var i = 0; i < instruments.Length; i++)
            {
                try
                {
                    var instrumentCode = instruments[i];

                    // инструмент должен быть активен для торгов
                    if (!AutoMarginCall.IsTradingAvailable(instrumentCode))
                        continue;

                    //проверка котировок инструмента
                    CheckInstrumentQuotes(instrumentCode, portfolio.TradeCode, alerts);
                }
                catch  // там всякое бывает при старте обработки, пока все данные не получены
                {
                }
            }

            _timer.Start();

            // рассылаем уведомления о проделанных операциях
            if (alerts.Count > 0)
            {
                new CommandInsert
                {
                    Object = Server.Alerts,
                    Data = alerts,
                }.ExecuteAsync();
            }
        }

        /// <summary>
        /// Проверка котировок по инструменту
        /// </summary>
        /// <param name="instrumentCode"></param>
        /// <param name="tradeCode"></param>
        /// <param name="alerts"></param>
        private void CheckInstrumentQuotes(string instrumentCode, string tradeCode, List<Alert> alerts)
        {
            // передаем запрос в транзак
            var quotes = ServerBase.Current.AddIns["Risk.Transaq.TransaqAddIn, Risk.Transaq"].Execute
                (
                    new Command
                    {
                        CommandText = "CheckInstrumentQuotes",
                        Data = new CheckInstrumentQuotesParameters
                        {
                            InstrumentCode = instrumentCode,
                            TradeCode = tradeCode
                        }
                    }
                ) as List<CheckInstrumentQuotesResult>;

            if (quotes == null || !quotes.Any())
                return;

            // если котировки пришли впервые - просто добавляем в словарь
            if (!QuotesCache.ContainsKey(instrumentCode))
                QuotesCache.Add(instrumentCode, quotes);
            else
            {
                // проверяем изменение котировок - если не поменялись, то добавляем алерт
                if (!NewQuotesExists(QuotesCache[instrumentCode], quotes))
                    alerts.Add(new Alert
                    {
                        DateTime = Server.Settings.ServerTime,
                        PortfolioRule = new PortfolioRule { RuleType = RuleType.NoInstrumentsQuotes },
                        Text = String.Format("Рыночные данные по инструменту {0} не получены", instrumentCode),
                    });

                // заменяем предыдущие значения в кэше
                QuotesCache[instrumentCode] = quotes;
            }
        }

        /// <summary>
        /// Проверка наличия новых котировок
        /// </summary>
        /// <param name="previousQuotes"></param>
        /// <param name="newQuotes"></param>
        /// <returns></returns>
        private bool NewQuotesExists(List<CheckInstrumentQuotesResult> previousQuotes, List<CheckInstrumentQuotesResult> newQuotes)
        {
            // смотрим есть ли в новых котировках обновленные значения
            var except = newQuotes.Except(previousQuotes, _quotesComparer).ToList();
            return except.Any();
        }

        /// <summary>
        /// Класс компаратор котировок инструмента
        /// </summary>
        private class QuotesComparer : IEqualityComparer<CheckInstrumentQuotesResult>
        {
            #region Implementation of IEqualityComparer<in CheckInstrumentQuotesResult>

            /// <summary>
            /// Determines whether the specified objects are equal.
            /// </summary>
            /// <returns>
            /// true if the specified objects are equal; otherwise, false.
            /// </returns>
            /// <param name="x">The first object of type <paramref name="T"/> to compare.</param><param name="y">The second object of type <paramref name="T"/> to compare.</param>
            public bool Equals(CheckInstrumentQuotesResult x, CheckInstrumentQuotesResult y)
            {
                if (x == null || y == null)
                    return false;

                return x.Price == y.Price && x.QuantityBid == y.QuantityBid && x.QuantitySell == y.QuantitySell;
            }

            /// <summary>
            /// Returns a hash code for the specified object.
            /// </summary>
            /// <returns>
            /// A hash code for the specified object.
            /// </returns>
            /// <param name="obj">The <see cref="T:System.Object"/> for which a hash code is to be returned.</param><exception cref="T:System.ArgumentNullException">The type of <paramref name="obj"/> is a reference type and <paramref name="obj"/> is null.</exception>
            public int GetHashCode(CheckInstrumentQuotesResult obj)
            {
                return obj.Price.GetHashCode() + obj.QuantityBid.GetHashCode() + obj.QuantitySell.GetHashCode();
            }

            #endregion
        }
    }
}
