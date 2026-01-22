using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mooSQL.data.context;
using System.Xml.Linq;
using System.Xml;
using System.Collections;
using System.Data.Common;
using System.Reflection;

namespace mooSQL.data
{
    /// <summary>
    /// 存放成员属性部分
    /// </summary>
    public partial class PackUp
    {

        private Dictionary<Type, TypeMapEntry> typeMap;
        MooClient _client { get; set; }
        public PackUp(MooClient client)
        {
            _client = client;
            typeMap = new Dictionary<Type, TypeMapEntry>(41)
            {
                [typeof(byte)] = DbType.Byte,
                [typeof(sbyte)] = DbType.SByte,
                [typeof(short)] = DbType.Int16,
                [typeof(ushort)] = DbType.UInt16,
                [typeof(int)] = DbType.Int32,
                [typeof(uint)] = DbType.UInt32,
                [typeof(long)] = DbType.Int64,
                [typeof(ulong)] = DbType.UInt64,
                [typeof(float)] = DbType.Single,
                [typeof(double)] = DbType.Double,
                [typeof(decimal)] = DbType.Decimal,
                [typeof(bool)] = DbType.Boolean,
                [typeof(string)] = DbType.String,
                [typeof(char)] = DbType.StringFixedLength,
                [typeof(Guid)] = DbType.Guid,
                [typeof(DateTime)] = TypeMapEntry.DoNotSet,
                [typeof(DateTimeOffset)] = DbType.DateTimeOffset,
                [typeof(TimeSpan)] = TypeMapEntry.DoNotSet,
                [typeof(byte[])] = DbType.Binary,
                [typeof(byte?)] = DbType.Byte,
                [typeof(sbyte?)] = DbType.SByte,
                [typeof(short?)] = DbType.Int16,
                [typeof(ushort?)] = DbType.UInt16,
                [typeof(int?)] = DbType.Int32,
                [typeof(uint?)] = DbType.UInt32,
                [typeof(long?)] = DbType.Int64,
                [typeof(ulong?)] = DbType.UInt64,
                [typeof(float?)] = DbType.Single,
                [typeof(double?)] = DbType.Double,
                [typeof(decimal?)] = DbType.Decimal,
                [typeof(bool?)] = DbType.Boolean,
                [typeof(char?)] = DbType.StringFixedLength,
                [typeof(Guid?)] = DbType.Guid,
                [typeof(DateTime?)] = TypeMapEntry.DoNotSet,
                [typeof(DateTimeOffset?)] = DbType.DateTimeOffset,
                [typeof(TimeSpan?)] = TypeMapEntry.DoNotSet,
                [typeof(object)] = DbType.Object,
                [typeof(SqlDecimal)] = TypeMapEntry.DecimalFieldValue,
                [typeof(SqlDecimal?)] = TypeMapEntry.DecimalFieldValue,
                [typeof(SqlMoney)] = TypeMapEntry.DecimalFieldValue,
                [typeof(SqlMoney?)] = TypeMapEntry.DecimalFieldValue,
            };
            ResetTypeHandlers(false);
        }

        /// <summary>
        /// 配置指定类型映射到给定的数据库类型。
        /// </summary>
        /// <param name="type">要映射的源类型。</param>
        /// <param name="dbType">要映射到的数据库类型。</param>
        public void AddTypeMap(Type type, DbType dbType)
            => AddTypeMap(type, dbType, false);

        /// <summary>
        /// 配置指定类型映射到给定的数据库类型。
        /// </summary>
        /// <param name="type">要映射的源类型。</param>
        /// <param name="dbType">要映射到的数据库类型。</param>
        /// <param name="useGetFieldValue">是否优先使用 <see cref="DbDataReader.GetFieldValue{T}(int)"/> 而不是 <see cref="DbDataReader.GetValue(int)"/>。</param>
        public void AddTypeMap(Type type, DbType dbType, bool useGetFieldValue)
        {
            // use clone, mutate, replace to avoid threading issues
            var snapshot = typeMap;
            var flags = TypeMapEntryFlags.None;
            if (dbType >= 0)
            {
                flags |= TypeMapEntryFlags.SetType;
            }
            if (useGetFieldValue)
            {
                flags |= TypeMapEntryFlags.UseGetFieldValue;
            }
            var value = new TypeMapEntry(dbType, flags);
            if (snapshot.TryGetValue(type, out var oldValue) && oldValue.Equals(value)) return; // nothing to do

            SetTypeMap(new Dictionary<Type, TypeMapEntry>(snapshot) { [type] = value });
        }

        private void SetTypeMap(Dictionary<Type, TypeMapEntry> value)
        {
            typeMap = value;

            // this cache is predicated on the contents of the type-map; reset it
            lock (s_ReadViaGetFieldValueCache)
            {
                s_ReadViaGetFieldValueCache.Clear();
            }
        }
        /// <summary>
        /// 从类型/数据库类型映射表中移除指定类型。
        /// </summary>
        /// <param name="type">要从当前映射中移除的类型。</param>
        public void RemoveTypeMap(Type type)
        {
            // use clone, mutate, replace to avoid threading issues
            var snapshot = typeMap;

            if (!snapshot.ContainsKey(type)) return; // nothing to do

            var newCopy = new Dictionary<Type, TypeMapEntry>(snapshot);
            newCopy.Remove(type);

            SetTypeMap(newCopy);
        }


        Func<DbDataReader,DBInstance, object> ReadViaGetFieldValueFactory(Type type, int index)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            var factory = (Func<int, Func<DbDataReader,DBInstance, object>>)s_ReadViaGetFieldValueCache[type];
            if (factory is null)
            {
                factory = (Func<int, Func<DbDataReader, DBInstance, object>>)Delegate.CreateDelegate(
                    typeof(Func<int, Func<DbDataReader, DBInstance, object>>), null, typeof(PackUp).GetMethod(
                    nameof(UnderlyingReadViaGetFieldValueFactory), BindingFlags.Static | BindingFlags.NonPublic)
                    .MakeGenericMethod(type));
                lock (s_ReadViaGetFieldValueCache)
                {
                    s_ReadViaGetFieldValueCache[type] = factory;
                }
            }
            return factory(index);
        }
        // cache of ReadViaGetFieldValueFactory<T> for per-value T
        static readonly Hashtable s_ReadViaGetFieldValueCache = new Hashtable();

        Func<DbDataReader, DBInstance, object> UnderlyingReadViaGetFieldValueFactory<T>(int index) {

            return (reader,db) =>
            {
                if (reader.IsDBNull(index)) {
                    return null;
                }
                return reader.GetFieldValue<T>(index);
            };
        }
             

        bool UseGetFieldValue(Type type) => typeMap.TryGetValue(type, out var mapEntry)
            && (mapEntry.Flags & TypeMapEntryFlags.UseGetFieldValue) != 0;



    }
}
