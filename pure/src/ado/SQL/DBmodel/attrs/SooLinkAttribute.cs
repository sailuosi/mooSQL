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
        /// 对方字段.
        /// </summary>
        public string? ThatKey { get; set; }
        /// <summary>
        /// 关联表名
        /// </summary>
        public string? ThatTable { get; set; }

        /// <summary>
        /// 预测表达式
        /// </summary>
        public string? ExpressionPredicate { get; set; }

        /// <summary>
        /// 推断表达式
        /// </summary>
        public Expression? Predicate { get; set; }

        /// <summary>
        /// </summary>
        public string? QueryExpressionMethod { get; set; }

        /// <summary>
        /// </summary>
        public Expression? QueryExpression { get; set; }

        /// <summary>
        /// </summary>
        public string? Storage { get; set; }

        /// <summary>
        /// </summary>
        public string? AssociationSetterExpressionMethod { get; set; }

        /// <summary>
        /// </summary>
        public Expression? AssociationSetterExpression { get; set; }

        internal bool? ConfiguredCanBeNull;
        /// <summary>
        /// </summary>
        public bool CanBeNull
        {
            get => ConfiguredCanBeNull ?? true;
            set => ConfiguredCanBeNull = value;
        }

        /// <summary>
        /// 关联别名
        /// </summary>
        public string? AliasName { get; set; }
    }
}
