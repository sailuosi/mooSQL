using System;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace mooSQL.data
{
    /// <summary>
    /// 实体间关联元数据（对标 CRL RelationInfo；按 Find(父,子) 方向解读 Field1/Field2）。
    /// </summary>
    public sealed class EntityRelationInfo
    {
        /// <summary>父侧（Find 第一参数）类型。</summary>
        public Type Type1 { get; set; }

        /// <summary>子侧（Find 第二参数）类型。</summary>
        public Type Type2 { get; set; }

        /// <summary>父侧关联属性名（→ EntityNavi.BossKey）。</summary>
        public string Field1Name { get; set; }

        /// <summary>子侧关联属性名（→ EntityNavi.SlaveKey）。</summary>
        public string Field2Name { get; set; }

        /// <summary>原始等值表达式 Body（二期 JOIN 可用）。</summary>
        public Expression Expression { get; set; }

        /// <summary>Lambda 参数（二期 JOIN 可用）。</summary>
        public ReadOnlyCollection<ParameterExpression> Parameters { get; set; }

        /// <summary>注册表键 Type1→Type2。</summary>
        public string Key => MakeKey(Type1, Type2);

        /// <summary>构造稳定键。</summary>
        public static string MakeKey(Type type1, Type type2)
        {
            if (type1 == null || type2 == null) return null;
            return (type1.FullName ?? type1.Name) + "|" + (type2.FullName ?? type2.Name);
        }
    }
}
