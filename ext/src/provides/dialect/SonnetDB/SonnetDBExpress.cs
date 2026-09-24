#if NET10_0_OR_GREATER
using mooSQL.data.builder;
using System;
using System.Text;

namespace mooSQL.data
{
    /// <summary>
    /// SonnetDB SQL 方言：LIMIT/OFFSET、双引号标识符、@ 命名参数、AUTO_INCREMENT + RETURNING。
    /// </summary>
    public class SonnetDBExpress : SQLExpression
    {
        public SonnetDBExpress(Dialect dia) : base(dia)
        {
            _paraPrefix = "@";
            // 无连接级 LAST_INSERT_ID；自增走 AUTO_INCREMENT + RETURNING
            _selectAutoIncrement = "";
            _provideType = "SonnetDB.Data.SndbProviderFactory,SonnetDB.Data";
        }

        public override string wrapKeyword(string value)
        {
            if (value.StartsWith("\"") && value.EndsWith("\""))
                return value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        public override string charIndex(string substring, string str)
            => $"INSTR({str}, {substring})";

        public override string charIndex(string substring, string str, string start)
            => $"(CASE WHEN {start} <= 0 THEN 0 ELSE INSTR(SUBSTRING({str}, {start}), {substring}) + {start} - 1 END)";

        public override string isNullOrWhiteSpace(string expr)
            => $"({expr} IS NULL OR TRIM({expr}) = '')";

        public override string substring(string expr, string start, string? length = null)
            => length == null ? $"SUBSTRING({expr}, {start})" : $"SUBSTRING({expr}, {start}, {length})";

        // SonnetDB 关系侧日期以 DATETIME / Unix 毫秒为主；差分用毫秒差再换算
        public override string dateDiffDay(string start, string end)
            => $"CAST(({end} - {start}) / 86400000 AS INT)";

        public override string dateDiffHour(string start, string end)
            => $"CAST(({end} - {start}) / 3600000 AS INT)";

        public override string dateDiffMinute(string start, string end)
            => $"CAST(({end} - {start}) / 60000 AS INT)";

        public override string dateDiffSecond(string start, string end)
            => $"CAST(({end} - {start}) / 1000 AS INT)";

        public override string dateDiffMillisecond(string start, string end)
            => $"CAST(({end} - {start}) AS INT)";

        public override string dateDiffYear(string start, string end)
            => $"CAST(({end} - {start}) / 31536000000 AS INT)";

        public override string dateDiffMonth(string start, string end)
            => $"CAST(({end} - {start}) / 2592000000 AS INT)";

        public override string dateDiffWeek(string start, string end)
            => $"CAST(({end} - {start}) / 604800000 AS INT)";

        public override string buildSelect(FragSQL frag)
        {
            var sb = new StringBuilder();
            sb.Append("SELECT ");
            if (frag.distincted)
                sb.Append("DISTINCT ");
            sb.Append(frag.selectInner);
            buildSelectFromToOrderPart(frag, sb);
            AppendLimitOffset(sb, frag);
            return sb.ToString();
        }

        protected override string WrapExistScalar(string existsSubquery)
            => $"SELECT EXISTS({existsSubquery})";

        protected override string AppendExistSubqueryTail(string innerSql, FragSQL frag)
            => innerSql + " LIMIT 1";

        public override string buildPagedSelect(FragSQL frag) => buildSelect(frag);

        public override string buildInsert(FragSQL frag)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("INSERT INTO {0} ", frag.insertInto);
            if (!string.IsNullOrWhiteSpace(frag.insertCols))
                sb.AppendFormat(" ({0}) ", frag.insertCols);

            if (frag.insertValues != null && frag.insertValues.Count > 0)
            {
                sb.AppendFormat(" VALUES ({0})", string.Join("),(", frag.insertValues));
                return sb.ToString();
            }

            if (!string.IsNullOrWhiteSpace(frag.fromInner) || !string.IsNullOrWhiteSpace(frag.selectInner))
            {
                sb.Append(" SELECT ");
                if (frag.distincted)
                    sb.Append("DISTINCT ");
                if (!string.IsNullOrWhiteSpace(frag.selectInner))
                    sb.AppendFormat(" {0} ", frag.selectInner);
                else
                    sb.AppendFormat(" {0} ", frag.insertValue);

                if (!string.IsNullOrWhiteSpace(frag.fromInner))
                {
                    sb.AppendFormat(" FROM {0} ", frag.fromInner);
                    if (!string.IsNullOrWhiteSpace(frag.whereInner))
                        sb.AppendFormat(" WHERE {0} ", frag.whereInner);
                    if (!string.IsNullOrWhiteSpace(frag.groupByInner))
                    {
                        sb.Append("GROUP BY ");
                        sb.Append(frag.groupByInner);
                        sb.Append(' ');
                    }
                    if (!string.IsNullOrWhiteSpace(frag.havingInner))
                    {
                        sb.Append("HAVING ");
                        sb.Append(frag.havingInner);
                        sb.Append(' ');
                    }
                }
                return sb.ToString();
            }

            if (!string.IsNullOrWhiteSpace(frag.insertValue))
            {
                sb.AppendFormat(" VALUES ({0}) ", frag.insertValue);
                return sb.ToString();
            }
            throw new Exception("SQL语句不完整！无法构造！");
        }

