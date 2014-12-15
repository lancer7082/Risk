using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Dynamic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;
using Risk;

namespace Finam.NativeConnect
{
    /// <summary>
    /// Подключение к серверу
    /// </summary>
    public class Connection : IExportConnection
    {
        private ConnectionState _state = ConnectionState.Closed;
        private DisconnectEvent disconnectEvent;
        private ConnectStateEvent connectStateEvent;
        private CommandEvent commandEvent;
        private MessageEvent messageEvent;
        private int instanceId;
        private IConnection connection;
        private DuplexChannelFactory<IConnection> factory;
        private string connectionString;
        private int reconnectCount = 5;
        private bool faultTolerant = true;
        private bool isTrace = false;
        public static Timer reconnectTimer;

        private string _serverName;
        private string _version;
        private string _userName;

        public string ConnectionId { get; private set; }
        private List<ExportDataSet> _activeDataSets = new List<ExportDataSet>();

        [ExportDll]
        [return: MarshalAs(UnmanagedType.LPWStr)]
        public static string CreateClient(int instatnceId, [MarshalAs(UnmanagedType.Interface)]out IExportConnection result)
        {
            result = null;
            try
            {
                result = new Connection(instatnceId);
                return null;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        static private void Configure()
        {
            AppDomain.CurrentDomain.SetData("APP_CONFIG_FILE", Assembly.GetExecutingAssembly().Location + ".config");
            var fiInit = typeof(System.Configuration.ConfigurationSettings).GetField("_configurationInitialized", BindingFlags.NonPublic | BindingFlags.Static);
            var fiSystem = typeof(System.Configuration.ConfigurationSettings).GetField("_configSystem", BindingFlags.NonPublic | BindingFlags.Static);
            if (fiInit != null && fiSystem != null)
            {
                fiInit.SetValue(null, false);
                fiSystem.SetValue(null, null);
            }
        }
        
        public ConnectionState State 
        {
            get
            {
                return _state;
            }

            private set
            {
                if (_state != value)
                {
                    _state = value;
                    if (connectStateEvent != null)
                        connectStateEvent(instanceId, (int)_state);
                }
            }
        }
       
        public Connection(int instatnceId)
        {
            this.instanceId = instatnceId;
            Configure();

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                {
                    throw (Exception)e.ExceptionObject;
                };
            // Environment.CurrentDirectory = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                try
                {
                    string assemblyPath = args.Name.IndexOf(",") >= 0 ? args.Name.Substring(0, args.Name.IndexOf(",")) : args.Name;
                    Assembly executingAssembly = Assembly.GetExecutingAssembly();
                    if (assemblyPath == null)
                        return null;
                    string tempFileName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar + assemblyPath + ".dll";
                    if (File.Exists(tempFileName))
                        return Assembly.LoadFrom(tempFileName);
                    tempFileName = Path.ChangeExtension(tempFileName, ".exe");
                    if (File.Exists(tempFileName))
                        return Assembly.LoadFrom(tempFileName);
                    return null;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            };
        }

        public void Dispose()
        {
            Disconnect();
        }

        #region Connection

        public void Connect(string connectionString)
        {
            this.connectionString = connectionString;

            InternalConnect();
        }

        private void InternalConnect()
        {
            // TODO: ??? Читать настройки из config
            var binding = new NetTcpBinding(SecurityMode.None);
            binding.MaxReceivedMessageSize = int.MaxValue;
            binding.ReceiveTimeout = new TimeSpan(0, 30, 0);
            binding.SendTimeout = new TimeSpan(0, 05, 0);
            var context = new InstanceContext(new MarshalConnectionCallback(this));
            factory = new DuplexChannelFactory<IConnection>(context, binding);
            var csBuilder = new ConnectionStringBuilder(connectionString);
            faultTolerant = csBuilder.FaultTolerant;
            isTrace = csBuilder.Trace;
            var server = new Uri(csBuilder.Uri, "Risk");
            EndpointAddress endpoint = new EndpointAddress(server);

            var channel = factory.CreateChannel(endpoint);
            ((ICommunicationObject)channel).Faulted += (s, e) => TryReconnect(s);
            try
            {
                var serverConnectionInfo = channel.Connect(csBuilder.UserID, csBuilder.Password, csBuilder.Options);
                ConnectionId = serverConnectionInfo.ConnectionId;
                _serverName = serverConnectionInfo.ServerName;
                _version = serverConnectionInfo.ServerVersion;
                _userName = serverConnectionInfo.UserName;
            }
            catch (EndpointNotFoundException)
            {
                throw new Exception(String.Format("Server not found '{0}' ", server.ToString()));
            }
            catch (Exception ex)
            {
                throw ex;
            }

            connection = channel;
            State = ConnectionState.Active;

            foreach (var command in _activeDataSets)
            {
                command.Open();
            }
            reconnectCount = 5;
        }

        private void InternalDisconnect()
        {
            try
            {
                if (connection != null)
                    connection.Disconnect();
            }
            catch { }
            finally
            {
                connection = null;
            }
            try
            {
                factory.Close();
            }
            catch { }
            finally
            {
                factory = null;
            }

            State = ConnectionState.Closed;
        }

        private void TryReconnect(object sender)
        {
            if (State == ConnectionState.Active)
            {
                if (faultTolerant && reconnectCount > 0)
                {
                    reconnectCount--;
                    IChannel channel = sender as IChannel;
                    if (channel != null)
                    {
                        channel.Abort();
                        channel.Close();
                        connection = null;
                    }

                    if (factory != null)
                    {
                        factory.Abort();
                        factory.Close();
                        factory = null;
                    }

                    State = ConnectionState.Reconnecting;

                    while (State == ConnectionState.Reconnecting)
                    {
                        try
                        {
                            InternalConnect();
                        }
                        catch
                        {
                            Thread.Sleep(5 * 1000);
                        }
                    }
                }
                else
                    Disconnect();
            }
        }

        public void Disconnect()
        {
            InternalDisconnect();
            if (disconnectEvent != null)
                disconnectEvent(instanceId);
        }

        public void CheckConnection()
        {
            if (State != ConnectionState.Active)
                throw new Exception("Not connected");
        }

        public string ServerName()
        {
            return  _serverName;
        }

        public string UserName()
        {
            return _userName;
        }

        public string Version()
        {
            return _version;
        }

        #endregion

        public void Trace(string message, params object[] args)
        {
            if (isTrace)
                ReceiveMessage(String.Format("{1:HH:mm:ss.fff} : {0}", String.Format(message, args), DateTime.Now), MessageType.Trace);
        }

        public void AddDataSet(ExportDataSet dataSet)
        {
            lock (_activeDataSets)
            {
                if (!_activeDataSets.Contains(dataSet))
                    _activeDataSets.Add(dataSet);
            }
        }

        public void RemoveDataSet(ExportDataSet dataSet)
        {
            lock (_activeDataSets)
            {
                if (_activeDataSets.Contains(dataSet))
                {
                    _activeDataSets.Remove(dataSet);
                }
            }
            if (dataSet.Notification)
            {
                Execute(new Command
                {
                    CommandText = "Unsubscribe",
                    CorrelationId = dataSet.CorrelationId,
                    Parameters = { new Parameter("ObjectName", dataSet.Text) }
                });
            }
        }

        public CommandResult Execute(Command command)
        {
            try
            {
                Trace("Send command '{0}' to server", command);
                CheckConnection();
                return connection.Execute(command);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                Trace("Receive command '{0}' result from server", command);
            }
        }

        public void ReceiveCommand(Command command)
        {
            try
            {
                lock (_activeDataSets)
                {
                    var sourceCommand = _activeDataSets.FirstOrDefault(c => c.CorrelationId == command.CorrelationId);
                    if (sourceCommand != null)
                        sourceCommand.ReceiveCommand(command);
                    else if (commandEvent != null && (String.IsNullOrEmpty(command.CorrelationId) || command.CorrelationId == ConnectionId)) // Command correlationId for connection must be with connection Id
                        commandEvent(instanceId, new ExportCommand(this, command, -1));
                }
            }
            catch
            {
                throw;
            }
        }

        public void ReceiveMessage(string message, MessageType messageType)
        {
            if (messageEvent != null)
                messageEvent(instanceId, message, (int)messageType);
        }

        public IExportCommand CreateCommand(int instanceId)
        {
            return new ExportCommand(this, new Command(), instanceId);
        }

        public IExportDataSet CreateDataSet(int instanceId)
        {
            return new ExportDataSet(this, instanceId);
        }

        public void OnDisconnect(DisconnectEvent disconnectEvent)
        {
            this.disconnectEvent += disconnectEvent;
        }

        public void OnStateChanged(ConnectStateEvent connectStateEvent)
        {
            this.connectStateEvent += connectStateEvent;
        }       

        public void OnMessage(MessageEvent messageEvent)
        {
            this.messageEvent += messageEvent;
        }

        public void OnCommand(CommandEvent commandEvent)
        {
            this.commandEvent += commandEvent;
        }
    }
}