using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Finam.NativeConnect
{
    public class TableKeyActivator
    {
        private int[] propSizes;
        private List<PropertyInfo> properties;

        public int KeySize { get; private set; }

        public TableKeyActivator(Type type, string keyFields)
        {
            if (type == null || String.IsNullOrWhiteSpace(keyFields))
            {
                KeySize = 0;
                return;
                // TODO: ??? throw new Exception("Empty key fields");
            }

            // Get key field properties
            properties = new List<PropertyInfo>();
            foreach (var fieldStr in keyFields.Split(','))
            {
                var fieldName = fieldStr.Trim();

                var property = type.GetProperties().FirstOrDefault(p => p.Name == fieldName);
                if (property == null)
                    throw new Exception(String.Format("Invalid key field '{0}' for object '{1}'", fieldName, type.Name));

                properties.Add(property);
            }

            // Calculate key fields size
            propSizes = new int[properties.Count()];
            int index = 0;
            foreach (var prop in properties)
            {
                if (!prop.PropertyType.IsValueType && prop.PropertyType != typeof(string))
                    throw new Exception(String.Format("Key field must be contains only value-type properties, type '{0}' not supported", prop.PropertyType.Name));

                int propSize;
                if (prop.PropertyType.IsEnum)
                    propSize = (byte)Marshal.SizeOf(Enum.GetUnderlyingType(prop.PropertyType));
                else if (prop.PropertyType.IsValueType)
                    propSize = (byte)Marshal.SizeOf(prop.PropertyType);
                else if (prop.PropertyType == typeof(string))
                {
                    var attr = prop.GetCustomAttribute<StringLengthAttribute>(true);
                    if (attr != null)
                        propSize = attr.MaximumLength;
                    else
                        throw new Exception(String.Format("String key field '{0}' type '{1}' must be set StringLength attribute", prop.Name, "unknown"));
                }
                else
                    throw new Exception(String.Format("Unsupported property type '{0}'", prop.PropertyType.Name));

                propSizes[index] = propSize;
                KeySize += propSize;
                index++;
            }
        }

        public TableKey Create(object data, int recordIndex)
        {
           // if (KeySize == 0) // If KeyFields is empty, create key on recordIndex
                return new TableKey(BitConverter.GetBytes(recordIndex));
            //else
            //    return Create(data);
        }

        private TableKey Create(object data)
        {
            if (KeySize == 0)
                throw new Exception("Key field is empty");

            var result = new byte[KeySize];
            // Array.Clear(result, 0, KeySize); // TODO: ???
            int index = 0;
            int offset = 0;
            foreach (var prop in properties)
            {
                byte[] propData = null;
                if (prop.PropertyType == typeof(int))
                    propData = BitConverter.GetBytes((int)prop.GetValue(data));
                else
                    if (prop.PropertyType == typeof(string))
                    {
                        var valueStr = (string)prop.GetValue(data);
                        Encoding.UTF8.GetBytes(valueStr, 0, valueStr.Length > propSizes[index] ? propSizes[index] : valueStr.Length, result, offset);
                    }
                    else if (prop.PropertyType == typeof(bool))
                        propData = BitConverter.GetBytes((bool)prop.GetValue(data));
                    else if (prop.PropertyType == typeof(char))
                        propData = BitConverter.GetBytes((char)prop.GetValue(data));
                    else if (prop.PropertyType == typeof(double))
                        propData = BitConverter.GetBytes((double)prop.GetValue(data));
                    else if (prop.PropertyType == typeof(float))
                        propData = BitConverter.GetBytes((float)prop.GetValue(data));
                    else if (prop.PropertyType == typeof(long))
                        propData = BitConverter.GetBytes((long)prop.GetValue(data));
                    else if (prop.PropertyType == typeof(short))
                        propData = BitConverter.GetBytes((short)prop.GetValue(data));
                    else if (prop.PropertyType == typeof(uint))
                        propData = BitConverter.GetBytes((uint)prop.GetValue(data));
                    else if (prop.PropertyType == typeof(ulong))
                        propData = BitConverter.GetBytes((ulong)prop.GetValue(data));
                    else if (prop.PropertyType == typeof(ushort))
                        propData = BitConverter.GetBytes((ushort)prop.GetValue(data));
                    else
                        throw new Exception(String.Format("Unsupported property type '{0}'", prop.PropertyType.Name));

                if (propData != null)
                    Buffer.BlockCopy(propData, 0, result, offset, propSizes[index]);
                offset += propSizes[index];
                index++;
            }
            return new TableKey(result);
        }
    }
}
