using mooSQL.data.context;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml;
using System.Reflection;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using mooSQL.utils;

namespace mooSQL.data
{
    /// <summary>
    /// 存放成员属性部分
    /// </summary>
    public partial class PackUp
    {

        private Dictionary<Type, ITypeParser> typeHandlers;

        internal T Parse<T>(object value)
        {
            if (value is null || value is DBNull) return default;
            if (value is T t) return t;
            var type = typeof(T);
            type = Nullable.GetUnderlyingType(type) ?? type;
            if (type.IsEnum)
            {
                if (value is float || value is double || value is decimal)
                {
                    value = Convert.ChangeType(value, Enum.GetUnderlyingType(type), CultureInfo.InvariantCulture);
                }
                return (T)Enum.ToObject(type, value);
            }
            if (typeHandlers.TryGetValue(type, out ITypeParser handler))
            {
                return (T)handler.Parse(type, value);
            }
            if (type.Name == "String") {
                if (value == null) { 
                    return default;
                }
                return (T)(object)value.ToString();
                //String strVal = Convert.ToString(value) ?? string.Empty;
                //Object vv = strVal;
                //return (T)vv;
            }
            if (type.Name == "Guid") {
                if (value is Guid gval) { 
                    return (T)(object)gval;
                }
                var val = value as string;
                if (RegxUntils.isGUID(val))
                {
                    return (T)(object)Guid.Parse(val);
                }
                return default;
            }
            if(type.Name == "DateTime")
            {
                if (value is DateTime dt) { return (T)(object)dt; }
                var val = value as string;
                if (TypeAs.asDateTimeFull(val,out var dtval)) { 
                    return (T)(object)dtval;
                }
                
            }
            return (T)Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
        }



        #region typeHandlers集合的增删改
        /// <summary>
        /// 配置类的自定义处理器
        /// </summary>
        /// <typeparam name="T">要处理的类型。</typeparam>
        /// <param name="handler">类型 <typeparamref name="T"/> 的处理器。</param>
        public void AddTypeHandler<T>(TypeHandler<T> handler) => AddTypeHandlerImpl(typeof(T), handler, true);

        /// <summary>
        /// 配置类的自定义处理器
        /// </summary>
        /// <param name="type">要处理的类型。</param>
        /// <param name="handler">处理 <paramref name="type"/> 的处理器。</param>
        /// <param name="clone">是否克隆当前类型处理器映射。</param>
        public void AddTypeHandlerImpl(Type type, ITypeParser handler, bool clone)
        {
            if (type is null) throw new ArgumentNullException(nameof(type));

            Type secondary = null;
            if (type.IsValueType)
            {
                var underlying = Nullable.GetUnderlyingType(type);
                if (underlying is null)
                {
                    secondary = typeof(Nullable<>).MakeGenericType(type); // the Nullable<T>
                    // type is already the T
                }
                else
                {
                    secondary = type; // the Nullable<T>
                    type = underlying; // the T
                }
            }

            var snapshot = typeHandlers;
            if (snapshot.TryGetValue(type, out var oldValue) && handler == oldValue) return; // nothing to do

            var newCopy = clone ? new Dictionary<Type, ITypeParser>(snapshot) : snapshot;

#pragma warning disable 618
            typeof(TypeHandlerCache<>).MakeGenericType(type).GetMethod(nameof(TypeHandlerCache<int>.SetHandler), BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { handler });
            if (secondary != null)
            {
                typeof(TypeHandlerCache<>).MakeGenericType(secondary).GetMethod(nameof(TypeHandlerCache<int>.SetHandler), BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { handler });
            }
#pragma warning restore 618
            if (handler is null)
            {
                newCopy.Remove(type);
                if (secondary != null) newCopy.Remove(secondary);
            }
            else
            {
                newCopy[type] = handler;
                if (secondary != null) newCopy[secondary] = handler;
            }
            typeHandlers = newCopy;
        }
        /// <summary>
        /// 清空类型处理器
        /// </summary>
        public void ResetTypeHandlers() => ResetTypeHandlers(true);

