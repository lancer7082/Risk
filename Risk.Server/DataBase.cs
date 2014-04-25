using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Text;
using NLog;

namespace Risk
{
    /// <summary>
    /// Набор процедур для работы с БД
    /// </summary>
    public class DataBase : DataContext
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        public byte FirmId { get; private set; }

        public DataBase(string connection, byte firmId)
            : base(connection)
        {
            this.FirmId = firmId;
        }

        #region StoredProcedures
        [Function(Name = "[Risk].[Clients]")]
        private IEnumerable<Client> GetClients([Parameter(Name = "firmId", DbType = "TinyInt")]byte firmId)
        {
            IExecuteResult result = this.ExecuteMethodCall(this, ((MethodInfo)(MethodInfo.GetCurrentMethod())), firmId);
            return ((IEnumerable<Client>)(result.ReturnValue));
        }

        /// <summary>
        /// Получение счетов ММА из БД
        /// </summary>
        /// <param name="firmId"></param>
        /// <returns></returns>
        [Function(Name = "[Risk].[Portfolios]")]
        private IEnumerable<Portfolio> GetPortfolios([Parameter(Name = "firmId", DbType = "TinyInt")]byte firmId)
        {
            IExecuteResult result = this.ExecuteMethodCall(this, ((MethodInfo)(MethodInfo.GetCurrentMethod())), firmId);
            return ((IEnumerable<Portfolio>)(result.ReturnValue));
        }

        [Function(Name = "[Risk].[GetSessionInitialData]")]
        private IEnumerable<SessionInitialData> GetSessionInitialData([Parameter(Name = "firmId", DbType = "TinyInt")]byte firmId)
        {
            IExecuteResult result = this.ExecuteMethodCall(this, ((MethodInfo)(MethodInfo.GetCurrentMethod())), firmId);
            return ((IEnumerable<SessionInitialData>)(result.ReturnValue));
        }

        /// <summary>
        /// Получение вводов/выводов в пределах текущей сессии
        /// </summary>
        /// <param name="firmId"></param>
        /// <returns></returns>
        [Function(Name = "[Risk].[MoneyInOutDay]")]
        private IEnumerable<MoneyInOut> GetMoneyInOutDay([Parameter(Name = "firmId", DbType = "TinyInt")]byte firmId)
        {
            IExecuteResult result = this.ExecuteMethodCall(this, ((MethodInfo)(MethodInfo.GetCurrentMethod())), firmId);
            return ((IEnumerable<MoneyInOut>)(result.ReturnValue));
        }

        /// <summary>
        /// Получение курсов на начало сессии из БД
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        [Function(Name = "[Risk].[Rates]")]
        public IEnumerable<Rate> GetRates([Parameter(Name = "Date", DbType = "DATE")]DateTime date)
        {
            IExecuteResult result = this.ExecuteMethodCall(this, ((MethodInfo)(MethodInfo.GetCurrentMethod())), date);
            return ((IEnumerable<Rate>)(result.ReturnValue));
        }

        /// <summary>
        /// Получение данных по инструментам на начало сессии из БД
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        [Function(Name = "[Risk].[Instruments]")]
        public IEnumerable<Instrument> GetInstruments([Parameter(Name = "Date", DbType = "DATE")]DateTime date)
        {
            IExecuteResult result = this.ExecuteMethodCall(this, ((MethodInfo)(MethodInfo.GetCurrentMethod())), date);
            return ((IEnumerable<Instrument>)(result.ReturnValue));
        }

        /// <summary>
        /// Сохранение настроек в БД
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        [Function(Name = "[Risk].[Settings::Write]")]
        private int WriteSettings([Parameter(Name = "Settings", DbType = "VARCHAR(8000)")]string settings)
        {
            IExecuteResult result = this.ExecuteMethodCall(this, ((MethodInfo)(MethodInfo.GetCurrentMethod())), settings);
            return (int)result.ReturnValue;
        }

        /// <summary>
        /// Чтение настроек из БД
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        [Function(Name = "[Risk].[Settings::Read]")]
        private int ReadSettings([Parameter(Name = "Settings", DbType = "VARCHAR(8000)")]ref string settings)
        {
            IExecuteResult result = this.ExecuteMethodCall(this, ((MethodInfo)(MethodInfo.GetCurrentMethod())), settings);
            settings = (string)(result.GetParameterValue(0));
            return (int)result.ReturnValue;
        }

        /// <summary>
        /// Отправка уведомления клиенту в терминал Transaq
        /// </summary>
        [Function(Name = "[Risk].[Alert::Send]")]
        private int NotifyClientTransaq(
            [Parameter(Name = "nnFirm",     DbType = "TinyInt")]    byte firmId,
            [Parameter(Name = "TradeCode",  DbType = "VARCHAR(30)")]string TradeCode,
            [Parameter(Name = "Login",      DbType = "VARCHAR(30)")]string Login,
            [Parameter(Name = "Message",    DbType = "VARCHAR(8000)")]string Message)
        {
            IExecuteResult result = this.ExecuteMethodCall(this, ((MethodInfo)(MethodInfo.GetCurrentMethod())), firmId, TradeCode, Login, Message);
            return (int)(result.ReturnValue);
        }

