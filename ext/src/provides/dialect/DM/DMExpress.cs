using mooSQL.data.builder;
using System;
using System.Text;

namespace mooSQL.data
{
    /// <summary>
    /// 达梦表达式：冒号参数、双引号标识符、LIMIT/OFFSET 分页、标准多行 VALUES、MERGE、DUAL。
    /// </summary>
    public class DMExpress : SQLExpression
    {
        public DMExpress(Dialect dia) : base(dia)
        {
            _paraPrefix = ":";
            _selectAutoIncrement = "";
            _provideType = "Dm.DmClientFactory,DM.DmProvider";
        }

        public override string wrapKeyword(string value)
        {
            if (value.StartsWith("\"") && value.EndsWith("\""))
                return value;
            return "\"" + value + "\"";
        }

        public override int? getWhereInLimit() => 1000;

        public override string charIndex(string substring, string str)
            => $"INSTR({str}, {substring})";

        public override string charIndex(string substring, string str, string start)
            => $"INSTR({str}, {substring}, {start})";

        public override string isNullOrWhiteSpace(string expr)
            => $"({expr} IS NULL OR LTRIM({expr}, ' \t\n\r\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u205f\u3000') IS NULL)";

        public override string dateDiffWeek(string start, string end)
            => $"(CAST ({end} as DATE) - CAST ({start} as DATE)) / 7";

        public override string dateDiffDay(string start, string end)
            => $"(CAST ({end} as DATE) - CAST ({start} as DATE))";

        public override string dateDiffHour(string start, string end)
            => $"(CAST ({end} as DATE) - CAST ({start} as DATE)) * 24";

        public override string dateDiffMinute(string start, string end)
            => $"(CAST ({end} as DATE) - CAST ({start} as DATE)) * 1440";

        public override string dateDiffSecond(string start, string end)
            => $"(CAST ({end} as DATE) - CAST ({start} as DATE)) * 86400";

        public override string dateDiffMillisecond(string start, string end)
            => "1000 * (EXTRACT(SECOND FROM CAST (" + end + " as TIMESTAMP) - CAST (" + start + " as TIMESTAMP))"
               + " + 60 * (EXTRACT(MINUTE FROM CAST (" + end + " as TIMESTAMP) - CAST (" + start + " as TIMESTAMP))"
               + " + 60 * (EXTRACT(HOUR FROM CAST (" + end + " as TIMESTAMP) - CAST (" + start + " as TIMESTAMP))"
               + " + 24 * EXTRACT(DAY FROM CAST (" + end + " as TIMESTAMP) - CAST (" + start + " as TIMESTAMP)))))";

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
            => $"SELECT CASE WHEN EXISTS ({existsSubquery}) THEN 1 ELSE 0 END FROM DUAL";

        protected override string AppendExistSubqueryTail(string innerSql, FragSQL frag)
            => innerSql + " LIMIT 1";

