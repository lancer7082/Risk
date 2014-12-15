using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace Risk.Core.Commands
{
    /// <summary>
    /// Команда проверки вхожения пользователя в группу
    /// </summary>
    [Command("UserInGroup")]
    public class CommandUserInGroup : CommandServer
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Группа
        /// </summary>
        public string Group
        {
            get { return (string)Parameters["Group"]; }
            set { Parameters["Group"] = value; }
        }

        #region Overrides of CommandServer

        protected internal override void InternalExecute()
        {
            if (Connection == null)
                return;

            if (string.IsNullOrWhiteSpace(Group))
                throw new ArgumentNullException("Group");

            // Пока существует одна группа
            SetResult(Group == "Admin" && Connection.IsAdminUser()); 
        }
        #endregion
    }
}
