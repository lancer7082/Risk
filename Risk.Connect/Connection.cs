using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;

namespace Risk
{
    public class Connection : IExport
    {
        [ExportDll]
        [return: MarshalAs(UnmanagedType.LPWStr)]
        public static string CreateClient(int instatnceId, [MarshalAs(UnmanagedType.Interface)]out IExport result)
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
            FieldInfo fiInit = typeof(System.Configuration.ConfigurationSettings).GetField("_configurationInitialized", BindingFlags.NonPublic | BindingFlags.Static);
            FieldInfo fiSystem = typeof(System.Configuration.ConfigurationSettings).GetField("_configSystem", BindingFlags.NonPublic | BindingFlags.Static);
            if (fiInit != null && fiSystem != null)
            {
                fiInit.SetValue(null, false);
                fiSystem.SetValue(null, null);
            }
        }

        private ConnectionState _state = ConnectionState.Closed;
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
        
        private DisconnectEvent disconnectEvent;
        private ConnectStateEvent connectStateEvent;
        private ReceiveEvent receiveEvent;
        private MessageEvent messageEvent;
        private int instanceId;
        private List<DataSet> commands = new List<DataSet>();

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

        public string Id { get; private set; }

        private IConnection connection;
        private DuplexChannelFactory<IConnection> factory;
        private string connectionString;
        private bool faultTolerant = true;
        public static Timer reconnectTimer;

        #region Connection

        public void Connect(string connectionString)
        {
            this.connectionString = connectionString;

            InternalConnect();
        }

        private void InternalConnect()
        {
            var binding = new NetTcpBinding(SecurityMode.None);
            binding.MaxReceivedMessageSize = int.MaxValue;

            var context = new InstanceContext(new MarshalConnectionCallback(this));
            factory = new DuplexChannelFactory<IConnection>(context, binding);

            var csBuilder = new ConnectionStringBuilder(connectionString);
            faultTolerant = csBuilder.FaultTolerant;
            var server = new Uri(csBuilder.Uri, "Risk");
            EndpointAddress endpoint = new EndpointAddress(server);

            var channel = factory.CreateChannel(endpoint);
            ((ICommunicationObject)channel).Faulted += (s, e) => TryReconnect(s);
            try
            {
                Id = channel.Connect(csBuilder.UserID, csBuilder.Password, Id);
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

            foreach (var command in commands)
            {
                OpenCommand(command);
            }
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
                if (faultTolerant)
                {
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

        #endregion

        public object Execute(Command command)
        {
            CheckConnection();
            return connection.Execute(command);
        }

        public T Execute<T>(Command command)
        {
            return (T)Execute(command);
        }

        public string Version()
        {
            return Execute<string>(new Command { Type = CommandType.Select, Text = "@@Version" });
        }

        public void SendMessage(string message)
        {
            // TODO: ??? connection.SendMessage(message);
        }

        public void ReceiveMessage(string message)
        {
            if (messageEvent != null)
                messageEvent(instanceId, message);
        }

        public void ReceiveCommand(Command command)
        {
            var dataSet = new DataSet(this, instanceId, command);
            var sourceCommand = commands.FirstOrDefault(c => c.CorrelationId == command.CorrelationId);
            if (sourceCommand != null)
                sourceCommand.ReceiveCommand(dataSet);
            else if (receiveEvent != null &&  (String.IsNullOrEmpty(command.CorrelationId) || command.CorrelationId == Id)) // Command correlationId for connection must be with connection Id
                receiveEvent(instanceId, dataSet);
        }

        public IDataSet CreateCommand(int instanceId)
        {
            return new DataSet(this, instanceId);
        }

        public void OpenCommand(IDataSet command)
        {
            var commandInstance = command as DataSet;
            if (commandInstance == null)
                throw new Exception("Command not initialized");

            commandInstance.NewCorrelationId();  // For ignore old notification
            
            if (!commands.Contains(commandInstance))
                commands.Add(commandInstance);

            commandInstance.Active = true;
            try
            {
                if (commandInstance.Notification)
                    Execute(new Command { Type = CommandType.Subscribe, Text = commandInstance.Text, CorrelationId = commandInstance.CorrelationId });
                else
                    Execute(new Command { Type = CommandType.Select, Text = commandInstance.Text, CorrelationId = commandInstance.CorrelationId });
            }
            catch (Exception ex)
            {
                commands.Remove(commandInstance);
                throw ex;
            }
        }

        public void CloseCommand(IDataSet command)
        {
            var commandInstance = command as DataSet;
            if (commandInstance == null)
                throw new Exception("Command not initialized");

            commandInstance.Active = false;

            if (commands.Contains(commandInstance))
            {
                commands.Remove(commandInstance);
                if (connection != null && commandInstance.Notification)
                    Execute(new Command { Type = CommandType.Unsubscribe, Text = commandInstance.Text, CorrelationId = commandInstance.CorrelationId });
            }

            commandInstance.Dispose();
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

        public void OnReceive(ReceiveEvent receiveEvent)
        {
            this.receiveEvent += receiveEvent;
        }
    }

    public delegate void DisconnectEvent(int instatnceId);
    public delegate void ConnectStateEvent(int instatnceId, int state);
    public delegate void ReceiveEvent(int instatnceId, IDataSet command);
    public delegate void MessageEvent(int instatnceId, [MarshalAs(UnmanagedType.LPWStr)]string message);

    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IExport
    {
        [return: MarshalAs(UnmanagedType.LPWStr)]
        string Version();

        void Connect([MarshalAs(UnmanagedType.LPWStr)]string connectionString);
        void Disconnect();
        void OnDisconnect([MarshalAs(UnmanagedType.FunctionPtr)]DisconnectEvent disconnectEvent);
        void OnStateChanged([MarshalAs(UnmanagedType.FunctionPtr)]ConnectStateEvent connectStateEvent);

        void SendMessage([MarshalAs(UnmanagedType.LPWStr)]string message);
        void OnMessage([MarshalAs(UnmanagedType.FunctionPtr)]MessageEvent messageEvent);

        [return: MarshalAs(UnmanagedType.Interface)]
        IDataSet CreateCommand(int instanceId);

        void OpenCommand([MarshalAs(UnmanagedType.Interface)]IDataSet Command);
        void CloseCommand([MarshalAs(UnmanagedType.Interface)]IDataSet Command);

        void OnReceive([MarshalAs(UnmanagedType.FunctionPtr)]ReceiveEvent receiveEvent);
    }
}