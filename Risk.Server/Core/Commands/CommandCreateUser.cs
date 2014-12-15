using System;
using NLog;
using Risk.Configuration;

namespace Risk.Core.Commands
{
    /// <summary>
    /// Команда создания пользователя
    /// </summary>
    [Command("CreateUser")]
    public class CommandCreateUser : CommandServer
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

        #region Overrides of CommandServer

        protected internal override void InternalExecute()
        {
            if (Connection == null)
                return;

            Connection.CheckAdminUser();

            if (string.IsNullOrWhiteSpace(UserName))
                throw new Exception("UserName can not be empty");


            if (string.IsNullOrWhiteSpace(Password))
                throw new Exception("Password can not be empty");

            // создаем пароль и пользователя
            var hash = PasswordHash.PasswordHash.CreateHash(Password);
            ServerBase.Current.ServerConfigurationSection.Authentications.Add(new AuthenticationConfigurationElement
            {
                UserName = UserName.ToUpper(),
                PasswordHash = hash
            });

            // сохраняем конфиг
            ServerBase.Current.ServerConfigurationSection.UpdateConfigFromObject();
        }
        #endregion
    }
}
