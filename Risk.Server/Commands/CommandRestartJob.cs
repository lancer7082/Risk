using System;

namespace Risk.Commands
{
    /// <summary>
    /// Команда 
    /// </summary>
    [Command("RestartJob")]
    public class CommandRestartJob : CommandServer
    {
        #region Overrides of CommandServer

        protected internal override void InternalExecute()
        {
            string jobName;
            try
            {
                jobName = Parameters["JobName"].ToString();
            }
            catch
            {
                throw new Exception("Ошибка в параметрах");
            }
            
            ServerBase.Current.JobManager.RestartJob(jobName);
        }
        #endregion
    }
}
