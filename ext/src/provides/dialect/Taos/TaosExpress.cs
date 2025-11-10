using mooSQL.data.builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace mooSQL.data
{
    public class TaosExpress : SQLExpression
    {
        public TaosExpress(Dialect dia) : base(dia)
        {
            _paraPrefix = "";
            _selectAutoIncrement = "";
            _provideType = "";
        }

        public override string wrapKeyword(string value)
        {
            if (value.StartsWith("`") && value.EndsWith("`"))
            {
                return value;
            }
            return "`" + value.Replace("`", "``") + "`";
        }

        public override string buildSelect(FragSQL frag)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT ");
            if (frag.distincted)
            {
                sb.Append("distinct ");
            }
            sb.Append(frag.selectInner);
            //如果使用了行号函数
            if (frag.hasRowNumber)
            {
                var t = buildRowNumber(frag);
                if (!string.IsNullOrWhiteSpace(t))
                {
                    if (!string.IsNullOrWhiteSpace(frag.selectInner))
                    {
                        sb.Append(",");
                    }
                    sb.Append(t);
                }

            }

            this.buildSelectFromToOrderPart(frag, sb);

            if (frag.toped > -1)
            {
                sb.Append("LIMIT ");
                sb.Append(frag.toped);
                sb.Append(" ");
            }

            return sb.ToString();
        }

        public override string buildInsert(FragSQL frag)
        {
            StringBuilder sb = new StringBuilder();
            // sql server 支持直接插入多行数据、单行数据
            sb.AppendFormat("INSERT INTO {0} ", frag.insertInto);
            if (string.IsNullOrWhiteSpace(frag.insertCols) == false)
            {
                sb.AppendFormat(" ({0}) ", frag.insertCols);
            }

            if (frag.insertValues != null && frag.insertValues.Count > 0)
            {
                //多行插入
                sb.Append(" VALUES ");
                for (var i = 0; i < frag.insertValues.Count; i++)
                {
                    sb.AppendFormat(" SELECT {0} from dual ", frag.insertValues[i]);
                    if (i < frag.insertValues.Count - 1)
                    {
                        sb.Append("UNION");
                    }
                }
                return sb.ToString();
            }
            //如果 from 不为空，则是 insert into  select...
            if (!string.IsNullOrWhiteSpace(frag.fromInner) || !string.IsNullOrWhiteSpace(frag.selectInner))
            {
                //此时的单行插入值，实际上是select 部分。但是，如果明确给了 select内容，则使用 select内容
                sb.Append(" select ");
                if (frag.distincted)
                {
                    sb.Append("distinct ");
                }
                if (!string.IsNullOrWhiteSpace(frag.selectInner))
                {
                    sb.AppendFormat(" {0} ", frag.selectInner);
                }
                else
                {
                    sb.AppendFormat(" {0} ", frag.insertValue);
                }
                //追加from 部分。
                if (!string.IsNullOrWhiteSpace(frag.fromInner))
                {
                    sb.AppendFormat(" FROM {0} ", frag.fromInner);

                    //带from 时，才允许追加 where条件
                    if (!string.IsNullOrWhiteSpace(frag.whereInner))
                    {
                        sb.AppendFormat(" WHERE {0} ", frag.whereInner);
                    }
                    if (!string.IsNullOrWhiteSpace(frag.groupByInner))
                    {
                        sb.Append("group by ");
                        sb.Append(frag.groupByInner);
                        sb.Append(" ");
                    }
                    if (!string.IsNullOrWhiteSpace(frag.havingInner))
                    {
                        sb.Append("having ");
                        sb.Append(frag.havingInner);
                        sb.Append(" ");
                    }
                }

                return sb.ToString();
            }
            //如果是单行插入
            if (!string.IsNullOrWhiteSpace(frag.insertValue))
            {
                sb.AppendFormat(" VALUES ({0}) ", frag.insertValue);
                return sb.ToString();
            }
            throw new Exception("SQL语句不完整！无法构造！");
        }

        public override string buildMergeInto(FragMergeInto frag)
        {
            //merge into table_name alias1   --目标表 可以用别名
            //using (table | view | sub_query) alias2--数据源表 可以是表、视图、子查询
            //on(join condition)   --关联条件
            //when matched then--当关联条件成立时 更新，删除，插入的
            //where部分为可选--更新
            //update  table_name set  col1 = colvalue  where……   --删除
            //delete from table_name where  col2 = colvalue  where…… --可以只更新不删除 也可以只删除不更新。 --如果更新和删除同时存在，删除的条件一定要在更新的条件内，否则数据不能删除。
            //when not matched then      --当关联条件不成立时--插入 insert(col3) values(col3values)  where……   when not matched by source then      --当源表不存在，目标表存在的数据删除
            //delete;
            return this.buildMergeIntoGeneral(frag);
        }

        #region DDL


        public override string getTableAutoIdSQL()
        {
            return "AUTO_INCREMENT";
        }

        public override string CreateDataBaseBy(string database)
        {
            return string.Format("CREATE DATABASE {0} CHARACTER SET utf8 COLLATE utf8_general_ci ", database);
        }
        public override string AddPrimaryKeyBy(string tableName, string columnName, string indexName)
        {
            return string.Format("ALTER TABLE {0} ADD PRIMARY KEY({2}) /*{1}*/", tableName, indexName, columnName);
        }
        public override string AddColumnToTableBy(string tableName, string columnName, string dataType, string defval, string nullable, string p2, string p3)
        {
            return string.Format("ALTER TABLE {0} ADD {1} {2}{3} {4} {5} {6}",
                tableName, columnName, dataType,
                defval, nullable, p2, p3
                );
        }
        public override string AlterColumnToTableby(string tableName, string columnName, string dataType, string defval, string nullable, string p2, string p3)
        {
            return string.Format("alter table {0} change  column {1} {1} {2}{3} {4} {5} {6}",
                tableName, columnName, dataType,
                defval, nullable, p2, p3
                );
        }
        public override string CreateTableBy(string tableName, string detail)
        {
            return string.Format("CREATE TABLE {0}(\r\n{1} $PrimaryKey)", tableName, detail);
        }
        public override string CreateTableColumnBy(string columnName, string dataType, string defval, string nullable, string p2, string p3)
        {
            return string.Format("{0} {1}{2} {3} {4} {5}",
                columnName, dataType,
                defval, nullable, p2, p3
                );
        }
        //protected override string TruncateTableSql (){ "TRUNCATE TABLE {0}";

        //protected override string DropTableSql (){ "DROP TABLE {0}";

        public override string DropColumnToTableBy(string tableName, string columnName)
        {
            return string.Format("ALTER TABLE {0} DROP COLUMN {1}", tableName, columnName);
        }
        public override string DropConstraintBy(string tableName, string constraintName)
        {
            return string.Format("ALTER TABLE {0} drop primary key;", tableName, constraintName);
        }
        public override string RenameColumnBy(string tableName, string oldName, string newName)
        {
            return string.Format("alter table {0} change  column {1} {2}", tableName, oldName, newName);
        }
        public override string CheckSystemTablePermissionsBy()
        {
            return "select 1 from Information_schema.columns limit 0,1";
        }
        public override string CreateTableNullBy()
        {
            return "NULL";
        }
        public override string CreateTableNotNullBy()
        {
            return "NOT NULL";
        }
        public override string CreateTablePirmaryKeyBy()
        {
            return "PRIMARY KEY";
        }



        public override string AddTableCaptionBy(string tableName, string caption)
        {
            return string.Format("ALTER TABLE {0} COMMENT='{1}';", tableName, caption);
        }
        public override string DeleteTableCaptionBy(string tableName)
        {
            return string.Format("ALTER TABLE {0} COMMENT='';", tableName);
        }

        public override string RenameTableBy(string oldTableName, string newTableName)
        {
            return string.Format("alter table {0} rename {1}", oldTableName, newTableName);
        }
        public override string CreateIndexBy(string indexName, string tableName, string columnName, string unque)
        {
            return string.Format("CREATE {3} INDEX Index_{0}_{2} ON {0} ({1})", tableName, columnName, indexName, unque);
        }
        public override string IsAnyIndexBy(string indexName)
        {
            return string.Format("SELECT count(*) FROM information_schema.statistics WHERE index_name = '{0}'", indexName);
        }
        #endregion
    }
}

