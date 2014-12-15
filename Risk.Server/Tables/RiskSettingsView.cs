using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Risk
{
    /// <summary>
    /// Опции
    /// </summary>
    [DataObject("Settings")]
    public class RiskSettingsView : DataObjectView<RiskSettings>
    {
        public RiskSettingsView()
            : base(Server.Settings)
        {
        }

        public override void SetData(ParameterCollection parameters, object data)
        {
            // сохранили прежнюю валюту
            var oldCurrencyCalc = Server.Settings.CurrencyCalc;
            var oldCurrencyDisplay = Server.Settings.CurrencyDisplay;

            // применили новые настройки
            base.SetData(parameters, data);
            Server.Current.DataBase.WriteSettings(Data);

            var currencyCalcSettingChanged = CheckForNewCurrencySettings(parameters, data, oldCurrencyCalc, "CurrencyCalc");
            var currencyDisplaySettingChanged = CheckForNewCurrencySettings(parameters, data, oldCurrencyDisplay, "CurrencyDisplay");
            
            // проверка наличия новой валюты и перезапуск джоба
            if (currencyCalcSettingChanged || currencyDisplaySettingChanged)
            {
                // пересчет конвертируемых полей
                UpdateConvertibleFields();

                // если изменяется валюта отображения, то нужно перегрузить финрез  в валюте отображения
                if (currencyDisplaySettingChanged)
                    ServerBase.Current.JobManager.RestartJob("UpdateFinancialResultsJob");
            }
            RecheckPortfolioRules();
            ClearMarginCallsInfo();
        }

        /// <summary>
        /// Очистка AutoMarginCallInfos
        /// </summary>
        private void ClearMarginCallsInfo()
        {
            if (Server.AutoMarginCallInfos.Any()) // Нужно очистить таблицу, чтобы там не остались старые записи
            {
                new CommandDelete
                {
                    Object = Server.AutoMarginCallInfos,
                    Data = Server.AutoMarginCallInfos.ToList()
                }.ExecuteAsync();
            }
        }

        /// <summary>
        /// Пересчитать правила для портфелей
        /// </summary>
        private void RecheckPortfolioRules()
        {
            var portfoliosItems = Server.Portfolios;
            Portfolios.CheckRules(portfoliosItems);

            // формируем обновляемые поля портфелей
            var portfoliosFields = portfoliosItems.Select(s => new Portfolio
            {
                TradeCode = s.TradeCode,
                IsMaxPercentProfitExceed = s.IsMaxPercentProfitExceed,
                IsMaxPercentTurnoverExceed = s.IsMaxPercentTurnoverExceed,
                IsMaxProfitExceed = s.IsMaxProfitExceed,
                IsMaxTurnoverExceed = s.IsMaxTurnoverExceed,
                MarginCall = s.MarginCall
            });

            ServerBase.Current.Execute(Command.Update("Portfolios", portfoliosFields,
                                        "IsMaxPercentProfitExceed,IsMaxPercentTurnoverExceed,IsMaxProfitExceed,IsMaxTurnoverExceed,MarginCall"));
        }

        /// <summary>
        /// Проверка применения в настройки новой валюты
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="data"></param>
        /// <param name="oldCurrency"></param>
        /// <param name="fieldName"></param>
        private bool CheckForNewCurrencySettings(ParameterCollection parameters, object data, string oldCurrency, string fieldName)
        {
            if (parameters == null)
                return false;

            //получаем объектное представление параметра Fields и извлекаем значение поля CurrencyCalc
            var properties = GetProperties((string)parameters["Fields"]);
            if (properties == null)
                return false;

            var currencyProperty = properties.FirstOrDefault(s => s.Name == fieldName);
            if (currencyProperty == null)
                return false;

            // сравниваем новое и старое значение настройки CurrencyCalc
            var newCurrency = currencyProperty.GetValue(data) as string;

            if (string.IsNullOrEmpty(newCurrency))
                return false;

            return newCurrency != oldCurrency;
        }

        /// <summary>
        /// Обновляет поля, значения которых конвертируются в валюту отображения и расчетов
        /// </summary>
        private static void UpdateConvertibleFields()
        {
            Positions.ApplyRates(Server.ExchangeRates, Server.Portfolios, Server.Positions);

            // формируем обновляемые поля позиций
            var positionsFields = Server.Positions.Select(s => new Position
            {
                PLCurrencyCalc = s.PLCurrencyCalc,
                PLCurrencyDisplay = s.PLCurrencyDisplay,
                TurnoverCurrencyCalc = s.TurnoverCurrencyCalc,
                TurnoverCurrencyDisplay = s.TurnoverCurrencyDisplay,
                SecCode = s.SecCode,
                TradeCode = s.TradeCode
            }).ToList();

            Portfolios.ApplyRates(Server.ExchangeRates, Server.Portfolios);

            // формируем обновляемые поля портфелей
            var portfoliosFields = Server.Portfolios.Select(s => new Portfolio
            {
                TradeCode = s.TradeCode,
                PLCurrencyCalc = s.PLCurrencyCalc,
                PLCurrencyDisplay = s.PLCurrencyDisplay,
                TurnoverCurrencyCalc = s.TurnoverCurrencyCalc,
                TurnoverCurrencyDisplay = s.TurnoverCurrencyDisplay,
                CapitalCurrencyCalc = s.CapitalCurrencyCalc,
                CapitalCurrencyDisplay = s.CapitalCurrencyDisplay
            }).ToList();

            // отправляем команды обновления позиций и портфелей
            ServerBase.Current.Execute(Command.Update("Positions", positionsFields,
                                        "PLCurrencyCalc,PLCurrencyDisplay,TurnoverCurrencyCalc,TurnoverCurrencyDisplay"));

            ServerBase.Current.Execute(Command.Update("Portfolios", portfoliosFields,
                                        "PLCurrencyCalc,PLCurrencyDisplay,TurnoverCurrencyCalc,TurnoverCurrencyDisplay,CapitalCurrencyCalc,CapitalCurrencyDisplay"));
        }
    }
}