using System;
using System.Data.SqlTypes;

namespace Risk
{
    /// <summary>
    /// Параметр для команды
    /// </summary>
    [Serializable]
    public class Parameter
    {
        public string Name { get; set; }
        public object Value { get; set; }
        // public SqlDbType Type { get; set; }

        public bool IsNull
        {
            get
            {
                if (Value == null || Value is DBNull) return true;
                if (Value is INullable) return ((INullable)Value).IsNull;
                return false;
            }
        }

        public Parameter()
        {
        }

        public Parameter(string name, object value)
        {
            this.Name = name;
            this.Value = value;
        }

        //public static SqlDbType GetSqlType(Object value)
        //{
        //    if (value is SqlByte) return SqlDbType.TinyInt; // TinyInt
        //    if (value is SqlInt16) return SqlDbType.SmallInt; // SmallInt
        //    if (value is SqlInt32) return SqlDbType.Int; // Int32
        //    if (value is SqlInt64) return SqlDbType.BigInt;   // BigInt
        //    if (value is SqlBytes) return SqlDbType.VarBinary;  // Binary, VarBinary
        //    if (value is SqlBoolean) return SqlDbType.Bit;    // Bit
        //    if (value is SqlString) return SqlDbType.VarChar; // Char, NChar, VarChar, NVarChar
        //    if (value is DateTime && ((DateTime)value) == ((DateTime)value).Date) return SqlDbType.Date; // Date
        //    if (value is DateTime) return SqlDbType.DateTime2; // DateTime2
        //    if (value is SqlDateTime) return SqlDbType.DateTime; // DateTime, SmallDateTime
        //    if (value is TimeSpan) return SqlDbType.Time; // Time
        //    // if (value is System.DateTimeOffset) return SqlDbType.DateTimeOffset; // DateTimeOffset ??? Не понятно как передать этот тип обратно на сервер ???
        //    if (value is SqlDecimal) return SqlDbType.Decimal; // Numeric, Decimal
        //    if (value is SqlDouble) return SqlDbType.Float; // Float
        //    if (value is SqlMoney) return SqlDbType.Money; // Money
        //    if (value is SqlSingle) return SqlDbType.Real; // Real
        //    if (value is SqlGuid) return SqlDbType.UniqueIdentifier; // UniqueIdentifier
        //    if (value is SqlXml) return SqlDbType.Xml; // UniqueIdentifier
        //    else
        //        throw new Exception(String.Format("Params not supported type '{0}'", value.GetType().Name));
        //}

