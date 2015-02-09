using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace Risk
{
    /// <summary>
    /// Мониторинг внутредневных вводов/выводов
    /// </summary>
    class IODailyMonitoring
    {
        /// <summary>
        /// Таймер
        /// </summary>
        private Timer _timer;

        /// <summary>
        /// Кэш портфелей
        /// </summary>
        private readonly List<Portfolio> _cachedPortfolios = new List<Portfolio>();

        private static Dictionary<string, decimal> CurrenciesCache = new Dictionary<string, decimal>();

        private static string cachedCurrencyCalc = string.Empty;

        private static decimal cachedIOValue = 0;

        /// <summary>
        /// Запуск проверки котировок
        /// </summary>
        public void Start()
        {
            if (_timer != null)
                return;

            _timer = new Timer(Server.Settings.IODailyMonitoringPeriod * 1000)
            {
                AutoReset = true
            };

            _timer.Elapsed += MonitorIO;
            _timer.Start();
        }

        /// <summary>
        /// Метод мониторинга вводов/выводов
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MonitorIO(object sender, ElapsedEventArgs e)
        {
            _timer.Stop();
            _timer.Interval = Server.Settings.IODailyMonitoringPeriod * 1000; // обновляем период на тот случай, если он изменился

            try
            {
                if (Server.Portfolios.All(s => s.Capital == 0) ||
                    Server.Portfolios.All(s => s.OpenBalanceBackOffice == 0))
                    return;

                if (!Server.ExchangeRates.Any())
                    return;

                CheckCurrencyCalc();

                // берем все портфели с ненулевым вводом/выводом
                var portfolios = Server.Portfolios.Where(p => p.MoneyInDay > 0 || p.MoneyOutDay > 0).ToList();
                if (!portfolios.Any())
                    return;

                var portfolioRules = new List<PortfolioRule>();

                // идем по списку портфелей
                foreach (var portfolio in portfolios)
                {
                    // проверяем можно ли слать оповещение по портфелю
                    if (!CanProcessPortfolio(portfolio))
                        continue;

                    // производим анализ состояния портфеля после ввода/вывода
                    // если [остаток на Т-1] + [вводы на текущую дату] > пороговой суммы => создается алерт:
                    // Исключение: если [остаток на Т-1] > пороговой суммы => алерт не создается
                    // если [остаток на Т-1] - [выводы на текущую дату] < пороговой суммы => создается алерт:
                    // Исключение: если [остаток на Т-1] < пороговой суммы => алерт не создается

                    if (portfolio.OpenBalanceBackOffice < CurrenciesCache[portfolio.Currency]
                        && portfolio.OpenBalanceBackOffice + portfolio.MoneyInDay > CurrenciesCache[portfolio.Currency]
                        && portfolio.MoneyInDay > 0)
                    {
                        AddPortfolioRule(portfolioRules, portfolio);
                        continue;
                    }
                    if (portfolio.OpenBalanceBackOffice > CurrenciesCache[portfolio.Currency]
                        && portfolio.OpenBalanceBackOffice - portfolio.MoneyOutDay < CurrenciesCache[portfolio.Currency]
                        && portfolio.MoneyOutDay > 0)
                    {
                        AddPortfolioRule(portfolioRules, portfolio);
                        continue;
                    }
                }

                if (!portfolioRules.Any())
                    return;

                // рассылаем уведомления
                NotifyRecipients(portfolioRules);
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
        private void CheckCurrencyCalc()
        {
            if (cachedCurrencyCalc != Server.Settings.CurrencyCalc || cachedIOValue != Server.Settings.IODailyMonitoringCapitalThreshold)
            {
                cachedCurrencyCalc = Server.Settings.CurrencyCalc;
                cachedIOValue = Server.Settings.IODailyMonitoringCapitalThreshold;
                _cachedPortfolios.Clear();
            }

            CurrenciesCache.Clear();
            CurrenciesCache.Add("RUB", cachedIOValue
                * Server.ExchangeRates.SingleOrDefault(s => s.CurrencyFrom == cachedCurrencyCalc && s.CurrencyTo == "RUB").Value);
            CurrenciesCache.Add("EUR", cachedIOValue
                * Server.ExchangeRates.SingleOrDefault(s => s.CurrencyFrom == cachedCurrencyCalc && s.CurrencyTo == "EUR").Value);
            CurrenciesCache.Add("USD", cachedIOValue
                * Server.ExchangeRates.SingleOrDefault(s => s.CurrencyFrom == cachedCurrencyCalc && s.CurrencyTo == "USD").Value);
        }

        /// <summary>
        /// Проверяет портфель, возвращает тру если можно отправлять оповещения по портфелю
        /// </summary>
        /// <param name="portfolio"></param>
        /// <returns></returns>
        private bool CanProcessPortfolio(Portfolio portfolio)
        {
            // юерем кэшированный портфель
            var cachedPortfolio = _cachedPortfolios.SingleOrDefault(s => s.TradeCode == portfolio.TradeCode);
            if (cachedPortfolio == null)
            {
                // если его еще нет, то можно работать
                _cachedPortfolios.Add(portfolio);
                return true;
            }

            // если портфель есть и обновились значения ввода/вывода, то можно работать с портфелем
            if (cachedPortfolio.MoneyInDay != portfolio.MoneyInDay ||
                cachedPortfolio.MoneyOutDay != portfolio.MoneyOutDay)
            {
                // перекэшируем портфель с обновленными значениями
                _cachedPortfolios.Remove(cachedPortfolio);
                _cachedPortfolios.Add(portfolio);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Добавляет правило
        /// </summary>
        /// <param name="portfolioRules"></param>
        /// <param name="portfolio"></param>
        private void AddPortfolioRule(List<PortfolioRule> portfolioRules, Portfolio portfolio)
        {
            portfolioRules.Add(new PortfolioRule
            {
                RuleType = RuleType.IODailyMonitoring,
                Portfolio = portfolio,
                RuleTime = ServerBase.Current.ServerTime,
                AlwaysSend = true
            });
        }

        /// <summary>
        /// Уведомляет получателей сообщения
        /// </summary>
        /// <param name="portfolioRules"></param>
        private void NotifyRecipients(List<PortfolioRule> portfolioRules)
        {
            new CommandMerge
            {
                Object = Server.PortfolioRules,
                Data = portfolioRules,
                KeyFields = "TradeCode,RuleType",
                Fields = "RuleTime,Portfolio",
            }.ExecuteAsync();
        }
    }
}
