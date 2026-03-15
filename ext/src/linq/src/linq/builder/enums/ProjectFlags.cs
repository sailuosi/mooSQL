using System;

namespace mooSQL.linq.Linq.Builder
{
    [Flags]
    enum ProjectFlags
    {
        None = 0x00,

        SQL = 0x01,
        Expression = 0x02,
        Root = 0x04,
        /// <summary>
        /// 强制将关联和 GroupJoin 展开为查询表达式
        /// </summary>
        ExtractProjection = 0x08,

        AggregationRoot = 0x10,
        /// <summary>
        /// 指定从整个上下文中我们只需要键字段。
        /// </summary>
        Keys = 0x20,
        /// <summary>
        /// 验证表达式是否可以转换为 SQL。返回值不可使用。
        /// </summary>
        Test = 0x40,
        AssociationRoot = 0x80,
        /// <summary>
        /// 指定我们正在查找一个表
        /// </summary>
        Table = 0x100,
        /// <summary>
        /// 指定我们的关联不应过滤掉记录集
        /// </summary>
        ForceOuterAssociation = 0x200,
        /// <summary>
        /// 指定我们期望在由 Selects 链隐藏的下方有实际表达式
        /// </summary>
        Traverse = 0x800,

        Subquery = 0x1000,

        /// <summary>
        /// 指示生成的 SQL 是用于扩展方法。
        /// </summary>
        ForExtension = 0x2000,
    }
}