using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Risk.Commands;

namespace Risk
{
    /// <summary>
    /// Таблица курсов
    /// </summary>
    [Table("Alerts", KeyFields = "AlertId")]
    public class Alerts : Table<Alert, AlertInfo>
    {
        public override void TriggerAfter(TriggerCollection<Alert> items)
        {
            foreach (var alert in items.Inserted)
            {
                if (!String.IsNullOrEmpty(alert.Text))
                {

                    alert.NotificationId = ServerBase.Current.DataBase.SaveNotification(new SavedNotification
                    {
                        AlertId = alert.AlertId,
                        DeliveryMethod = alert.NotifyType,
                        MessageText = alert.Text,
                        TradeCode = alert.Portfolio != null ? alert.Portfolio.TradeCode : null,
                        Recipients = alert.Portfolio != null ? alert.Portfolio.Client : null,
                        Type = alert.PortfolioRule != null ? alert.PortfolioRule.RuleType : (RuleType?)null,
                        UpdateDate = alert.DateTime
                    });

                    if (alert.Portfolio != null)
                    {
                        // Оповещение клиента
                        if (alert.NotifyType.HasFlag(NotifyType.Terminal) &&
                            (alert.PortfolioRule == null ||
                            !alert.PortfolioRule.NotifyTypesAccomplished.HasFlag(NotifyType.Terminal)))
                        {
                            // Оповещение клиента через терминал
                            new CommandNotifyClientTransaq
                            {
                                Parameters = new ParameterCollection
                                {
                                    new Parameter
                                    {
                                        Name = "TradeCode",
                                        Value = alert.Portfolio.TradeCode
                                    },
                                    new Parameter
                                    {
                                        Name = "Message",
                                        Value = alert.Text
                                    }
                                }
                            }.ExecuteAsync();

                            if (alert.PortfolioRule != null)
                            {
                                // Ставим признак, что сообщение в терминал уже послано
                              //  alert.PortfolioRule.NotifyTypesAccomplished |= NotifyType.Terminal;
                            }
                        }

                        if (alert.NotifyType.HasFlag(NotifyType.Email) &&
                            (alert.PortfolioRule == null ||
                            !alert.PortfolioRule.NotifyTypesAccomplished.HasFlag(NotifyType.Email)))
                        {
                            NotifyByEmail(alert);
                        }
                    }

                    var message = "";
                    if (alert.Portfolio != null)
                        message = String.Format("{0} : {1}", alert.Portfolio.TradeCode, alert.Text);
                    else
                        message = alert.Text;

                    // Оповещение пользователя
                    new CommandAlert
                    {
                        AlertType =  alert.AlertType,
                        Message = message,
                    }.ExecuteAsync();

                    /*
                    // Оповещение пользователя
                    new CommandMessage
                    {
                        MessageType = MessageType.Info,
                        Message = message,
                    }.ExecuteAsync();
                    */
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public override object GetData(ParameterCollection parameters)
        {
            var items = ((IEnumerable<AlertInfo>)base.GetData(parameters)).ToList();

            DateTime dateFrom, dateTo;

            GetDatesParameters(parameters, out dateFrom, out dateTo);

            // если даты не указаны, то возвращаем оригинальную коллекцию
            if (dateFrom == DateTime.MinValue && dateTo == DateTime.MaxValue)
                return items.ToArray();

            // загружаем опопвещения из БД
            var notifications = ServerBase.Current.DataBase.LoadNotifications(dateFrom, dateTo);

            var alerts = new List<Alert>();

            // создаем для каждого загруженного из БД оповещения алерт и добавляем его в оригинальную коллекцию
            foreach (var notification in notifications)
            {
                var alert = new Alert
                {
                    AlertId = notification.AlertId,
                    DateTime = notification.UpdateDate,
                    NotificationId = notification.Id,
                    NotifyType = notification.DeliveryMethod,
                    Text = notification.MessageText
                };

                if (notification.Type != null)
                    alert.PortfolioRule = new PortfolioRule
                    {
                        RuleType = notification.Type.Value,
                        NotifyTypesAccomplished = NotifyType.Email | NotifyType.SMS | NotifyType.Terminal // чтобы не рассылать уведомления
                    };

                if (!string.IsNullOrEmpty(notification.TradeCode))
                    alert.Portfolio = new Portfolio
                    {
                        TradeCode = notification.TradeCode
                    };

                alerts.Add(alert);
            }

            alerts.RemoveAll(s => items.Any(a => a.AlertId == s.AlertId));

            var predicateResult = PredicateResult(parameters);
            if (predicateResult != null)
                items.AddRange(alerts.Select(alert => (AlertInfo)alert).Where(predicateResult.Compile()));
            else
                items.AddRange(alerts.Select(alert => (AlertInfo)alert));

            return items.ToArray();
        }


        protected override Expression<Func<AlertInfo, bool>> PredicateResult(ParameterCollection parameters)
        {
            var predicate = base.PredicateResult(parameters);

            DateTime dateFrom, dateTo;

            GetDatesParameters(parameters, out dateFrom, out dateTo);

            // добавляем фильтр в предикат если хотя бы одна дата заполена
            if (dateFrom != DateTime.MinValue || dateTo != DateTime.MaxValue)
                predicate = predicate.And(s => s.DateTime.Date >= dateFrom && s.DateTime.Date <= dateTo);

            return predicate;
        }

        /// <summary>
        /// Загружает даты из параметров todo Вынести в общий хелпер
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="dateFrom"></param>
        /// <param name="dateTo"></param>
        public static void GetDatesParameters(ParameterCollection parameters, out DateTime dateFrom, out DateTime dateTo)
        {
            dateFrom = DateTime.MinValue;
            dateTo = DateTime.MaxValue;

            if (parameters["DateBegin"] != null)
                dateFrom = DateTime.Parse((string)parameters["DateBegin"]).Date;
            if (parameters["DateEnd"] != null)
                dateTo = DateTime.Parse((string)parameters["DateEnd"]).Date;
        }

        /// <summary>
        /// Отправка email
        /// </summary>
        /// <param name="alert"></param>
        private static void NotifyByEmail(Alert alert)
        {
            var body = MakeMessageBody(alert);
            var subject = MakeMessageSubject(alert);
            var recipients = MakeRecipients(alert);

            // Оповещение клиента по Email
            new CommandSendMail
            {
#if DEBUG
                From = "<Risk (Test)> <24_support@corp.whotrades.eu>",
                To = "turyansky@corp.finam.ru;khayrullin@corp.finam.ru;ayukulikov@corp.finam.ru;dssmirnov@corp.finam.ru",
                Subject = subject,
#else
                From = "<Risk> <24_support@corp.whotrades.eu>",
                To = recipients + ";turyansky@corp.finam.ru;khayrullin@corp.finam.ru",
                Subject = subject,
#endif
                Body = body,
                IsBodyHtml = true
            }.ExecuteAsync();

            if (alert.PortfolioRule != null)
            {
                // Ставим признак, что сообщение в почту уже послано
                //alert.PortfolioRule.NotifyTypesAccomplished |= NotifyType.Email;
            }
        }

        /// <summary>
        /// Формирование получателей сообщения
        /// </summary>
        /// <param name="alert"></param>
        /// <returns></returns>
        private static string MakeRecipients(Alert alert)
        {
            if (alert.PortfolioRule.RuleType == RuleType.MaxPercentUtilWarningExceed)
            {
                return "dssmirnov@corp.finam.ru;moiseev_a@corp.finam.ru";
            }
            else if (alert.PortfolioRule.RuleType == RuleType.MaxPercentUtilMarginCallExceed)
            {
                return "dssmirnov@corp.finam.ru;moiseev_a@corp.finam.ru";
            }
            return "dssmirnov@corp.finam.ru;moiseev_a@corp.finam.ru";
        }

        /// <summary>
        /// Формирование темы сообщения
        /// </summary>
        /// <param name="alert"></param>
        /// <returns></returns>
        private static string MakeMessageSubject(Alert alert)
        {
            if (alert.PortfolioRule.RuleType == RuleType.MaxPercentUtilWarningExceed)
            {
                return "WhoTrades. Извещение о превышении использования капитала по счету MMA";
            }
            else if (alert.PortfolioRule.RuleType == RuleType.MaxPercentUtilMarginCallExceed)
            {
                return "WhoTrades. Извещение о закрытии позиции по счету MMA";
            }
            return "WhoTrades. Оповещение о наступлении события";
        }

        /// <summary>
        /// Формирование сообщения почты
        /// </summary>
        /// <param name="alert"></param>
        /// <returns></returns>
        private static string MakeMessageBody(Alert alert)
        {
            var stringBuilder = new StringBuilder();

            if (alert.PortfolioRule == null)
                return string.Empty;

            if (alert.PortfolioRule.RuleType == RuleType.MaxPercentUtilWarningExceed)
            {
                stringBuilder.AppendFormat("Добрый день, {0}! {1}", alert.Portfolio.Client, "</br>");
                stringBuilder.AppendFormat("{0:dd.MM.yyyy} {1:HH:mm:ss} Уровень обеспечения открытых позиций {2} равно {4:.##}%. {3}",
                                  alert.PortfolioRule.RuleTime, alert.PortfolioRule.RuleTime, alert.Portfolio.TradeCode, "</br>", alert.Portfolio.CoverageFact);

                stringBuilder.AppendFormat("При снижении обеспечения открытых позиций ниже 50% мы будем вынуждены закрыть позиции до 100% обеспечения {0}", "</br>");
            }
            else if (alert.PortfolioRule.RuleType == RuleType.MaxPercentUtilMarginCallExceed)
            {
                stringBuilder.AppendFormat("Добрый день, {0}! {1}", alert.Portfolio.Client, "</br>");
                stringBuilder.AppendFormat("{0:dd.MM.yyyy} в {1:HH:mm:ss} Ваши позиции по счету {2} были частично закрыты в связи с недостаточным обеспечением. {3}",
                                  alert.PortfolioRule.RuleTime, alert.PortfolioRule.RuleTime, alert.Portfolio.TradeCode, "</br>");
                stringBuilder.AppendFormat("Уровень обеспечения открытых позиций снизился ниже 50%. {0}", "</br>");
            }
            else if (alert.PortfolioRule.RuleType == RuleType.IncorrectGORate)
            {
                stringBuilder.AppendFormat("Добрый день, {0}! {1}", alert.Portfolio.Client, "</br>");
                stringBuilder.AppendFormat("В соответствии с регламентом проведения торгов {0:dd.MM.yyyy} в {1:HH:mm:ss} все ваши открытые позиции по счету {2} должны были быть принудительно закрыты в связи с недостаточным обеспечением:{3}",
                                  alert.PortfolioRule.RuleTime, alert.PortfolioRule.RuleTime, alert.Portfolio.TradeCode, "</br>");
                stringBuilder.AppendFormat("Однако это не удалось осуществить из-за расхождения ставок ГО </br>");
                stringBuilder.AppendFormat("Размер капитала равен {0:.##} {1} {2}", alert.Portfolio.Capital, alert.Portfolio.Currency, "</br>");
                stringBuilder.AppendFormat("Уровень использования капитала равен {0:.##}% {1}", alert.Portfolio.UtilizationFact, "</br>");
            }
            return stringBuilder.ToString();
        }
    }
}
