using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using mooSQL.utils;

namespace mooSQL.data
{
    /// <summary>
    /// 类映射策略
    /// </summary>
    public sealed class DefaultTypeMap : ITypeMap
    {
        private readonly List<FieldInfo> _fields;
        private readonly Type _type;

        MooClient _client;
        /// <summary>
        /// 默认映射
        /// </summary>
        /// <param name="type">Entity type</param>
        public DefaultTypeMap(Type type,MooClient client)
        {
            this._client = client;
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            _fields = GetSettableFields(type);
            Properties = GetSettableProps(type);
            _type = type;
        }

        private EntityContext entityContext {
            get {
                return _client.EntityCash;
            }
        }

        internal static MethodInfo GetPropertySetterOrThrow(PropertyInfo propertyInfo, Type type)
        {
            return GetPropertySetter(propertyInfo, type) ?? Throw(propertyInfo);


        }
        static MethodInfo Throw(PropertyInfo propertyInfo) => throw new InvalidOperationException("Property setting not found for: " + propertyInfo?.Name);
        internal static MethodInfo GetPropertySetter(PropertyInfo propertyInfo, Type type)
        {
            if (propertyInfo.DeclaringType == type) return propertyInfo.GetSetMethod(true);

            return propertyInfo.DeclaringType.GetProperty(
                   propertyInfo.Name,
                   BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                   Type.DefaultBinder,
                   propertyInfo.PropertyType,
                   propertyInfo.GetIndexParameters().Select(p => p.ParameterType).ToArray(),
                   null).GetSetMethod(true);
        }

        internal static List<PropertyInfo> GetSettableProps(Type t)
        {
            return t
                  .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                  .Where(p => GetPropertySetter(p, t) != null)
                  .ToList();
        }

        internal static List<FieldInfo> GetSettableFields(Type t)
        {
            return t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).ToList();
        }

        /// <summary>
        /// 寻找最优构造器
        /// </summary>
        /// <param name="names">DataReader列名</param>
        /// <param name="types">DataReader列类型</param>
        /// <returns>匹配的构造器</returns>
        public ConstructorInfo FindConstructor(string[] names, Type[] types, Deserializer deserializer)
        {
            var constructors = _type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (ConstructorInfo ctor in constructors.OrderBy(c => c.IsPublic ? 0 : (c.IsPrivate ? 2 : 1)).ThenBy(c => c.GetParameters().Length))
            {
                ParameterInfo[] ctorParameters = ctor.GetParameters();
                if (ctorParameters.Length == 0)
                    return ctor;

                if (ctorParameters.Length != types.Length)
                    continue;
                //这里要注意处理匿名类，如果是匿名类，则所有值均通过构造函数传入
                if (_type.IsAnonymous())
                {
                    if (ctorParameters.Length == types.Length) {
                        return ctor;
                    }
                    
                }
                int i = 0;
                for (; i < ctorParameters.Length; i++)
                {
                    if (EqualsCI(ctorParameters[i].Name, names[i]))
                    {

                        continue;
                    } // exact match
                    else if (MatchNamesWithUnderscores && EqualsCIU(ctorParameters[i].Name, names[i]))
                    { } // match after applying underscores
                    else
                    {
                        // not a name match
                        break;
                    }

                    if (types[i] == typeof(byte[]) && ctorParameters[i].ParameterType.FullName == MapperUntils.LinqBinary)
                        continue;
                    var unboxedType = Nullable.GetUnderlyingType(ctorParameters[i].ParameterType) ?? ctorParameters[i].ParameterType;
                    if ((unboxedType != types[i] && !deserializer.HasTypeHandler(unboxedType))
                        && !(unboxedType.IsEnum && Enum.GetUnderlyingType(unboxedType) == types[i])
                        && !(unboxedType == typeof(char) && types[i] == typeof(string))
                        && !(unboxedType.IsEnum && types[i] == typeof(string)))
                    {
                        break;
                    }
                }

                if (i == ctorParameters.Length)
                    return ctor;
            }

            return null;
        }

        /// <summary>
        /// Returns the constructor, if any, that has the ExplicitConstructorAttribute on it.
        /// </summary>
        public ConstructorInfo FindExplicitConstructor()
        {
            var constructors = _type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var withAttr = constructors.Where(c => c.GetCustomAttributes(typeof(ExplicitConstructorAttribute), true).Length > 0).ToList();

            if (withAttr.Count == 1)
            {
                return withAttr[0];
            }

            return null;
        }

        /// <summary>
        /// Gets mapping for constructor parameter
        /// </summary>
        /// <param name="constructor">Constructor to resolve</param>
        /// <param name="columnName">DataReader column name</param>
        /// <returns>Mapping implementation</returns>
        public IMemberMap GetConstructorParameter(ConstructorInfo constructor, string columnName)
        {
            var param = MatchFirstOrDefault(constructor.GetParameters(), columnName, p => p.Name) ?? Throw(columnName);
            return new SimpleMemberMap(columnName, param);


        }
        private static ParameterInfo Throw(string name) => throw new ArgumentException("Constructor parameter not found for " + name);