        /// <summary>
        /// Проверка получения рыночных данных из Transaq
        /// по выбранным инструментам
        /// </summary>
        [Function(Name = "[Risk].[CheckTransaqPrices]")]
        public int CheckTransaqPrices(
            [Parameter(Name = "Instruments",DbType = "VARCHAR(8000)")]ref string instruments,
            [Parameter(Name = "Status",     DbType = "TINYINT")]ref byte status)
        {
            IExecuteResult result = this.ExecuteMethodCall(this, ((MethodInfo)(MethodInfo.GetCurrentMethod())), instruments, status);
            instruments = (string)(result.GetParameterValue(0));
            status = (byte)(result.GetParameterValue(1));
            return status;
        }

        /// <summary>
        /// Сохранение поручения в БД
        /// Если передан orderId, то обновление поручения
        /// </summary>
        /// <returns></returns>
        [Function(Name = "[Risk].[SaveOrder]")]
        private int SaveOrder(
            [Parameter(Name = "Date",       DbType = "DATETIME")]DateTime date,
            [Parameter(Name = "OrderId",    DbType = "INT")]ref int orderId,
            [Parameter(Name = "OrderNo",    DbType = "INT")]int orderNo,
            [Parameter(Name = "TradeCode",  DbType = "VARCHAR(30)")]string tradeCode,
            [Parameter(Name = "SecCode",    DbType = "VARCHAR(50)")]string secCode,
            [Parameter(Name = "Quantity",   DbType = "INT")]int quantity,
            [Parameter(Name = "Price",      DbType = "NUMERIC(18,4)")]decimal price,
            [Parameter(Name = "OrderType",  DbType = "TINYINT")]byte orderType,
            [Parameter(Name = "OrderStatus",DbType = "TINYINT")]byte orderStatus)
        {
            IExecuteResult result = this.ExecuteMethodCall(this, ((MethodInfo)(MethodInfo.GetCurrentMethod())), 
                date, orderId, orderNo, tradeCode, secCode, quantity, price, 
                orderType, orderStatus);
            orderId = (int)(result.GetParameterValue(1));
            return (int)(result.ReturnValue);
        }

        #endregion

        public void CheckConnection()
        {
            lock (Connection)
            {
                if (Connection.State == ConnectionState.Broken)
                {
                    Connection.Close();
                    Connection.Open();
                }
                if (Connection.State == ConnectionState.Closed)
                {
                    Connection.Open();
                    return;
                }
            }
        }

        public IEnumerable<Client> GetClients()
        {
            return GetClients(FirmId);
        }

        public IEnumerable<Portfolio> GetPortfolios()
        {
            var result = GetPortfolios(FirmId);
            return result;
        }

        public IEnumerable<SessionInitialData> GetSessionInitialData()
        {
            var result = GetSessionInitialData(FirmId);
            return result;
        }

        public IEnumerable<MoneyInOut> GetMoneyInOutDay()
        {
            var result = GetMoneyInOutDay(FirmId);
            return result;
        }

        public sealed class Utf8StringWriter : StringWriter
        {
            public override Encoding Encoding { get { return Encoding.UTF8; } }
        }

        /// <summary>
        /// Сохранение настроек в БД
        /// </summary>
        public bool WriteSettings(RiskSettings settings)
        {
            string s;
            XmlSerializer xs = new XmlSerializer(typeof(RiskSettings));

            using (var sw = new Utf8StringWriter())
            {
                XmlWriter writer = XmlWriter.Create(sw);
                xs.Serialize(writer, settings);
                s = sw.ToString();
                sw.Close();
            }
            WriteSettings(s);
            return true;
        }

        /// <summary>
        /// Чтение настроек из БД
        /// </summary>
        public RiskSettings ReadSettings()
        {
            string settings = null;
            RiskSettings rs = null;
            ReadSettings(ref settings);
            if (String.IsNullOrEmpty(settings)) return null;

            try
            {
                XmlSerializer xs = new XmlSerializer(typeof(RiskSettings));
                using (var sr = new StringReader(settings))
                {
                    XmlReader reader = XmlReader.Create(sr);
                    rs = (RiskSettings)xs.Deserialize(reader);
                }
            }
            catch (Exception ex)
            {
                log.ErrorException(String.Format("Error Load Config: {0}", ex.Message), ex);
            }

            return rs;
        }

        /// <summary>
        ///  Отправка уведомления клиенту в терминал Transaq
        /// </summary>
        public void NotifyClientTransaq(string TradeCode, string Login, string Message)
        {
            // Для тестирования отправляем сообщение только по выбранным клиентам
            //TradeCode = "MCE1026"; 
            if (TradeCode == "MCE1026")
            {
                NotifyClientTransaq(FirmId, TradeCode, Login, Message);   
            }
        }

        /// <summary>
        /// Сохранение поручения в БД
        /// </summary>
        public int SaveOrder(Order order)
        {
            int orderId = order.OrderId;
            SaveOrder(order.Date, ref orderId, (int)order.OrderNo, order.TradeCode, 
                order.SecСode, order.Quantity, order.Price,
                (byte)order.OrderType, 0);
            return orderId;
        }
    }
}
