using System;
using NLog;

namespace Risk.Core.Commands
{
    /// <summary>
    /// Команда определения группы пользователя
    /// </summary>
    [Command("GetUserGroup")]
    public class CommandGetUserGroup : CommandServer
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

        #region Overrides of CommandServer

        protected internal override void InternalExecute()
        {
            if (Connection == null)
                return;

            if (string.IsNullOrWhiteSpace(UserName))
                throw new Exception("You must specify user name");

            // берем группу пользователя и возвращаем ее
            var userGroup = ServerBase.Current.ServerConfigurationSection.UsersToGroups[UserName.ToUpper()];
            if (userGroup == null)
            {
                SetResult(-1);
                return;
            }
            //SetResult(((Connection.UserGroup)userGroup.GroupId).ToString().ToUpper());
            SetResult(userGroup.GroupId);
        }
        #endregion
    }
}
