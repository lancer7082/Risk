using System.Configuration;
using System.Xml.Linq;
using Finam.Configuration;

namespace Risk.Configuration
{
    /// <summary>
    /// Элемент конфигурации расширений
    /// </summary>
    public class AddinConfigurationElement : UnrecognizedConfigurationElement<Server>
    {

        /// <summary>
        /// Имя
        /// </summary>
        [ConfigurationProperty("name", IsRequired = true)]
        public string Name
        {
            get { return (string)this["name"]; }
            set { this["name"] = value; }
        }

        /// <summary>
        /// Type
        /// </summary>
        [ConfigurationProperty("type", IsRequired = true)]
        public string Type
        {
            get { return (string)this["type"]; }
            set { this["type"] = value; }
        }

        /// <summary>
        /// Enabled
        /// </summary>
        [ConfigurationProperty("enabled", DefaultValue = true)]
        public bool Enabled
        {
            get { return (bool)this["enabled"]; }
            set { this["enabled"] = value; }
        }

        #region Overrides of ConfigurationElementBase<Server>

        /// <summary>
        /// Применяет конфиг к objectToConfigure
        /// </summary>
        /// <param name="objectToConfigure"></param>
        public override void ApplyConfigToObject(Server objectToConfigure)
        {
            AddIns.AddInProxy addInProxy;
            var addInExists = true;

            try
            {
                // выбираем нужное расширение
                addInProxy = objectToConfigure.AddIns[Name];
            }
            catch
            {
                // если иксепшн, то это значит, что расширения вообще нет в списке
                addInProxy = null;
                addInExists = false;
            }

            var strUnrecognizedData = UnrecognizedData != null ? UnrecognizedData.ToString() : null;

            // если прокси расширения пустой, т.е. расширение либо незарегистрировано, либо остановлено
            if (addInProxy == null)
            {
                // если расширение разрешено в конфиге
                if (Enabled)
                {
                    // если расширения еще нет в списке расширений, то его нужно сначала зарегистрировать
                    if (!addInExists)
                    {
                        objectToConfigure.AddIns.Register(Name, configuration: strUnrecognizedData);
                        
                        // если ранее сервер уже запускал все расширения, то это новое для сервера расширение нужно запустить отсюда
                        if (objectToConfigure.AddIns.AllStarted)
                            objectToConfigure.AddIns.Start(Name);
                    }
                    else
                        objectToConfigure.AddIns.Start(Name); // если уже есть в списке, то это значит оно выключено и его нужно запустить
                }
            }
            else  // прокси непустой - расширение активно
            {
                if (Enabled)  // если расширение разрешено в конфиге
                {
                    addInProxy.Configure(strUnrecognizedData);  // применяем конфигурацию
                }
                else  // расширение было выключено
                {
                    objectToConfigure.AddIns.Stop(Name);  // останавливаем расширение
                }
            }
        }

        /// <summary>
        /// Обновляет конфиг из objectToConfigure
        /// </summary>
        /// <param name="objectToConfigure"></param>
        public override void UpdateConfigFromObject(Server objectToConfigure)
        {
            var unrecognizedData = objectToConfigure.AddIns[Name].GetConfiguration();
            UnrecognizedData = unrecognizedData != null ? XElement.Parse(unrecognizedData) : null;
        }

        #endregion
    }
}