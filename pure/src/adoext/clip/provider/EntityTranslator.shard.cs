using System;
using mooSQL.data.model;

namespace mooSQL.data
{
    public partial class EntityTranslator
    {
        /// <summary>
        /// 解析实体对应的物理表名（供仓储等外部调用）。
        /// </summary>
        public string GetResolvedTableName(EntityInfo en, object row = null, Func<string> loadName = null)
        {
            return resolveTableName(en, loadName, row);
        }

        /// <summary>
        /// 按时间范围构建 SELECT + UNION FROM。
        /// </summary>
        public SQLBuilder BuildSelectFromRange(
            SQLBuilder kit,
            EntityInfo en,
            DateTime from,
            DateTime to,
            ShardQueryOptions options = null)
        {
            options ??= ShardQueryOptions.ForRange(from, to);
            if (options.RangeFrom == null)
                options.RangeFrom = from;
            if (options.RangeTo == null)
                options.RangeTo = to;

            if (en.DType != DBTableType.Table)
            {
                BuildSelectFrom(kit, en);
                return kit;
            }

            BuildSelectColumns(kit, en);
            ShardQueryBuilder.BuildUnionFrom(kit, en, options);
            return kit;
        }

        private void BuildSelectColumns(SQLBuilder kit, EntityInfo en)
        {
            var exp = kit.Dialect.expression;
            foreach (var field in en.Columns)
            {
                if (CheckEdition(kit.DBLive, field) == false)
                    continue;
                if (field.Kind == FieldKind.Base && !string.IsNullOrWhiteSpace(field.DbColumnName))
                    kit.select(exp.wrapField(field.DbColumnName));
            }
        }
    }
}