        private static string mapName(PropertyInfo p) { 
            return p.Name;
        }

        private PropertyInfo loadTypeProp(string columnName) {
            var entitiy = entityContext.getEntityInfo(_type);
            if (entitiy != null)
            {
                //如果能够依据反射特性得到，则使用它，否则，走默认映射。
                if (entitiy.Columns != null && entitiy.Columns.Count > 0)
                {
                    foreach (var col in entitiy.Columns)
                    {
                        if (col.DbColumnName == columnName || col.Alias == columnName)
                        {
                            return col.PropertyInfo;
                        }
                    }
                }
            }
            return null;
        }
        /// <summary>
        /// 获取字段的属性映射
        /// </summary>
        /// <param name="columnName">reader字段名</param>
        /// <returns>Mapping implementation</returns>
        public IMemberMap GetMember(string columnName)
        {
            var col= loadTypeProp(columnName);
            if (col != null) {
                return new SimpleMemberMap(columnName, col);
            }
            var property = MatchFirstOrDefault(Properties, columnName, mapName);

            if (property != null)
                return new SimpleMemberMap(columnName, property);

            // roslyn automatically implemented properties, in particular for get-only properties: <{Name}>k__BackingField;
            var backingFieldName = "<" + columnName + ">k__BackingField";

            // preference order is:
            // exact match over underscore match, exact case over wrong case, backing fields over regular fields, match-inc-underscores over match-exc-underscores
            var field = _fields.Find(p => string.Equals(p.Name, columnName, StringComparison.Ordinal))
                ?? _fields.Find(p => string.Equals(p.Name, backingFieldName, StringComparison.Ordinal))
                ?? _fields.Find(p => string.Equals(p.Name, columnName, StringComparison.OrdinalIgnoreCase))
                ?? _fields.Find(p => string.Equals(p.Name, backingFieldName, StringComparison.OrdinalIgnoreCase));

            if (field is null && MatchNamesWithUnderscores)
            {
                var effectiveColumnName = columnName.Replace("_", "");
                backingFieldName = "<" + effectiveColumnName + ">k__BackingField";

                field = _fields.Find(p => string.Equals(p.Name, effectiveColumnName, StringComparison.Ordinal))
                    ?? _fields.Find(p => string.Equals(p.Name, backingFieldName, StringComparison.Ordinal))
                    ?? _fields.Find(p => string.Equals(p.Name, effectiveColumnName, StringComparison.OrdinalIgnoreCase))
                    ?? _fields.Find(p => string.Equals(p.Name, backingFieldName, StringComparison.OrdinalIgnoreCase));
            }

            if (field != null)
                return new SimpleMemberMap(columnName, field);

            return null;
        }
        /// <summary>
        /// Should column names like User_Id be allowed to match properties/fields like UserId ?
        /// </summary>
        public static bool MatchNamesWithUnderscores { get; set; }

        T MatchFirstOrDefault<T>(IList<T> members, string name, Func<T, string> selector) where T : class
        {



            if (members !=null &&  members.Count > 0 )
            {
                // try exact first
                foreach (var member in members)
                {
                    if (string.Equals(name, selector(member), StringComparison.Ordinal))
                    {
                        return member;
                    }
                }
                // then exact ignoring case
                foreach (var member in members)
                {
                    if (string.Equals(name, selector(member), StringComparison.OrdinalIgnoreCase))
                    {
                        return member;
                    }
                }
                if (MatchNamesWithUnderscores)
                {
                    // same again, minus underscore delta
                    name = name?.Replace("_", "");

                    // match normalized column name vs actual property name
                    foreach (var member in members)
                    {
                        if (string.Equals(name, selector(member), StringComparison.Ordinal))
                        {
                            return member;
                        }
                    }
                    foreach (var member in members)
                    {
                        if (string.Equals(name, selector(member), StringComparison.OrdinalIgnoreCase))
                        {
                            return member;
                        }
                    }

                    // match normalized column name vs normalized property name
                    foreach (var member in members)
                    {
                        if (string.Equals(name, selector(member)?.Replace("_", ""), StringComparison.Ordinal))
                        {
                            return member;
                        }
                    }
                    foreach (var member in members)
                    {
                        if (string.Equals(name, selector(member)?.Replace("_", ""), StringComparison.OrdinalIgnoreCase))
                        {
                            return member;
                        }
                    }
                }
            }
            return null;
        }

        internal static bool EqualsCI(string x, string y)
            => string.Equals(x, y, StringComparison.OrdinalIgnoreCase);
        internal static bool EqualsCIU(string x, string y)
            => string.Equals(x?.Replace("_", ""), y?.Replace("_", ""), StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// The settable properties for this typemap
        /// </summary>
        public List<PropertyInfo> Properties { get; }
    }
}