        //[SqlMethod(OnNullCall = true, InvokeIfReceiverIsNull = true)]
        //public void Read(BinaryReader r)
        //{
        //    string name = r.ReadString();
        //    SqlDbType type = (SqlDbType)r.ReadInt16();
        //    Object value = null;
        //    Int32 len; Int32 lcid; SqlCompareOptions co;
        //    switch (type)
        //    {
        //        case SqlDbType.BigInt: value = new SqlInt64(r.ReadInt64()); break;
        //        case SqlDbType.Binary: len = r.ReadInt32(); value = new SqlBytes(r.ReadBytes(len)); break;
        //        case SqlDbType.Bit: value = new SqlBoolean(r.ReadBoolean()); break;
        //        case SqlDbType.Char: co = (SqlCompareOptions)r.ReadInt32();
        //            lcid = r.ReadInt32();
        //            value = new SqlString(r.ReadString(), lcid, co);
        //            break;
        //        case SqlDbType.Date: value = DateTime.FromBinary(r.ReadInt64()); break;
        //        case SqlDbType.DateTime: value = new SqlDateTime(DateTime.FromBinary(r.ReadInt64())); break;
        //        // Not support SqlDbType.DateTimeOffset
        //        case SqlDbType.DateTime2: value = DateTime.FromBinary(r.ReadInt64()); break;
        //        case SqlDbType.Decimal: value = new SqlDecimal(r.ReadDecimal()); break;
        //        case SqlDbType.Float: value = new SqlDouble(r.ReadDouble()); break;
        //        // Not support SqlDbType.Image
        //        case SqlDbType.Int: value = new SqlInt32((Int32)r.ReadInt32()); break;
        //        case SqlDbType.Money: value = new SqlMoney(r.ReadDecimal()); break;
        //        case SqlDbType.NChar: co = (System.Data.SqlTypes.SqlCompareOptions)r.ReadInt32();
        //            lcid = r.ReadInt32();
        //            value = new SqlString(r.ReadString(), lcid, co);
        //            break;
        //        // Not support SqlDbType.NText
        //        case SqlDbType.NVarChar: co = (SqlCompareOptions)r.ReadInt32();
        //            lcid = r.ReadInt32();
        //            value = new SqlString(r.ReadString(), lcid, co);
        //            break;
        //        case SqlDbType.Real: value = new SqlSingle(r.ReadDouble()); break;
        //        case SqlDbType.SmallDateTime: value = DateTime.FromBinary(r.ReadInt64()); break;
        //        case SqlDbType.SmallInt: value = new SqlInt16((Int16)r.ReadInt16()); break;
        //        case SqlDbType.SmallMoney: value = new SqlMoney(r.ReadDecimal()); break;
        //        // Not support SqlDbType.Structured
        //        // Not support SqlDbType.Text
        //        case SqlDbType.Time: value = TimeSpan.FromTicks(r.ReadInt64()); break;
        //        // Not support SqlDbType.Timestamp
        //        case SqlDbType.TinyInt: value = new SqlByte(r.ReadByte()); break;
        //        case SqlDbType.Udt:
        //            // TODO: Пока поддержа только SqlDbParams
        //            // string assemblyName = r.ReadString();
        //            // string typeName = r.ReadString();
        //            // value = System.Activator.CreateInstance(assemblyName, typeName).Unwrap();
        //            //if (value is IBinarySerialize)
        //            //  (value as IBinarySerialize).Read(r);
        //            //else
        //            //    throw new Exception(String.Format("Невозможно прочитать данные, тип UDT '{0}' не поддерживается текущей версией {1}", value.GetType().Name, this.GetType().Name));
        //            value = System.Activator.CreateInstance(this.GetType());
        //            (value as IBinarySerialize).Read(r);
        //            break;
        //        case SqlDbType.UniqueIdentifier: value = new SqlGuid(r.ReadString()); break;
        //        case SqlDbType.VarBinary: len = r.ReadInt32(); value = new SqlBytes(r.ReadBytes(len)); break;
        //        case SqlDbType.VarChar: co = (SqlCompareOptions)r.ReadInt32();
        //            lcid = r.ReadInt32();
        //            value = new SqlString(r.ReadString(), lcid, co);
        //            break;
        //        // Not support SqlDbType.Variant
        //        case SqlDbType.Xml:
        //            XmlReader rXml = XmlReader.Create(new System.IO.StringReader(r.ReadString()));
        //            value = new SqlXml(rXml);
        //            break;
        //        default:
        //            throw new Exception(String.Format("Params not supported type '{0}'", type.ToString()));
        //    }
        //    Value = value;
        //    Type = type;
        //    Name = name;
        //}

        //[SqlMethod(Name = "Write", IsMutator = false, OnNullCall = true, InvokeIfReceiverIsNull = true, IsDeterministic = true)]
        //public void Write(BinaryWriter w)
        //{
        //    w.Write(Name);
        //    SqlDbType type = GetSqlType(Value);
        //    w.Write((Int16)type);

