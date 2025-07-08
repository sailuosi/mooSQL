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

namespace mooSQL.data
{
    /// <summary>
    /// 存放成员属性部分
    /// </summary>
    public partial class Deserializer
    {

        private Dictionary<Type, ITypeHandler> typeHandlers;

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
            if (typeHandlers.TryGetValue(type, out ITypeHandler handler))
            {
                return (T)handler.Parse(type, value);
            }
            if (type.Name == "String") {
                String strVal = Convert.ToString(value) ?? string.Empty;
                Object vv = strVal;
                return (T)vv;
            }
            return (T)Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
        }



        #region typeHandlers集合的增删改
        /// <summary>
        /// 配置类的自定义处理器
        /// </summary>
        /// <typeparam name="T">The type to handle.</typeparam>
        /// <param name="handler">The handler for the type <typeparamref name="T"/>.</param>
        public void AddTypeHandler<T>(TypeHandler<T> handler) => AddTypeHandlerImpl(typeof(T), handler, true);

        /// <summary>
        /// 配置类的自定义处理器
        /// </summary>
        /// <param name="type">The type to handle.</param>
        /// <param name="handler">The handler to process the <paramref name="type"/>.</param>
        /// <param name="clone">Whether to clone the current type handler map.</param>
        public void AddTypeHandlerImpl(Type type, ITypeHandler handler, bool clone)
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

            var newCopy = clone ? new Dictionary<Type, ITypeHandler>(snapshot) : snapshot;

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
            typeHandlers = new Dictionary<Type, ITypeHandler>();
            AddTypeHandlerImpl(typeof(DataTable), new DataTableHandler(), clone);
            AddTypeHandlerImpl(typeof(XmlDocument), new XmlDocumentHandler(), clone);
            AddTypeHandlerImpl(typeof(XDocument), new XDocumentHandler(), clone);
            AddTypeHandlerImpl(typeof(XElement), new XElementHandler(), clone);
        }

        /// <summary>
        /// Configure the specified type to be processed by a custom handler.
        /// </summary>
        /// <param name="type">The type to handle.</param>
        /// <param name="handler">The handler to process the <paramref name="type"/>.</param>
        public void AddTypeHandler(Type type, ITypeHandler handler) => AddTypeHandlerImpl(type, handler, true);
        /// <summary>
        /// Determine if the specified type will be processed by a custom handler.
        /// </summary>
        /// <param name="type">The type to handle.</param>
        /// <returns>Boolean value specifying whether the type will be processed by a custom handler.</returns>
        public bool HasTypeHandler(Type type) => typeHandlers.ContainsKey(type);



        /// <summary>
        /// OBSOLETE: For internal usage only. Lookup the DbType and handler for a given Type and member
        /// </summary>
        /// <param name="type">The type to lookup.</param>
        /// <param name="name">The name (for error messages).</param>
        /// <param name="demand">Whether to demand a value (throw if missing).</param>
        /// <param name="handler">The handler for <paramref name="type"/>.</param>

        public DbType LookupDbType(Type type, string name, bool demand, out ITypeHandler handler)
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
                            handler = (ITypeHandler)Activator.CreateInstance(
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
