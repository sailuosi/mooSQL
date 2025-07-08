using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using mooSQL.data.Mapping;

namespace mooSQL.data
{
    /// <summary>
    /// 代表一个字段或属性是关联对象的外键。
    /// </summary>
    [AttributeUsage(
    AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Interface,
    AllowMultiple = true, Inherited = true)]
    public class SooLinkAttribute : Attribute
    {
        public SooLinkAttribute(LinkWay linkType, string thisKey)
        {
            Type = linkType;
            ThisKey = thisKey;
        }

        public SooLinkAttribute(LinkWay linkType, string thisKey, string thatKey)
        {
            Type = linkType;
            ThisKey = thisKey;
            ThatKey = thatKey;
        }
        /// <summary>
        /// 关联类型
        /// </summary>
        public LinkWay Type { get; set; }

        /// <summary>
        /// 获取或设置关联这一侧以逗号分隔的关联键成员列表。
        /// 这些键将用于生成连接谓词，并且必须与<see cref="ThatKey"/>键兼容。
        /// 如果不使用自定义谓词（请参阅<see cref="ExpressionPredicate"/>），则必须指定键。
        /// </summary>
        public string? ThisKey { get; set; }

        /// <summary>
        /// Gets or sets comma-separated list of association key members on another side of association.
        /// Those keys will be used for join predicate generation and must be compatible with <see cref="ThisKey"/> keys.
        /// You must specify keys it you do not use custom predicate (see <see cref="ExpressionPredicate"/>).
        /// </summary>
        public string? ThatKey { get; set; }

        /// <summary>
        /// Specifies static property or method without parameters, that returns join predicate expression. This predicate will be used together with
        /// <see cref="ThisKey"/>/<see cref="ThatKey"/> join keys, if they are specified.
        /// Predicate expression lambda function takes two parameters: this record and other record and returns boolean result.
        /// </summary>
        public string? ExpressionPredicate { get; set; }

        /// <summary>
        /// Specifies predicate expression. This predicate will be used together with
        /// <see cref="ThisKey"/>/<see cref="ThatKey"/> join keys, if they are specified.
        /// Predicate expression lambda function takes two parameters: this record and other record and returns boolean result.
        /// </summary>
        public Expression? Predicate { get; set; }

        /// <summary>
        /// Specifies static property or method without parameters, that returns IQueryable expression. If is set, other association keys are ignored.
        /// Result of query method should be lambda which takes two parameters: this record, IDataContext and returns IQueryable result.
        /// <para>
        /// <example>
        /// <code>
        /// public class SomeEntity
        /// {
        ///     [Association(ExpressionQueryMethod = nameof(OtherImpl), CanBeNull = true)]
        ///     public SomeOtherEntity Other { get; set; }
        ///
        ///     public static Expression&lt;Func&lt;SomeEntity, IDataContext, IQueryable&lt;SomeOtherEntity&gt;&gt;&gt; OtherImpl()
        ///     {
        ///         return (e, db) =&gt; db.GetTable&lt;SomeOtherEntity&gt;().Where(se =&gt; se.Id == e.Id);
        ///     }
        /// }
        /// </code>
        /// </example>
        /// </para>
        /// </summary>
        public string? QueryExpressionMethod { get; set; }

        /// <summary>
        /// Specifies query expression. If is set, other association keys are ignored.
        /// Lambda function takes two parameters: this record, IDataContext and returns IQueryable result.
        /// <para>
        /// <example>
        /// <code>
        /// Expression&lt;Func&lt;SomeEntity, IDataContext, IQueryable&lt;SomeOtherEntity&gt;&gt;&gt; associationQuery;
        /// <para />
        /// associationQuery = (e, db) =&gt; db.GetTable&lt;SomeOtherEntity&gt;().Where(se =&gt; se.Id == e.Id);
        /// </code>
        /// </example>
        /// </para>
        /// </summary>
        public Expression? QueryExpression { get; set; }

        /// <summary>
        /// Specify name of property or field to store association value, loaded using <see cref="LinqExtensions.LoadWith{TEntity,TProperty}(System.Linq.IQueryable{TEntity},Expression{Func{TEntity,TProperty}})"/> method.
        /// When not specified, current association member will be used.
        /// </summary>
        public string? Storage { get; set; }

        /// <summary>
        /// Specifies static property or method without parameters, that returns a setter expression.
        /// If is set, it will be used to set the storage member when using LoadWith().
        /// Result of method should be Action which takes two parameters: the storage member and the value to assign to it.
        /// <para>
        /// <example>
        /// <code>
        /// public class SomeEntity
        /// {
        ///     [Association(SetExpressionMethod = nameof(OtherImpl), CanBeNull = true)]
        ///     public SomeOtherEntity Other { get; set; }
        ///
        ///     public static Expression&lt;Action&lt;SomeContainerType,SomeOtherEntity&gt;&gt; OtherImpl()
        ///     {
        ///         return (container, value) =&gt; container.Value = value;
        ///     }
        /// }
        /// </code>
        /// </example>
        /// </para>
        /// </summary>
        public string? AssociationSetterExpressionMethod { get; set; }

        /// <summary>
        /// Specifies a setter expression. If is set, it will be used to set the storage member when using LoadWith().
        /// Action takes two parameters: the storage member and the value to assign to it.
        /// <para>
        /// <example>
        /// <code>
        /// Expression&lt;Action&lt;SomeContainerType,SomeOtherEntity&gt;&gt; setContainerValue;
        /// <para />
        /// setContainerValue = (container, value) =&gt; container.Value = value;
        /// </code>
        /// </example>
        /// </para>
        /// </summary>
        public Expression? AssociationSetterExpression { get; set; }

        internal bool? ConfiguredCanBeNull;
        /// <summary>
        /// Defines type of join:
        /// - inner join for <c>CanBeNull = false</c>;
        /// - outer join for <c>CanBeNull = true</c>.
        /// When using Configuration.UseNullableTypesMetadata, the default value
        /// for associations (cardinality 1) is derived from nullability.
        /// Otherwise the default value is <c>true</c> (for collections and when option is disabled).
        /// </summary>
        public bool CanBeNull
        {
            get => ConfiguredCanBeNull ?? true;
            set => ConfiguredCanBeNull = value;
        }

        /// <summary>
        /// Gets or sets alias for association. Used in SQL generation process.
        /// </summary>
        public string? AliasName { get; set; }
    }
}
