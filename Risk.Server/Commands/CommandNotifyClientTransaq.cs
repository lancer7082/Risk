using System;

namespace Risk.Commands
{
    /// <summary>
    /// Уведомление клиента в терминале Transaq
    /// </summary>
    [Command("NotifyClientTransaq")]
    public class CommandNotifyClientTransaq : CommandServer
    {
        private const string Delimeter = ",";

        protected internal override void InternalExecute()
        {
            string tradeCode, message;

            if (Connection != null)
                Connection.CheckDealerUser();

            try
            {
                tradeCode = Parameters["TradeCode"].ToString();
                message = Parameters["Message"].ToString();
            }
            catch
            {
                throw new Exception("Ошибка в параметрах");
            }

            if (tradeCode.Contains(Delimeter))
            {
                tradeCode = tradeCode.TrimEnd(Delimeter.ToCharArray());
                var tradeCodes = tradeCode.Split(Delimeter.ToCharArray());
                foreach (var code in tradeCodes)
                {
                    ServerBase.Current.DataBase.NotifyClientTransaq(code, string.Empty, message);
                }
            }
            else
            {
                ServerBase.Current.DataBase.NotifyClientTransaq(tradeCode, string.Empty, message);
            }
        }
    }
}