        public override string buildPagedSelect(FragSQL frag)
            => HasSkipTakePaging(frag) ? buildSelect(frag) : base.buildPagedSelect(frag);

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
                        sb.Append(" ");
                    }
                    if (!string.IsNullOrWhiteSpace(frag.havingInner))
                    {
                        sb.Append("HAVING ");
                        sb.Append(frag.havingInner);
                        sb.Append(" ");
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
            => buildMergeIntoGeneral(frag);

        #region DDL

        protected override string buildConstrainPK(string pkname, string fields)
            => string.Format("CONSTRAINT {0} PRIMARY KEY ({1})", pkname, fields);

        protected override string buildDDLFieldsCaption(DDLFragSQL frag)
            => buildDDLSoloCaptions(frag);

        public override string buildSoloFieldCaption(DDLFragSQL frag, DDLField fie)
            => string.Format("COMMENT ON COLUMN {0}.{1} IS '{2}';", frag.Table, fie.FieldName, fie.Caption);

        public override string buildSoloTableCaption(DDLFragSQL frag)
            => string.Format("COMMENT ON TABLE {0} IS '{1}';", frag.Table, frag.TableCaption);

        public override string buildAlterView(DDLFragSQL frag)
        {
            var sb = new StringBuilder();
            sb.Append("CREATE OR REPLACE VIEW ")
                .Append(frag.Table)
                .Append(" AS ")
                .Append(frag.SelectSQL);
            return sb.ToString();
        }

        public override string buildCopyTableSchema(DDLFragSQL frag)
            => string.Format("CREATE TABLE {0} AS SELECT * FROM {1} WHERE 1 = 0", frag.Table, frag.SrcTable);

        public override string buildCopyTable(DDLFragSQL frag)
            => string.Format("CREATE TABLE {0} AS SELECT * FROM {1} ", frag.Table, frag.SrcTable);

        public override string buildDropIndex(string indexName, string tableName = null)
            => string.Format("DROP INDEX {0}", indexName);

        public override string CreateIndexBy(string indexName, string tableName, string columnName, string unique)
            => string.Format("CREATE {3} INDEX Index_{0}_{2} ON {0}({1})", tableName, columnName, indexName, unique);

        public override string IsAnyIndexBy(string indexName)
            => string.Format("SELECT COUNT(1) FROM user_ind_columns WHERE index_name=('{0}')", indexName);

        public override string CreateDataBaseBy(string database)
            => string.Format("CREATE DATABASE {0}", database);

        public override string AddPrimaryKeyBy(string tableName, string columnName, string indexName)
            => string.Format("ALTER TABLE {0} ADD CONSTRAINT {1} PRIMARY KEY({2})", tableName, indexName, columnName);

        public override string AddColumnToTableBy(string tableName, string columnName, string dataType, string defval, string nullable, string p2, string p3)
            => string.Format("ALTER TABLE {0} ADD ({1} {2}{3} {4} {5} {6})",
                tableName, columnName, dataType, defval, nullable, p2, p3);

        public override string AlterColumnToTableby(string tableName, string columnName, string dataType, string defval, string nullable, string p2, string p3)
            => string.Format("ALTER TABLE {0} MODIFY ({1} {2}{3} {4} {5} {6}) ",
                tableName, columnName, dataType, defval, nullable, p2, p3);

        public override string CreateTableBy(string tableName, string detail)
            => string.Format("CREATE TABLE {0}(\r\n{1})", tableName, detail);

        public override string CreateTableColumnBy(string columnName, string dataType, string defval, string nullable, string p2, string p3)
            => string.Format("{0} {1}{2} {3} {4} {5}", columnName, dataType, defval, nullable, p2, p3);

        public override string DropColumnToTableBy(string tableName, string columnName)
            => string.Format("ALTER TABLE {0} DROP COLUMN {1}", tableName, columnName);

        public override string DropConstraintBy(string tableName, string constraintName)
            => string.Format("ALTER TABLE {0} DROP CONSTRAINT  {1}", tableName, constraintName);

        public override string RenameColumnBy(string tableName, string oldName, string newName)
            => string.Format("ALTER TABLE {0} RENAME COLUMN {1} TO {2}", tableName, oldName, newName);

        public override string AddColumnCaptionBy(string tableName, string columnName, string caption)
            => string.Format("COMMENT ON COLUMN {1}.{0} IS '{2}'", columnName, tableName, caption);

        public override string UpdateColumnCaptionBy(string tableName, string columnName, string caption)
            => AddColumnCaptionBy(tableName, columnName, caption);

        public override string DeleteColumnCaptionBy(string tableName, string columnName)
            => string.Format("COMMENT ON COLUMN {1}.{0} IS ''", columnName, tableName);

        public override string IsAnyColumnCaptionBy(string tableName, string columnName)
            => string.Format("SELECT * FROM user_col_comments WHERE Table_Name='{1}' AND COLUMN_NAME='{0}' ORDER BY column_name",
                columnName, tableName);

        public override string AddTableCaptionBy(string tableName, string caption)
            => string.Format("COMMENT ON TABLE {0} IS '{1}'", tableName, caption);

        public override string UpdateTableCaptionBy(string tableName, string caption)
            => AddTableCaptionBy(tableName, caption);

        public override string DeleteTableCaptionBy(string tableName)
            => string.Format("COMMENT ON TABLE {0} IS ''", tableName);

        public override string IsAnyTableCaptionBy(string tableName)
            => string.Format("SELECT * FROM user_tab_comments WHERE Table_Name='{0}'ORDER BY Table_Name", tableName);

        public override string RenameTableBy(string oldTableName, string newTableName)
            => string.Format("ALTER TABLE {0} RENAME TO {1}", oldTableName, newTableName);

        public override string CheckSystemTablePermissionsBy()
            => "SELECT t.table_name FROM user_tables t WHERE ROWNUM=1";

        public override string CreateTableNullBy() => "";

        public override string CreateTableNotNullBy() => " NOT NULL ";

        public override string CreateTablePirmaryKeyBy() => "PRIMARY KEY";

        /// <summary>达梦 IDENTITY 列：取当前会话最近插入自增值。</summary>
        public override string getTableAutoIdSQL()
            => "SELECT IDENT_CURRENT(NULL) FROM DUAL";

        #endregion
    }
}
