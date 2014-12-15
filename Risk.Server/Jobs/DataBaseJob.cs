using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;

namespace Risk.Jobs
{

    /// <summary>
    /// Класс джоба работы с БД
    /// </summary>
    public class DatabaseJob : JobBase
    {
        /// <summary>
        /// Имя таблицы
        /// </summary>
        public string DataObjectName { get; set; }

        /// <summary>
        /// Поля таблицы
        /// </summary>
        public string DataObjectFields { get; set; }

        /// <summary>
        /// Имя команды
        /// </summary>
        public string CommandName { get; set; }

        /// <summary>
        /// Строка подключения
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Строка подключения
        /// </summary>
        public string ConnectionStringName { get; set; }

        /// <summary>
        /// Таймаут команды
        /// </summary>
        public int DatabaseCommandTimeout { get; set; }

        /// <summary>
        /// Имя хранимки
        /// </summary>
        private string _storedProcedureName;

        /// <summary>
        /// Имя хранимки
        /// </summary>
        public string StoredProcedureName
        {
            get { return _storedProcedureName; }
            set
            {
                if (value != _storedProcedureName)  // очистка _detectedStoredProcedureParameters
                    _detectedStoredProcedureParameters = null;

                _storedProcedureName = value;
            }
        }

        /// <summary>
        /// Параметры хранимки
        /// </summary>
        public Dictionary<string, object> StoredProcedureParameters { get; private set; }

        /// <summary>
        /// Параметры хранимки, полученные из БД
        /// </summary>
        private List<SqlParameter> _detectedStoredProcedureParameters;

        /// <summary>
        /// Блокировка ресурсов для синхронизации
        /// </summary>
        public object Locker = new object();

        /// <summary>
        /// Признак конфигурирования джоба
        /// </summary>
        public bool IsConfiguring { get; set; }

        /// <summary>
        /// Конструктор
        /// </summary>
        public DatabaseJob()
        {
            StoredProcedureParameters = new Dictionary<string, object>();
        }

        #region Overrides of JobBase

        /// <summary>
        /// Тело выполняемого по таймеру метода
        /// </summary>
        /// <param name="data"></param>
        protected override void DoWork(object data)
        {
            if (IsConfiguring)
                return;

            lock (Locker)
            {
                if (!Enabled)
                    return;

                if (IsConfiguring)
                {
                    Log.Info("Configuring job {0} ", Name);
                    return;
                }

                //логирование и обработка исключений
                Log.Info("Executing job {0} ", Name);
                try
                {
                    bool success;

                    // вызов метода конктретного джоба
                    ExecuteConcreteJob(out success);

                    Log.Info(success ? "Job {0} completed successfully" : "Job {0} failed", Name);
                }
                catch (Exception e)
                {
                    Log.ErrorException(string.Format("Job {0} execution error: {1}", Name, e.Message), e);
                }
            }
        }

        #endregion

        /// <summary>
        /// Непосредственное выполнение джоба
        /// </summary>
        protected void ExecuteConcreteJob(out bool success)
        {
            success = false;

            // поиск объекта в таблицах по имени
            var dataObject = ServerBase.Current.FindDataObject(DataObjectName);

            if (dataObject == null)
            {
                WriteErrorLog(GetEmptyStringIfNull(DataObjectName) + " data object not found");
                return;
            }

            // загрузка данных из БД
            var data = LoadDataFromDatabase(dataObject);

            if (data == null)
            {
                WriteErrorLog("Can't load data with stored procedure " + GetEmptyStringIfNull(StoredProcedureName));
                return;
            }

            // Выполнение команды
            CreateAndExecuteServerCommand(dataObject, data, out success);

            if (!success)
                WriteErrorLog("Can't create and execute command " + GetEmptyStringIfNull(CommandName));
        }