        //[MemberNotNull(nameof(typeHandlers))]
        private void ResetTypeHandlers(bool clone)
        {
            typeHandlers = new Dictionary<Type, ITypeParser>();
            AddTypeHandlerImpl(typeof(DataTable), new DataTableHandler(), clone);
            AddTypeHandlerImpl(typeof(XmlDocument), new XmlDocumentHandler(), clone);
            AddTypeHandlerImpl(typeof(XDocument), new XDocumentHandler(), clone);
            AddTypeHandlerImpl(typeof(XElement), new XElementHandler(), clone);
        }

        /// <summary>
        /// 配置指定类型由自定义处理器处理。
        /// </summary>
        /// <param name="type">要处理的类型。</param>
        /// <param name="handler">处理 <paramref name="type"/> 的处理器。</param>
        public void AddTypeHandler(Type type, ITypeParser handler) => AddTypeHandlerImpl(type, handler, true);
        /// <summary>
        /// 确定指定类型是否将由自定义处理器处理。
        /// </summary>
        /// <param name="type">要处理的类型。</param>
        /// <returns>布尔值，指定类型是否将由自定义处理器处理。</returns>
        public bool HasTypeHandler(Type type) => typeHandlers.ContainsKey(type);



        /// <summary>
        /// 已过时：仅供内部使用。查找给定类型和成员的 DbType 和处理器
        /// </summary>
        /// <param name="type">要查找的类型。</param>
        /// <param name="name">名称（用于错误消息）。</param>
        /// <param name="demand">是否要求值（如果缺失则抛出异常）。</param>
        /// <param name="handler"><paramref name="type"/> 的处理器。</param>

        public DbType LookupDbType(Type type, string name, bool demand, out ITypeParser handler)
        {
            handler = null;
            var nullUnderlyingType = Nullable.GetUnderlyingType(type);
            if (nullUnderlyingType != null) type = nullUnderlyingType;
            if (type.IsEnum && !typeMap.ContainsKey(type))
            {
                type = Enum.GetUnderlyingType(type);
            }
            if (typeMap.TryGetValue(type, out var mapEntry))
            {
                if ((mapEntry.Flags & TypeMapEntryFlags.SetType) == 0)
                {
                    return default( DbType);
                }
                return mapEntry.DbType;
            }
            if (type.FullName == MapperUntils.LinqBinary)
            {
                return DbType.Binary;
            }
            if (typeHandlers.TryGetValue(type, out handler))
            {
                return DbType.Object;
            }
            if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                // auto-detect things like IEnumerable<SqlDataRecord> as a family
                if (type.IsInterface && type.IsGenericType
                    && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                    && typeof(IEnumerable<IDataRecord>).IsAssignableFrom(type))
                {
                    var argTypes = type.GetGenericArguments();
                    if (typeof(IDataRecord).IsAssignableFrom(argTypes[0]))
                    {
                        try
                        {
                            handler = (ITypeParser)Activator.CreateInstance(
                                typeof(SqlDataRecordHandler<>).MakeGenericType(argTypes));
                            AddTypeHandlerImpl(type, handler, true);
                            return DbType.Object;
                        }
                        catch
                        {
                            handler = null;
                        }
                    }
                }
                return MapperUntils.EnumerableMultiParameter;
            }

            switch (type.FullName)
            {
                case "Microsoft.SqlServer.Types.SqlGeography":
                    AddTypeHandler(type, handler = new UdtTypeHandler("geography"));
                    return DbType.Object;
                case "Microsoft.SqlServer.Types.SqlGeometry":
                    AddTypeHandler(type, handler = new UdtTypeHandler("geometry"));
                    return DbType.Object;
                case "Microsoft.SqlServer.Types.SqlHierarchyId":
                    AddTypeHandler(type, handler = new UdtTypeHandler("hierarchyid"));
                    return DbType.Object;
            }

            if (demand)
                throw new NotSupportedException($"The member {name} of type {type.FullName} cannot be used as a parameter value");
            return DbType.Object;
        }
        #endregion
    }
}
