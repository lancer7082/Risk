using System;
using System.Collections.Generic;
using System.Linq;

namespace Risk
{
    /// <summary>
    /// Таблица курсов
    /// </summary>
    [Table("Alerts")]
    public class Alerts : Table<Alert,AlertInfo>
    {
        public override void TriggerAfter(TriggerCollection<Alert> items)
        {
            foreach (var alert in items.Inserted)
            {                
                if (!String.IsNullOrEmpty(alert.Text))
                {
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
                                Message = alert.Text,
                                TradeCode = alert.Portfolio.TradeCode,
                                Login = null
                            }.ExecuteAsync();

                            if (alert.PortfolioRule != null)
                            {
                                // Ставим признак, что сообщение в терминал уже послано
                                alert.PortfolioRule.NotifyTypesAccomplished |= NotifyType.Terminal;
                            }
                        }

                        if (alert.NotifyType.HasFlag(NotifyType.Email) &&
                            (alert.PortfolioRule == null ||
                            !alert.PortfolioRule.NotifyTypesAccomplished.HasFlag(NotifyType.Email)))
                        {
                            var body = String.Format("Уважаемый клиент.\n{0}\nСчет: {1}",
                                alert.Text, alert.Portfolio.TradeCode);

                            // Оповещение клиента по Email
                            new CommandSendMail
                            {
#if DEBUG
                                From = "<Risk (Test)> <noreply@corp.finam.ru>",
                                To = "turyansky@corp.finam.ru;khayrullin@corp.finam.ru",
#else
                                From = "<Risk> <noreply@corp.finam.ru>",
                                To = "turyansky@corp.finam.ru;khayrullin@corp.finam.ru;dorofeev@corp.finam.ru",
#endif
                                Body = body,
                                Subject = "Оповещение о наступлении события"
                            }.ExecuteAsync();

                            if (alert.PortfolioRule != null)
                            {
                                // Ставим признак, что сообщение в почту уже послано
                                alert.PortfolioRule.NotifyTypesAccomplished |= NotifyType.Email;
                            }
                        }
                    }

                    var message = "";
                    if (alert.Portfolio != null)
                        message = String.Format("{0} : {1}", alert.Portfolio.TradeCode, alert.Text);
                    else
                        message = alert.Text;

                    // Оповещение пользователя
                    new CommandMessage
                    {
                        MessageType = MessageType.Info,
                        Message = message,
                    }.ExecuteAsync();
                }
            }
        }
    }
}