        /// <summary>
        /// Создание и выполнение серверной команды
        /// </summary>
        /// <param name="dataObject">таблица</param>
        /// <param name="data">данные</param>
        /// <param name="success">признак успешного выполнения</param>
        private void CreateAndExecuteServerCommand(IDataObject dataObject, IEnumerable<object> data, out bool success)
        {
            success = true;
            try
            {
                // получаем полное название типа команды по имени
                var fullCommandTypeName = Server.Current.Commands[CommandName].FullName;

                // создаем объект команды
                dynamic serverCommand = Assembly.GetExecutingAssembly().CreateInstance(fullCommandTypeName);

                // заполняем данные команды и выполняем ее
                serverCommand.Object = dataObject;
                serverCommand.Data = data;
                if (!string.IsNullOrEmpty(DataObjectFields))
                    serverCommand.Fields = DataObjectFields;

                serverCommand.Execute();
            }
            catch (Exception e)
            {
                WriteErrorLog(e.ToString());
                success = false;
            }
        }

        /// <summary>
        /// Загрузка данных их БД
        /// </summary>
        /// <param name="dataObject">таблица</param>
        /// <returns></returns>
        private IEnumerable<object> LoadDataFromDatabase(IDataObject dataObject)
        {
            try
            {
                // создаем конекшн к БД, заполняем параметры хранимки и выполняем ее
                IEnumerable<object> data;
                using (var connection = new SqlConnection(ConnectionString))
                {
                    using (var sqlCommand = new SqlCommand("", connection))
                    {
                        if (DatabaseCommandTimeout > 0)
                            sqlCommand.CommandTimeout = DatabaseCommandTimeout;

                        connection.Open();

                        sqlCommand.CommandType = System.Data.CommandType.StoredProcedure;

                        sqlCommand.CommandText = StoredProcedureName;

                        // определения параметров вызова хранимки
                        DetectStoredProcedureParameters(connection, StoredProcedureName);

                        // Применение параметров к процедуре
                        ApplyStoredProcedureParameters(sqlCommand);

                        var reader = sqlCommand.ExecuteReader();

                        // конвертация данных в объектный тип
                        data = GetObjectDataFromReader(dataObject.ObjectType, reader).ToList();
                    }
                }
                return data;
            }
            catch (Exception e)
            {
                WriteErrorLog(e.ToString());
                return null;
            }
        }

