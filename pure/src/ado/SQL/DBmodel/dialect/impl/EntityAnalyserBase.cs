using System;

namespace mooSQL.data.Mapping
{
    /// <summary>
    /// 实体解析器非泛型基类：注册名、表/列名转换钩子。
    /// </summary>
    public abstract class EntityAnalyserBase : IEntityAnalyser
    {
        string _name;

        /// <summary>
        /// 注册键；同名注册时替换已有项。空则注册时回退为类型全名。
        /// </summary>
        public virtual string Name
        {
            get => _name;
            protected set => _name = value;
        }

        /// <summary>
        /// 解析失败时是否进入回退链。
        /// </summary>
        public virtual bool FailBacked => false;

        /// <summary>
        /// 隐式表名转换（特性未指定 Name 时）。
        /// </summary>
        public Func<string, string> TableNameConvert { get; set; }

        /// <summary>
        /// 隐式列名转换（特性未指定 Name 时）。
        /// </summary>
        public Func<string, string> ColumnNameConvert { get; set; }

        /// <inheritdoc />
        public abstract bool CanParse(Type type);

        /// <inheritdoc />
        public abstract EntityInfo ParseEntity(Type entity, EntityInfo info);

        /// <inheritdoc />
        public abstract EntityColumn ParseColumn(Type entity, System.Reflection.PropertyInfo propertyInfo, EntityInfo entityInfo, EntityColumn entityColumn);

        /// <summary>
        /// 解析注册键：显式 Name 优先，否则类型全名。
        /// </summary>
        public static string ResolveRegistrationKey(IEntityAnalyser analyser)
        {
            if (analyser == null)
                throw new ArgumentNullException(nameof(analyser));
            var name = analyser.Name;
            if (!string.IsNullOrWhiteSpace(name))
                return name.Trim();
            return analyser.GetType().FullName ?? analyser.GetType().Name;
        }

        /// <summary>
        /// 隐式表名：先 Func，再虚方法扩展点。
        /// </summary>
        protected virtual string ResolveTableName(string rawName)
        {
            if (string.IsNullOrEmpty(rawName))
                return rawName;
            var converted = TableNameConvert != null ? TableNameConvert(rawName) : rawName;
            return converted;
        }

        /// <summary>
        /// 隐式列名：先 Func，再虚方法扩展点。
        /// </summary>
        protected virtual string ResolveColumnName(string rawName)
        {
            if (string.IsNullOrEmpty(rawName))
                return rawName;
            var converted = ColumnNameConvert != null ? ColumnNameConvert(rawName) : rawName;
            return converted;
        }
    }
}
