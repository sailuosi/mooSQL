
using mooSQL.data.builder;
using System;
using System.Text;

namespace mooSQL.data
{
    /// <summary>
    /// Jet SQL (Access / Excel) 表达式方言：TOP、@@IDENTITY、方括号标识符，不支持 ROW_NUMBER/PIVOT/UNPIVOT。
    /// </summary>
    public class JetSQLExpress : SQLExpression
    {
        public JetSQLExpress(Dialect dia) : base(dia)
        {
            // 占位符仍用 @ 生成，执行前由 JetSQLDialect.addCmdPara 按位替换为 ?
            _paraPrefix = "@";
            _selectAutoIncrement = "SELECT @@IDENTITY";
            _provideType = "System.Data.OleDb.OleDbFactory,System.Data.OleDb";
        }

        public override string wrapKeyword(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            if (value.StartsWith("[") && value.EndsWith("]")) return value;
            return "[" + value + "]";
        }

        public override string buildSelect(FragSQL frag)
        {
            var sb = new StringBuilder();
            sb.Append("SELECT ");
            if (frag.distincted) sb.Append("DISTINCT ");
            if (frag.toped > -1)
            {
                sb.Append("TOP ");
                sb.Append(frag.toped);
                sb.Append(" ");
            }
            sb.Append(frag.selectInner);
            sb.Append(" ");
            buildSelectFromToOrderPart(frag, sb);
            return sb.ToString();
        }

        public override string buildInsert(FragSQL frag)
        {
            var sb = new StringBuilder();
            sb.Append("INSERT INTO ");
            sb.Append(frag.insertInto);
            sb.Append(" ");
            if (!string.IsNullOrWhiteSpace(frag.insertCols))
                sb.Append("(").Append(frag.insertCols).Append(") ");
            if (frag.insertValues != null && frag.insertValues.Count > 0)
            {
                sb.Append("VALUES (").Append(string.Join("),(", frag.insertValues)).Append(")");
                return sb.ToString();
            }
            if (!string.IsNullOrWhiteSpace(frag.fromInner) || !string.IsNullOrWhiteSpace(frag.selectInner))
            {
                sb.Append("SELECT ");
                if (frag.distincted) sb.Append("DISTINCT ");
                sb.Append(string.IsNullOrWhiteSpace(frag.selectInner) ? frag.insertValue : frag.selectInner);
                if (!string.IsNullOrWhiteSpace(frag.fromInner))
                {
                    sb.Append(" FROM ").Append(frag.fromInner);
                    if (!string.IsNullOrWhiteSpace(frag.whereInner))
                        sb.Append(" WHERE ").Append(frag.whereInner);
                    if (!string.IsNullOrWhiteSpace(frag.groupByInner))
                        sb.Append(" GROUP BY ").Append(frag.groupByInner);
                    if (!string.IsNullOrWhiteSpace(frag.havingInner))
                        sb.Append(" HAVING ").Append(frag.havingInner);
                }
                return sb.ToString();
            }
            if (!string.IsNullOrWhiteSpace(frag.insertValue))
            {
                sb.Append("VALUES (").Append(frag.insertValue).Append(")");
                return sb.ToString();
            }
            throw new Exception("SQL语句不完整！无法构造！");
        }

        /// <summary>
        /// Jet 无 OFFSET/ROW_NUMBER，仅支持 TOP；翻页由调用方或 BulkInsertByInsertValues 等处理，此处仅按 TOP 生成。
        /// </summary>
        public override string buildPagedSelect(FragSQL frag)
        {
            if (frag.pageSize > -1 && frag.pageNum == 1)
                frag.toped = frag.pageSize;
            return buildSelect(frag);
        }

        public override string getTableAutoIdSQL()
        {
            return "AUTOINCREMENT";
        }

        public override string CreateDataBaseBy(string database)
        {
            return string.Empty;
        }

        public override string AddPrimaryKeyBy(string tableName, string columnName, string indexName)
        {
            return string.Format("ALTER TABLE {0} ADD CONSTRAINT {1} PRIMARY KEY ({2})", tableName, indexName, columnName);
        }

        public override string AddColumnToTableBy(string tableName, string columnName, string dataType, string defval, string nullable, string p2, string p3)
        {
            return string.Format("ALTER TABLE {0} ADD {1} {2} {3} {4}", tableName, columnName, dataType, defval, nullable);
        }

        public override string AlterColumnToTableby(string tableName, string columnName, string dataType, string defval, string nullable, string p2, string p3)
        {
            return string.Format("ALTER TABLE {0} ALTER COLUMN {1} {2} {3} {4}", tableName, columnName, dataType, defval, nullable);
        }

        public override string CreateTableBy(string tableName, string detail)
        {
            return string.Format("CREATE TABLE {0} ({1})", tableName, detail);
        }

        public override string CreateTableColumnBy(string columnName, string dataType, string defval, string nullable, string p2, string p3)
        {
            return string.Format("{0} {1} {2} {3}", columnName, dataType, defval, nullable);
        }

        public override string DropColumnToTableBy(string tableName, string columnName)
        {
            return string.Format("ALTER TABLE {0} DROP COLUMN {1}", tableName, columnName);
        }

        public override string DropConstraintBy(string tableName, string constraintName)
        {
            return string.Format("ALTER TABLE {0} DROP CONSTRAINT {1}", tableName, constraintName);
        }

        public override string CreateTableNullBy() => "NULL";
        public override string CreateTableNotNullBy() => "NOT NULL";
        public override string CreateTablePirmaryKeyBy() => "PRIMARY KEY";
        public override string getBoolColumnType() => "BIT";
        public override string getDateTimeColumnType(int length) => "DATETIME";
        public override string getGuidColumnType() => "GUID";
    }
}