        /// <summary>
        /// Определения параметров вызова хранимки
        /// </summary>
        /// <param name="connection">подключение</param>
        /// <param name="storedProcedureName">имя хранимки</param>
        private void DetectStoredProcedureParameters(SqlConnection connection, String storedProcedureName)
        {
            // проверка _detectedSPParameters, т.к. обращение к БД выполняется только один раз
            if (_detectedStoredProcedureParameters != null)
                return;

            _detectedStoredProcedureParameters = new List<SqlParameter>();
            var cmd = new SqlCommand(storedProcedureName, connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            // получение и сохранение параметро вызова хранимки
            SqlCommandBuilder.DeriveParameters(cmd);

            foreach (SqlParameter parameter in cmd.Parameters)
            {
                if (parameter.Direction == ParameterDirection.Input || parameter.Direction == ParameterDirection.InputOutput)
                    _detectedStoredProcedureParameters.Add(parameter);
            }
        }

        /// <summary>
        /// Применение параметров к процедуре
        /// </summary>
        /// <param name="command">Хранимка</param>
        /// <returns></returns>
        private void ApplyStoredProcedureParameters(SqlCommand command)
        {
            if (_detectedStoredProcedureParameters == null || !_detectedStoredProcedureParameters.Any())
                return;

            // проходим по всем параметрам хранимки, переданных в StoredProcedureParameters
            // получаем их реальные значения и приводим к типу, указанному в _detectedSPParameters
            foreach (var parameter in StoredProcedureParameters)
            {
                // берем имя параметра
                var sqlParametr = new SqlParameter
                {
                    ParameterName = parameter.Key
                };

                // если параметр строковый, то парсим его
                if (parameter.Value is string)
                {
                    sqlParametr.Value = ParseStringStoredProcedureParameter(parameter.Value as string);
                }
                // если нет - то просто копируем это значение в параметр хранимки
                else
                {
                    sqlParametr.Value = parameter.Value;
                }

                if (sqlParametr.Value == null)
                {
                    WriteErrorLog("Can't load stored procedure parameter " + GetEmptyStringIfNull(parameter.Key));
                    continue;
                }

                sqlParametr.ParameterName = "@" + sqlParametr.ParameterName;

                // ищем соответсвующий параметр вызова хранимки и копируем его тип 
                var detectedSqlParameter = _detectedStoredProcedureParameters.FirstOrDefault(s => s.ParameterName == sqlParametr.ParameterName);
                if (detectedSqlParameter != null)
                {
                    sqlParametr.DbType = detectedSqlParameter.DbType;
                    command.Parameters.Add(sqlParametr);
                }
            }
        }

        /// <summary>
        /// Парсинг строкового параметра хранимки
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        private object ParseStringStoredProcedureParameter(string parameter)
        {
            if (string.IsNullOrEmpty(parameter))
                return parameter;

            //если значение параметра заключено в символы %%, то это значение необходимо вычислить
            if (!parameter.StartsWith("%"))
                return parameter;

            // убираем %%
            var valueStr = parameter.TrimEnd('%').TrimStart('%');

            // сплиттим
            var splittedValue = valueStr.Split('.');

            object value = null;

            // если в сплите только один элемент, то берем значение свойства  у текущего класса
            if (splittedValue.Length == 1)
            {
                value = GetPropertyValue(this, valueStr);
            }

            // если в сплите два элемента, то берем значение свойства, указанного после разделителя у таблицы,
            // указанной до разделителя
            else if (splittedValue.Length == 2)
            {
                // ищем таблицу
                var dataObject = Server.Current.FindDataObject(splittedValue[0]);

                // таблица не может быть с несколькими строками - расчет на таблицу Settings и подобные
                if (dataObject is IEnumerable)
                {
                    WriteErrorLog("Stored procedure parametr can't be IEnumerable");
                    return null;
                }

                // получение объекта с данными таблицы
                var dataObjectData = dataObject.GetData(null);

                // получаем значение свойста 
                value = GetPropertyValue(dataObjectData, splittedValue[1]);
            }
            return value;
        }

        /// <summary>
        /// Заполняет поле propertyName в объекте  @object значением propertyValue
        /// </summary>
        /// <param name="object"></param>
        /// <param name="propertyName"></param>
        /// <param name="propertyValue"></param>
        public static void FillObjectWithProperty(ref object @object, string propertyName, object propertyValue)
        {
            var objectType = @object.GetType();
            if (objectType != null)
            {
                var propInfo = objectType.GetProperty(propertyName);
                if (propInfo != null) propInfo.SetValue(@object, propertyValue);
            }
        }

        /// <summary>
        /// Получает объектное представление DbDataReader
        /// </summary>
        /// <param name="objectType">Тип к которому приводятся записи в ридере</param>
        /// <param name="reader">ридер</param>
        /// <returns></returns>
        public static IEnumerable<object> GetObjectDataFromReader(Type objectType, DbDataReader reader)
        {
            var objects = new List<Object>();

            // читаем датаридер
            while (reader.Read())
            {
                // создаем инстанс объекта указанного типа
                var instance = Activator.CreateInstance(objectType);

                //проходим по колонкам ридера
                foreach (DataRow dataRow in reader.GetSchemaTable().Rows)
                {
                    //в первой колонке ItemArray находится имя колонки
                    var columnName = dataRow.ItemArray[0].ToString();

                    // пустые записи не копируем
                    if (reader[columnName] is DBNull)
                        continue;

                    // заполняем поле в инстансе значением из колонки
                    FillObjectWithProperty(ref instance, columnName, reader[dataRow.ItemArray[0].ToString()]);
                }
                objects.Add(instance);
            }

            return objects;
        }

        /// <summary>
        /// Получает значение свойства propertyName в объекте @object
        /// </summary>
        /// <param name="object"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public object GetPropertyValue(object @object, string propertyName)
        {
            try
            {
                return @object.GetType().GetProperties()
                    .Single(p => p.Name == propertyName)
                    .GetValue(@object, null);
            }
            catch
            {
                return null;
            }
        }
    }
}