        //    switch (Type)
        //    {
        //        case SqlDbType.BigInt: w.Write((Int64)((SqlInt64)Value)); break;
        //        case SqlDbType.Binary: w.Write((Int32)((SqlBytes)Value).Length); w.Write(((SqlBytes)Value).Buffer, 0, (Int32)((SqlBytes)Value).Length); break;
        //        case SqlDbType.Bit: w.Write((bool)((SqlBoolean)Value)); break;
        //        case SqlDbType.Char:
        //            SqlString Char = (SqlString)Value;
        //            w.Write((Int32)Char.SqlCompareOptions);
        //            w.Write(Char.LCID);
        //            w.Write(Char.Value);
        //            break;
        //        case SqlDbType.Date: w.Write((Int64)((DateTime)Value).ToBinary()); break;
        //        case SqlDbType.DateTime: w.Write((Int64)((DateTime)((SqlDateTime)Value).Value).ToBinary()); break;
        //        // Not support SqlDbType.DateTimeOffset
        //        case SqlDbType.DateTime2: w.Write((Int64)((DateTime)Value).ToBinary()); break;
        //        case SqlDbType.Decimal: w.Write(((SqlDecimal)Value).Value); break;
        //        case SqlDbType.Float: w.Write(((SqlDouble)Value).Value); break;
        //        // Not support SqlDbType.Image
        //        case SqlDbType.Int: w.Write((Int32)((SqlInt32)Value).Value); break;
        //        case SqlDbType.Money: w.Write(((SqlMoney)Value).Value); break;
        //        case SqlDbType.NChar:
        //            SqlString NChar = (SqlString)Value;
        //            w.Write((Int32)NChar.SqlCompareOptions);
        //            w.Write(NChar.LCID);
        //            w.Write(NChar.Value);
        //            break;
        //        // Not support SqlDbType.NText
        //        case SqlDbType.NVarChar:
        //            SqlString NVarChar = (SqlString)Value;
        //            w.Write((Int32)NVarChar.SqlCompareOptions);
        //            w.Write(NVarChar.LCID);
        //            w.Write(NVarChar.Value);
        //            break;
        //        case SqlDbType.Real: w.Write((Double)((SqlSingle)Value).Value); break;
        //        case SqlDbType.SmallDateTime: w.Write((Int64)((DateTime)((SqlDateTime)Value).Value).ToBinary()); break;
        //        case SqlDbType.SmallInt: w.Write((Int16)((SqlInt16)Value)); break;
        //        case SqlDbType.SmallMoney: w.Write(((SqlMoney)Value).Value); break;
        //        // Not support SqlDbType.Structured
        //        // Not support SqlDbType.Text
        //        case SqlDbType.Time: w.Write((Int64)((TimeSpan)Value).Ticks); break;
        //        // Not support SqlDbType.Timestamp
        //        case SqlDbType.TinyInt: w.Write((Byte)((SqlByte)Value)); break;
        //        case SqlDbType.Udt:
        //            // TODO: Пока поддержа только SqlDbParams
        //            // w.Write(Value.GetType().Assembly.FullName);
        //            // w.Write(Value.GetType().FullName);
        //            //if (Value is IBinarySerialize)
        //            //    (Value as IBinarySerialize).Write(w);
        //            //else
        //            //    throw new Exception(String.Format("Невозможно записать данные, тип UDT '{0}' не поддерживается текущей версией {1}", Value.GetType().Name, this.GetType().Name));
        //            (Value as IBinarySerialize).Write(w);
        //            break;
        //        case SqlDbType.UniqueIdentifier: w.Write(((Guid)((SqlGuid)Value).Value).ToString()); break;
        //        case SqlDbType.VarBinary: w.Write((Int32)((SqlBytes)Value).Length); w.Write(((SqlBytes)Value).Buffer, 0, (Int32)((SqlBytes)Value).Length); break;
        //        case SqlDbType.VarChar:
        //            SqlString VarChar = (SqlString)Value;
        //            w.Write((Int32)VarChar.SqlCompareOptions);
        //            w.Write(VarChar.LCID);
        //            w.Write(VarChar.Value);
        //            break;
        //        // Not support SqlDbType.Variant
        //        case SqlDbType.Xml: w.Write(((SqlXml)Value).Value); break;
        //        // Not support SqlDbType.Xml
        //        default:
        //            throw new Exception(String.Format("Params not supported type '{0}'", Type.ToString()));
        //    }
        //}
    }
}