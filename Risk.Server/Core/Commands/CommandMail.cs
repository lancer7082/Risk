using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using NLog;

namespace Risk
{
    [Command("SendMail")]
    public class CommandSendMail : CommandServer
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// От кого
        /// </summary>
        public string From
        {
            get { return (string)Parameters["From"]; }
            set { Parameters["From"] = value; }
        }
        
        /// <summary>
        /// Кому
        /// </summary>
        public string To
        {
            get { return (string)Parameters["To"]; }
            set { Parameters["To"] = value; }
        }

        /// <summary>
        /// Копия
        /// </summary>
        public string CC
        {
            get { return (string)Parameters["CC"]; }
            set { Parameters["CC"] = value; }
        }

        /// <summary>
        /// Скрытая копия
        /// </summary>
        public string BCC
        {
            get { return (string)Parameters["BCC"]; }
            set { Parameters["BCC"] = value; }
        }

        /// <summary>
        /// Приоритет
        /// </summary>
        public MailPriority Priority
        {
            get { return (MailPriority)Parameters["Priority", MailPriority.Normal]; }
            set { Parameters["Priority"] = value; }
        }

        /// <summary>
        /// Тема письма
        /// </summary>
        public string Subject
        {
            get { return (string)Parameters["Subject"]; }
            set { Parameters["Subject"] = value; }
        }

        /// <summary>
        /// Тест письма в html
        /// </summary>
        public bool IsBodyHtml
        {
            get { return (bool)Parameters["IsBodyHtml", false]; }
            set { Parameters["IsBodyHtml"] = value; }
        }

        /// <summary>
        /// Текст письма
        /// </summary>
        public string Body
        {
            get { return (string)Parameters["Body"]; }
            set { Parameters["Body"] = value; }
        }

        private void AddAddresses(MailAddressCollection mailAddresses, string addresses)
        {
            if (!String.IsNullOrWhiteSpace(addresses))
                foreach (string email in addresses.Split(';'))
                {
                    mailAddresses.Add(email);
                }
        }

        protected internal override void InternalExecute()
        {
            var logParams = new StringBuilder();
            try
            {
                SmtpClient client = new SmtpClient("mail.finam.ru", 25); // TODO: !!! Remove to config
                client.EnableSsl = false; // TODO: ??? Remove to confg
                client.DeliveryMethod = SmtpDeliveryMethod.Network;

                // TODO: ???
                // client.Credentials = new NetworkCredential(UserName, UserPassword);
                client.UseDefaultCredentials = true;

                // Прописываем полное имя хоста, иначе имее ошибку: Helo command rejected: need fully-qualified hostname
                // if (ClientDomain != null)
                // {
                //     const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                //     FieldInfo result = typeof(SmtpClient).GetField("clientDomain", flags);
                //     result.SetValue(client, ClientDomain);
                // }

                MailMessage msg = new MailMessage();
                msg.From = new MailAddress(From);

                if (String.IsNullOrWhiteSpace(To))
                    throw new Exception("AddressTo can not be empty");

                AddAddresses(msg.To, To);
                AddAddresses(msg.CC, CC);
                AddAddresses(msg.Bcc, BCC);

                msg.Subject = Subject;
                msg.Priority = Priority;
                msg.IsBodyHtml = IsBodyHtml;
                msg.Body = Body;

                // TODO: ??? Attachments
                //if (AttachData)
                //{
                //    stream.Position = 0;
                //    Attachment attData = new Attachment(stream, "data.xml");
                //    msg.Attachments.Add(attData);
                //}

                // Если нужна определенная кодировка
                //msg.SubjectEncoding = Encoding.Default;
                //msg.BodyEncoding = Encoding.Default;
                //msg.Headers["Content-type"] = "text/plain; charset=windows-1251";

                //msg.SubjectEncoding = System.Text.Encoding.UTF8; // Указываем кодировку темы письма
                //msg.BodyEncoding = System.Text.Encoding.UTF8; // Указываем кодировку текста письма

                logParams.Append(String.Format("To = '{0}'", To));

                if (!String.IsNullOrWhiteSpace(CC))
                    logParams.Append(String.Format(", CC = '{0}'", CC));

                if (!String.IsNullOrWhiteSpace(BCC))
                    logParams.Append(String.Format(", BCC = '{0}'", BCC));

                client.Send(msg);
                log.Info("SendMail ({0}): {1}", logParams, Subject);
            }
            catch (Exception ex)
            {
                log.ErrorException(String.Format("Error SendMail ({0}): {1}", logParams, ex.Message), ex);
                throw;
            }
        }
    }
}