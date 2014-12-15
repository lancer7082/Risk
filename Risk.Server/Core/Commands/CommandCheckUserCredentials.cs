using System;
using NLog;
using Risk.Configuration;

namespace Risk.Core.Commands
{
    /// <summary>
    /// Команда создания пользователя
    /// </summary>
    [Command("CheckUserCredentials")]
    public class CommandCheckUserCredentials : CommandServer
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Логин
        /// </summary>
        public string UserName
        {
            get { return (string)Parameters["UserName"]; }
            set { Parameters["UserName"] = value; }
        }

        /// <summary>
        /// Пароль
        /// </summary>
        public string Password
        {
            get { return (string)Parameters["Password"]; }
            set { Parameters["Password"] = value; }
        }

        /// <summary>
        /// WebAccess
        /// </summary>
        public bool WebAccess
        {
            get
            {
                return Parameters["WebAccess"] != null && Convert.ToBoolean(Parameters["WebAccess"]);
            }
            set { Parameters["WebAccess"] = value; }
        }

        #region Overrides of CommandServer

        protected internal override void InternalExecute()
        {
            if (Connection == null)
            {
                SetResult(false);
                return;
            }

            if (string.IsNullOrWhiteSpace(UserName))
                throw new Exception("UserName can not be empty");


            if (string.IsNullOrWhiteSpace(Password))
                throw new Exception("Password can not be empty");

            // берем текущего пользователя
            var auth = ServerBase.Current.ServerConfigurationSection.Authentications[UserName.ToUpper()];
            if (auth == null)
            {
                SetResult(false);
                return;
            }

            // проверяем старый пароль
            var isPasswordValid = PasswordHash.PasswordHash.ValidatePassword(Password, auth.PasswordHash);

            if (isPasswordValid)
            {
                if (!WebAccess || (WebAccess && auth.WebAccess))
                {
                    SetResult(true);
                    return;
                }
            }
            SetResult(false);
        }
        #endregion
    }
}
