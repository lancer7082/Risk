using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Text;
using NLog;
using Risk.Commands;

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

        /// <summary>
        /// Получение позиций Этны на момент начала торговой сессии
        /// </summary>
        /// <param name="firmId"></param>
        /// <returns></returns>
        [Function(Name = "[Risk].[EtnaOpenPositions]")]
        public IEnumerable<ETNAPosition> GetEtnaOpenPositions([Parameter(Name = "firmId", DbType = "TinyInt")]byte firmId)
        {
            IExecuteResult result = this.ExecuteMethodCall(this, ((MethodInfo)(MethodInfo.GetCurrentMethod())), firmId);
            return ((IEnumerable<ETNAPosition>)(result.ReturnValue));
        }

        /// <summary>
        /// Получение данных по позициям из RRM
        /// </summary>
        /// <returns></returns>
        [Function(Name = "[Risk].[GetRRMPositionsSummary]")]
        [ResultType(typeof(ExternalPosition))]
        public IMultipleResults GetRRMPositionsSummary()
        {
            var result = ExecuteMethodCall(this, ((MethodInfo)(MethodInfo.GetCurrentMethod())));
            return (IMultipleResults)result.ReturnValue;
        }

        /// <summary>
        /// Получение данных по позициям из FORTS
        /// </summary>
        /// <returns></returns>
        [Function(Name = "[Risk].[GetFORTSPositionsSummary]")]
        [ResultType(typeof(FORTSPosition))]
        public IMultipleResults GetFORTSPositionsSummary()
        {
            var result = ExecuteMethodCall(this, ((MethodInfo)(MethodInfo.GetCurrentMethod())));
            return (IMultipleResults)result.ReturnValue;
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
            [Parameter(Name = "nnFirm", DbType = "TinyInt")]    byte firmId,
            [Parameter(Name = "TradeCode", DbType = "VARCHAR(30)")]string TradeCode,
            [Parameter(Name = "Login", DbType = "VARCHAR(30)")]string Login,
            [Parameter(Name = "Message", DbType = "VARCHAR(8000)")]string Message)
        {
            IExecuteResult result = this.ExecuteMethodCall(this, ((MethodInfo)(MethodInfo.GetCurrentMethod())), firmId, TradeCode, Login, Message);
            return (int)(result.ReturnValue);
        }

        /// <summary>
        /// Сохранение поручения в БД
        /// Если передан orderId, то обновление поручения
        /// </summary>
        /// <returns></returns>
        [Function(Name = "[Risk].[SaveOrder]")]
        private int SaveOrder(
            [Parameter(Name = "Date", DbType = "DATETIME")]DateTime date,
            [Parameter(Name = "OrderId", DbType = "INT")]ref int orderId,
            [Parameter(Name = "OrderNo", DbType = "INT")]int orderNo,
            [Parameter(Name = "TradeCode", DbType = "VARCHAR(30)")]string tradeCode,
            [Parameter(Name = "SecCode", DbType = "VARCHAR(50)")]string secCode,
            [Parameter(Name = "Quantity", DbType = "INT")]int quantity,
            [Parameter(Name = "Price", DbType = "NUMERIC(18,4)")]decimal price,
            [Parameter(Name = "OrderType", DbType = "TINYINT")]byte orderType,
            [Parameter(Name = "OrderStatus", DbType = "TINYINT")]byte orderStatus)
        {
            IExecuteResult result = this.ExecuteMethodCall(this, ((MethodInfo)(MethodInfo.GetCurrentMethod())),
                date, orderId, orderNo, tradeCode, secCode, quantity, price,
                orderType, orderStatus);
            orderId = (int)(result.GetParameterValue(1));
            return (int)(result.ReturnValue);
        }

        /// <summary>
        /// Загрузка поручений
        /// </summary>
        /// <returns></returns>
        [Function(Name = "[Risk].[LoadOrders]")]
        public IEnumerable<Order> LoadOrders()
        {
            var result = ExecuteMethodCall(this, ((MethodInfo)(MethodInfo.GetCurrentMethod())));
            return (IEnumerable<Order>)result.ReturnValue;
        }

        /// <summary>
        /// Сохранение оповещения в БД
        /// </summary>
        /// <returns></returns>
        [Function(Name = "[Risk].[SaveNotification]")]
        private int SaveNotification(
            [Parameter(Name = "Id", DbType = "BIGINT")]out long id,
            [Parameter(Name = "AlertId", DbType = "uniqueidentifier")]Guid alertId,
            [Parameter(Name = "Type", DbType = "INT")]int? type,
            [Parameter(Name = "DeliveryMethod", DbType = "INT")]int deliveryMethod,
            [Parameter(Name = "TradeCode", DbType = "VARCHAR(20)")]string tradeCode,
            [Parameter(Name = "Recipients", DbType = "VARCHAR(MAX)")]string recipients,
            [Parameter(Name = "MessageText", DbType = "VARCHAR(MAX)")]string messageText,
            [Parameter(Name = "UpdateDate", DbType = "DATETIME")]DateTime updateDate)
        {
            id = 0;
            var result = ExecuteMethodCall(this, ((MethodInfo)(MethodInfo.GetCurrentMethod())), id, alertId,
                type, deliveryMethod, tradeCode, recipients, messageText, updateDate);
            id = (long)(result.GetParameterValue(0));
            return (int)(result.ReturnValue);
        }

        /// <summary>
        /// Загрузка оповещений
        /// </summary>
        /// <returns></returns>
        [Function(Name = "[Risk].[LoadNotifications]")]
        public IEnumerable<SavedNotification> LoadNotifications(
            [Parameter(Name = "DateFrom", DbType = "DATETIME")]DateTime dateFrom,
            [Parameter(Name = "DateTo", DbType = "DATETIME")]DateTime dateTo)
        {
            var result = ExecuteMethodCall(this, ((MethodInfo)(MethodInfo.GetCurrentMethod())), dateFrom, dateTo);
            return (IEnumerable<SavedNotification>)result.ReturnValue;
        }

        /// <summary>
        /// Загрузка оповещений
        /// </summary>
        /// <returns></returns>
        [Function(Name = "[Risk].[LoadTrades]")]
        public IEnumerable<Trade> LoadTrades(
            [Parameter(Name = "DateFrom", DbType = "DATETIME")]DateTime dateFrom,
            [Parameter(Name = "DateTo", DbType = "DATETIME")]DateTime dateTo)
        {
            var result = ExecuteMethodCall(this, ((MethodInfo)(MethodInfo.GetCurrentMethod())), dateFrom, dateTo);
            return (IEnumerable<Trade>)result.ReturnValue;
        }

        /// <summary>
        /// Изменяет торговые параметры клиента
        /// </summary>
        [Function(Name = "[Risk].[UpdateTraderAccountParameters]")]
        public int ChangeTraderAccountParameters(
           [Parameter(Name = "ClientCodes", DbType = "VARCHAR(MAX)")]string clientCodes,
           [Parameter(Name = "BsStopDeny", DbType = "BIT")]bool? bsStopDeny,
           [Parameter(Name = "GoCoeff", DbType = "INT")]int? goCoeff,
           [Parameter(Name = "AccessAuction", DbType = "BIT")]bool? accessAuction,
           [Parameter(Name = "Retain", DbType = "VARCHAR(20)")]string retain,
           [Parameter(Name = "Login", DbType = "VARCHAR(MAX)")]string login)
        {
            var result = ExecuteMethodCall(this, ((MethodInfo)(MethodInfo.GetCurrentMethod())), clientCodes, bsStopDeny, goCoeff,
                accessAuction, retain, login);
            return (int)(result.ReturnValue);
        }

        /// <summary>
        /// Сохраняет торговую статистику по клиенту
        /// </summary>
        [Function(Name = "[Risk].[SaveClientTradingStatistic]")]
        public int SaveClientTradingStatistic(

            [Parameter(Name = "TradeId", DbType = "BIGINT")]long tradeId,
            [Parameter(Name = "OrderId", DbType = "BIGINT")]long orderId,
            [Parameter(Name = "UpdateDate", DbType = "DATETIME")]DateTime updateDate,
            [Parameter(Name = "Login", DbType = "VARCHAR(50)")]string login,
            [Parameter(Name = "TradeCode", DbType = "VARCHAR(50)")]string tradeCode,
            [Parameter(Name = "Client", DbType = "VARCHAR(150)")]string client,
            [Parameter(Name = "PortfolioCurrency", DbType = "VARCHAR(3)")]string portfolioCurrency,
            [Parameter(Name = "Capital", DbType = "NUMERIC(18,4)")]decimal capital,
            [Parameter(Name = "CoverageFact", DbType = "NUMERIC(18,4)")]decimal coverageFact,
            [Parameter(Name = "UtilizationFact", DbType = "NUMERIC(18,4)")]decimal utilizationFact,

            [Parameter(Name = "SecCode", DbType = "VARCHAR(50)")]string secCode,
            [Parameter(Name = "SecurityCurrency", DbType = "VARCHAR(3)")]string securityCurrency,
            [Parameter(Name = "Quote", DbType = "NUMERIC(18,4)")]decimal quote,
            [Parameter(Name = "OpenBalance", DbType = "INT")]int openBalance,
            [Parameter(Name = "Bought", DbType = "INT")]int bought,
            [Parameter(Name = "Sold", DbType = "INT")]int sold,
            [Parameter(Name = "Balance", DbType = "INT")]int balance,
            [Parameter(Name = "PL", DbType = "NUMERIC(18,4)")]decimal pl)
        {
            var result = ExecuteMethodCall(this, ((MethodInfo)(MethodInfo.GetCurrentMethod())), tradeId, orderId, updateDate, login, tradeCode,
                client, portfolioCurrency, capital, coverageFact, utilizationFact, secCode, securityCurrency, quote,
                openBalance, bought, sold, balance, pl);
            return (int)(result.ReturnValue);
        }

        /// <summary>
        /// Изменяет параметры инструментов
        /// </summary>
        [Function(Name = "[Risk].[UpdateInstrumentsParameters]")]
        public int ChangeInstrumentsParameters(
            [Parameter(Name = "Codes", DbType = "VARCHAR(MAX)")]string codes,
            [Parameter(Name = "Enabled", DbType = "BIT")]bool? enabled,
            [Parameter(Name = "MarketPermitted", DbType = "BIT")]bool? marketPermitted,
            [Parameter(Name = "LongPermitted", DbType = "BIT")]bool? longPermitted,
            [Parameter(Name = "ShortPermitted", DbType = "BIT")]bool? shortPermitted,
            [Parameter(Name = "Resident", DbType = "INT")]int? resident,
            [Parameter(Name = "BsStopDeny", DbType = "BIT")]bool? bsStopDeny,
            [Parameter(Name = "BsStopDenyZone", DbType = "INT")]int? bsStopDenyZone,
            [Parameter(Name = "Login", DbType = "VARCHAR(MAX)")]string login
           )
        {
            const int partSize = 30;
            const string delimeter = ",";

            IExecuteResult result = null;
            var splittedCodes = codes.Split(delimeter.ToCharArray()).ToList();
            if (splittedCodes.Count < partSize)
            {
                result  = ExecuteMethodCall(this, ((MethodInfo)(MethodInfo.GetCurrentMethod())), codes, enabled,
                             marketPermitted, longPermitted, shortPermitted, resident, bsStopDeny, bsStopDenyZone, login);
            }
            else
            {
                while (splittedCodes.Any())
                {
                    var partCodes = splittedCodes.Take(partSize).ToList();
                    
                    if (splittedCodes.Count > partSize)
                        splittedCodes.RemoveRange(0, partSize);
                    else
                        splittedCodes.Clear();

                    var partCodesString = partCodes.Aggregate(string.Empty, (s, s1) => s + s1 + delimeter).TrimEnd(delimeter.ToCharArray());
                    result = ExecuteMethodCall(this, ((MethodInfo)(MethodInfo.GetCurrentMethod())), partCodesString, enabled,
                        marketPermitted, longPermitted, shortPermitted, resident, bsStopDeny, bsStopDenyZone, login);
                }
            }
            return (int)(result.ReturnValue);
        }

        /// <summary>
        /// Получение итоговый результата
        /// </summary>
        /// <param name="firmId"></param>
        /// <param name="currencyCalc"></param>
        /// <param name="dateFrom"></param>
        /// <param name="dateTo"></param>
        /// <returns></returns>
        [Function(Name = "[Risk].[FinRes]")]
        public IEnumerable<FinancialResult> LoadFinancialResults(
            [Parameter(Name = "FirmId", DbType = "TinyInt")] byte firmId,
            [Parameter(Name = "CurrencyCalc", DbType = "VARCHAR(3)")] string currencyCalc,
            [Parameter(Name = "DateFrom", DbType = "DATETIME")] DateTime? dateFrom,
            [Parameter(Name = "DateTo", DbType = "DATETIME")] DateTime? dateTo)
        {
            IExecuteResult result = ExecuteMethodCall(this, ((MethodInfo)(MethodInfo.GetCurrentMethod())), firmId, currencyCalc, dateFrom, dateTo);
            return ((IEnumerable<FinancialResult>)(result.ReturnValue));
        }

        #endregion

        public IEnumerable<ETNAPosition> GetEtnaOpenPositions()
        {
            return GetEtnaOpenPositions(FirmId);
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


            try
            {
                ReadSettings(ref settings);
                if (String.IsNullOrEmpty(settings)) return null;

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
#if DEBUG
            if (TradeCode == "MCE1026")
#endif
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
                order.SecCode, order.Quantity, order.Price,
                (byte)order.OrderType, 0);
            return orderId;
        }

        /// <summary>
        /// Сохранение поручения в БД
        /// </summary>
        public long SaveNotification(SavedNotification notification)
        {
            long id;

            int? notificationType = null;
            if (notification.Type.HasValue)
                notificationType = (int)notification.Type;

            SaveNotification(out id, notification.AlertId, notificationType, (int)notification.DeliveryMethod, notification.TradeCode, notification.Recipients,
                notification.MessageText, notification.UpdateDate);
            return id;
        }
    }

    /// <summary>
    /// todo перенести
    /// Сохраненные в БД оповещения пользователя
    /// </summary>
    public class SavedNotification
    {
        /// <summary>
        /// Id
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// AlertId
        /// </summary>
        public Guid AlertId { get; set; }

        /// <summary>
        /// Тип оповещения
        /// </summary>
        public RuleType? Type { get; set; }

        /// <summary>
        /// Способ доставки оповещения
        /// </summary>
        public NotifyType DeliveryMethod { get; set; }

        /// <summary>
        /// Торговый код
        /// </summary>
        public string TradeCode { get; set; }

        /// <summary>
        /// Получатели
        /// </summary>
        public string Recipients { get; set; }

        /// <summary>
        /// Текс сообщения
        /// </summary>
        public string MessageText { get; set; }

        /// <summary>
        /// Дата опповещения
        /// </summary>
        public DateTime UpdateDate { get; set; }
    }
}
