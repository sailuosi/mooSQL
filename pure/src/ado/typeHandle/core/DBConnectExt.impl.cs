using mooSQL.data.context;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    public static partial class DBConnectExt
    {



        #region 被SQLBuilder调用

        /// <summary>
        /// 查询一行并转为某个类
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <param name="effectiveType"></param>
        /// <param name="cmd"></param>
        /// <param name="conn"></param>
        /// <param name="addToCache"></param>
        /// <returns></returns>
        internal static T queryRowByType<T>(this CmdExecutor executor,DbDataReader reader, Type effectiveType, DbCommand cmd, IDbConnection conn, bool addToCache,DBInstance DB)
        {
            var identity = new Identity(cmd.CommandText, cmd.CommandType, conn, effectiveType, cmd.Parameters.GetType());
            var info = MapperCache.GetCacheInfo(executor.deserializer,identity, null, addToCache);
            int hash = MapperUntils.GetColumnHash(reader);
            var tuple = info.Deserializer;

            if (tuple.Func is null || tuple.Hash != hash)
            {
                tuple = info.Deserializer = new DeserializerState(hash, executor.deserializer.GetDeserializer(effectiveType, reader, 0, -1, false, DB));
                if (addToCache) MapperCache.SetQueryCache(identity, info);
            }
            T result = default;
            var func = tuple.Func;
            var convertToType = Nullable.GetUnderlyingType(effectiveType) ?? effectiveType;
            if (reader.Read() && reader.FieldCount != 0) { 
                object val = func(reader,DB);
                result= GetValue<T>(reader, effectiveType, val);            
            }
            while (reader.Read())
            {
                //后续的忽略
            }
            while (reader.NextResult()) { /* 忽略后续内容 */ }
            return result;
        }


        internal static T queryOnlyRowByType<T>(this CmdExecutor executor, DbDataReader reader, Type effectiveType, DbCommand cmd, IDbConnection conn, bool addToCache,DBInstance DB)
        {
            var identity = new Identity(cmd.CommandText, cmd.CommandType, conn, effectiveType, cmd.Parameters.GetType());
            var info = MapperCache.GetCacheInfo(executor.deserializer,identity, null, addToCache);
            int hash = MapperUntils.GetColumnHash(reader);
            var tuple = info.Deserializer;

            if (tuple.Func is null || tuple.Hash != hash)
            {
                tuple = info.Deserializer = new DeserializerState(hash, executor.deserializer.GetDeserializer(effectiveType, reader, 0, -1, false, DB));
                if (addToCache) MapperCache.SetQueryCache(identity, info);
            }
            T result = default;
            var func = tuple.Func;
            var convertToType = Nullable.GetUnderlyingType(effectiveType) ?? effectiveType;
            if (reader.Read() && reader.FieldCount != 0)
            {
                object val = func(reader, DB);
                result = GetValue<T>(reader, effectiveType, val);
            }
            while (reader.Read())
            {
                //后续的还有的话，抛出null
                return default(T);
            }
            while (reader.NextResult()) { /* 忽略后续内容 */ }
            return result;
        }

        /// <summary>
        /// 查询首行首列值，并转为类型
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="effectiveType"></param>
        /// <param name="cmd"></param>
        /// <returns></returns>
        internal static T queryScalarByType<T>(DbCommand cmd,Deserializer deserializer)
        {
            var  data = cmd.ExecuteScalar();
            return deserializer.Parse<T>(data);
        }
        #endregion


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T GetValue<T>(DbDataReader reader, Type effectiveType, object val)
        {
            if (val is T tVal)
            {
                return tVal;
            }
            else if (val is null && (!effectiveType.IsValueType || Nullable.GetUnderlyingType(effectiveType) != null))
            {
                return default;
            }
            else if (val is Array array && typeof(T).IsArray)
            {
                var elementType = typeof(T).GetElementType();
                var result = Array.CreateInstance(elementType, array.Length);
                for (int i = 0; i < array.Length; i++)
                    result.SetValue(Convert.ChangeType(array.GetValue(i), elementType, CultureInfo.InvariantCulture), i);
                return (T)(object)result;
            }
            else
            {
                try
                {
                    var convertToType = Nullable.GetUnderlyingType(effectiveType) ?? effectiveType;
                    return (T)Convert.ChangeType(val, convertToType, CultureInfo.InvariantCulture);
                }
                catch (Exception ex)
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    MapperUntils.ThrowDataException(ex, 0, reader, val);
#pragma warning restore CS0618 // Type or member is obsolete
                    return default; // For the compiler - we've already thrown
                }
            }
        }




        private static CommandBehavior GetBehavior(bool close, CommandBehavior @default)
        {
            return (close ? (@default | CommandBehavior.CloseConnection) : @default) & Settings.AllowedCommandBehaviors;
        }



    }
}
