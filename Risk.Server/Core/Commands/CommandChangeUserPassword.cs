using NLog;

namespace Risk.Core.Commands
{
    /// <summary>
    /// Команда изменения пароля пользователя
    /// </summary>
    [Command("ChangeUserPassword")]
    public class CommandChangeUserPassword : CommandServer
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Старый пароль
        /// </summary>
        public string OldPassword
        {
            get { return (string)Parameters["OldPassword"]; }
            set { Parameters["OldPassword"] = value; }
        }

        /// <summary>
        /// Новый пароль
        /// </summary>
        public string NewPassword
        {
            get { return (string)Parameters["NewPassword"]; }
            set { Parameters["NewPassword"] = value; }
        }

        #region Overrides of CommandServer

        protected internal override void InternalExecute()
        {
            if (Connection == null)
                return;
            
            // берем текущего пользователя
            var auth = ServerBase.Current.ServerConfigurationSection.Authentications[Connection.UserName];
            if (auth == null)
                return;

            // проверяем старый пароль
            var isPasswordValid = PasswordHash.PasswordHash.ValidatePassword(OldPassword, auth.PasswordHash);

            if (!isPasswordValid)
                return;

            // создаем новый
            var hash = PasswordHash.PasswordHash.CreateHash(NewPassword);
            auth.PasswordHash = hash;

            // сохраняем
            ServerBase.Current.ServerConfigurationSection.UpdateConfigFromObject();
        }
        #endregion
    }
}
