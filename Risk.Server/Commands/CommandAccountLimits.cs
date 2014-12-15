using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Description;
using NLog;

namespace Risk.Commands
{
    /// <summary>
    /// Команда Лимиты по счетам перекрытия:
    /// •	Данные обновляются по таймеры (15 минут) и по запросу
    /// </summary>
    [Command("GetAccountsLimits")]
    public class CommandGetAccountsLimits : CommandServer
    {
        #region Overrides of CommandServer

        protected internal override void InternalExecute()
        {
            GetAccountsLimits();
        }

        #endregion

        /// <summary>
        /// AutoMarginCallClose
        /// </summary>
        private void GetAccountsLimits()
        {
            try
            {
                var limits = new List<AccountLimit>();
                limits.Add(GetMicexLimits());
                limits.Add(GetFortsLimits());
                limits.Add(GetUSALimits());

                new CommandMerge
                {
                    Object = Server.AccountsLimits,
                    Data = limits,

                }.ExecuteAsync();
            }
            catch (Exception e)
            {
                LogManager.GetCurrentClassLogger().Error("GetAccountsLimits exception: " + e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private AccountLimit GetUSALimits()
        {
            var serviceName = "TransaqAPI";
            var targetTradeCode = "US00120";
            var hostName = (Server.Current as Server).TransaqUsaHostName;

            var endpoint = new ServiceEndpoint(ContractDescription.GetContract(typeof(IServiceTransaq)),
                               new BasicHttpBinding(BasicHttpSecurityMode.None),
                               new EndpointAddress(new Uri(new Uri(hostName), serviceName)));
            var client = new ChannelFactory<IServiceTransaq>(endpoint).CreateChannel();
            var portfolios = client.GetPortfolioLimits(targetTradeCode);

            return new AccountLimit
            {
                Market = "USA",
                Currency = "RUR",
                DateTime = DateTime.Now,
                GO = portfolios.MoneyInUse,
                Limit = portfolios.Limit,
                Free = portfolios.FreeMoney
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private AccountLimit GetFortsLimits()
        {
            /*
             •	Оценка \ ГО  = Сумма по полю GoPos для всех позиций (записей) из процедуры для сверки (РИСК ЗАО). 
                    •	Лимит всего = константа на сумму 20 000 000 RUB. 
                    •	Свободные средства = [Лимит всего] – [Средства \ ГО]
            o	Сумма ГО не может быть отрицательной в любом случае.

             */
            // вызываем хранимку
            var externalPositions = ServerBase.Current.DataBase.GetFORTSPositionsSummary();

            // хранимка возвращает два датасета. Первый читаем, второй не нужен
            var result = externalPositions.GetResult<FORTSPosition>().ToList();

            return new AccountLimit
            {
                Market = "FORTS",
                Currency = "RUR",
                DateTime = DateTime.Now,
                GO = result.Sum(s => s.GoPos),
                Limit = 20000000,
                Free = 20000000 - result.Sum(s => s.GoPos)
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private static AccountLimit GetMicexLimits()
        {
            /*
             2.	ММВБ:
            •	Оценка \ ГО = Поле «Средства текущие» (I_Means) в первом датасете из процедуры для сверки рефандинга (РИСК RMM). 
            •	Лимит всего = константа (15 000 000 RUB * 50 плечо = 750 000 000 RUB). 
            •	Свободные средства = [Лимит всего] + [Средства \ ГО]
            o	Средства на счете могут быть отрицательными, т.к. счет без вводов ДС.

             */
            // вызываем хранимку
            var externalPositions = ServerBase.Current.DataBase.GetRRMPositionsSummary();

            // хранимка возвращает два датасета. Первый читаем только для того, чтобы добраться до второго
            var data = externalPositions.GetResult<MicexData>().FirstOrDefault();

            return new AccountLimit
            {
                Market = "MICEX",
                Currency = "RUR",
                DateTime = DateTime.Now,
                GO = data.I_Means,
                Limit = 750000000,
                Free = data.I_Means + 750000000
            };
        }

        /// <summary>
        /// 
        /// </summary>
        private class MicexData
        {
            public decimal I_Means { get; set; }
        }

        [ServiceContract(Name = "TransaqAPI", Namespace = "")]
        public interface IServiceTransaq
        {
            [OperationContract(Action = "GetBalanceMMA", Name = "GetBalanceMMA")]
            List<Portfolio> GetBalanceMMA(string TradeCodes, bool ErrorIfNotExist = false);

            [OperationContract(Action = "GetBalanceSpot", Name = "GetBalanceSpot")]
            List<Portfolio> GetBalanceSpot(string TradeCodes, bool ErrorIfNotExist = false);

            [OperationContract(Action = "GetBalanceFut", Name = "GetBalanceFut")]
            List<Portfolio> GetBalanceFut(string TradeCodes, bool ErrorIfNotExist = false);

            [OperationContract(Action = "GetPortfolioLimits", Name = "GetPortfolioLimits")]
            PortfolioLimits GetPortfolioLimits(string TradeCodes, bool ErrorIfNotExist = false);
        }

        [DataContract(Namespace = "")]
        public class Portfolio
        {
            [DataMember]
            public string TradeCode { get; set; }

            [DataMember]
            public decimal Balance { get; set; }

            [DataMember]
            public decimal Equity { get; set; }
        }

        [DataContract(Namespace = "")]
        public class PortfolioLimits
        {
            [DataMember]
            public decimal MoneyInUse { get; set; }

            [DataMember]
            public decimal Limit { get; set; }

            [DataMember]
            public decimal FreeMoney { get; set; }
        }
    }
}