        public override string buildMergeInto(FragMergeInto frag)
            => throw new NotSupportedException("SonnetDB 不支持 MERGE INTO。");

        #region DDL

        public override string getTableAutoIdSQL() => "AUTO_INCREMENT";

        public override string CreateDataBaseBy(string database) => string.Empty;

        public override string AddPrimaryKeyBy(string tableName, string columnName, string indexName)
            => string.Format("ALTER TABLE {0} ADD PRIMARY KEY ({1})", tableName, columnName);

        public override string AddColumnToTableBy(string tableName, string columnName, string dataType, string defval, string nullable, string p2, string p3)
            => string.Format("ALTER TABLE {0} ADD COLUMN {1} {2}{3} {4} {5} {6}",
                tableName, columnName, dataType, defval, nullable, p2, p3);

        public override string AlterColumnToTableby(string tableName, string columnName, string dataType, string defval, string nullable, string p2, string p3)
            => string.Format("ALTER TABLE {0} ALTER COLUMN {1} TYPE {2}", tableName, columnName, dataType);

        public override string CreateTableBy(string tableName, string detail)
            => string.Format("CREATE TABLE {0}(\r\n{1})", tableName, detail);

        public override string CreateTableColumnBy(string columnName, string dataType, string defval, string nullable, string p2, string p3)
            => string.Format("{0} {1}{2} {3} {4} {5}",
                columnName,
                dataType ?? string.Empty,
                defval ?? string.Empty,
                nullable ?? string.Empty,
                p2 ?? string.Empty,
                p3 ?? string.Empty
            ).Trim();

        public override string DropColumnToTableBy(string tableName, string columnName)
            => string.Format("ALTER TABLE {0} DROP COLUMN {1}", tableName, columnName);

        public override string DropConstraintBy(string tableName, string constraintName)
            => string.Format("ALTER TABLE {0} DROP CONSTRAINT {1}", tableName, constraintName);

        public override string RenameColumnBy(string tableName, string oldName, string newName)
            => string.Format("ALTER TABLE {0} RENAME COLUMN {1} TO {2}", tableName, oldName, newName);

        public override string CreateTableNullBy() => "NULL";
        public override string CreateTableNotNullBy() => "NOT NULL";
        public override string CreateTablePirmaryKeyBy() => "PRIMARY KEY";

        public override string AddTableCaptionBy(string tableName, string caption)
            => string.Empty;

        public override string UpdateTableCaptionBy(string tableName, string caption)
            => string.Empty;

        public override string DeleteTableCaptionBy(string tableName)
            => string.Empty;

        public override string AddColumnCaptionBy(string tableName, string columnName, string caption)
            => string.Empty;

        public override string UpdateColumnCaptionBy(string tableName, string columnName, string caption)
            => string.Empty;

        public override string DeleteColumnCaptionBy(string tableName, string columnName)
            => string.Empty;

        public override string RenameTableBy(string oldTableName, string newTableName)
            => string.Format("ALTER TABLE {0} RENAME TO {1}", oldTableName, newTableName);

        public override string CreateIndexBy(string indexName, string tableName, string columnName, string unique)
            => string.Format("CREATE {3} INDEX {2} ON {0} ({1})", tableName, columnName, indexName, unique);

        public override string IsAnyIndexBy(string indexName)
            => string.Format(
                "SELECT COUNT(*) FROM information_schema.statistics WHERE index_name = '{0}'",
                indexName);

        public override string buildDropIndex(string indexName, string tableName = null)
            => string.Format("DROP INDEX {0}", indexName);

        #endregion
    }
}
#endif
