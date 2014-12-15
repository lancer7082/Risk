namespace Risk.Commands
{
    /// <summary>
    /// Команда сверки
    /// </summary>
    [Command("SaveConfigurationToConfig")]
    public class CommandSaveConfigurationToConfig : CommandServer
    {
        #region Overrides of CommandServer

        protected internal override void InternalExecute()
        {
            Server.Current.ServerConfigurationSection.UpdateConfigFromObject();
        }

        #endregion
    }
}
