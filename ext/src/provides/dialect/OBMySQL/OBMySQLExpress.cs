
using mooSQL.data.builder;
using mooSQL.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// MYSQL的特性语法
    /// </summary>
    public class OBMySQLExpress : MySQLExpress
    {
        public OBMySQLExpress(Dialect dia) : base(dia)
        {
            _paraPrefix = "?";
            _selectAutoIncrement = "Select Last_Insert_Id()";
            _provideType = "MySql.Data.MySqlClient.MySqlClientFactory,MySql.Data";
        }

        /// <summary>
        /// 创建普通的select语句
        /// </summary>
        /// <param name="frag"></param>
        /// <returns></returns>
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
            if (frag.hasRowNumber) {
                var t = buildRowNumber(frag);
                if (!string.IsNullOrWhiteSpace(t)) {
                    if (!string.IsNullOrWhiteSpace(frag.selectInner)) {
                        sb.Append(",");
                    }
                    sb.Append(t);
                }
                
            }
            buildSelectFromToOrderPart(frag, sb);

            if (frag.toped > -1)
            {
                sb.Append("limit ");
                sb.Append(frag.toped);
                sb.Append(" ");
            }

            return sb.ToString();
        }

        public override string buildPagedSelect(FragSQL frag)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT ");
            if (frag.distincted)
            {
                sb.Append("distinct ");
            }
            sb.Append(frag.selectInner);
            sb.Append(" ");
            sb.Append("from ");
            sb.Append(frag.fromInner);
            sb.Append(" ");
            if (!string.IsNullOrWhiteSpace(frag.whereInner))
            {
                sb.Append("where ");
                sb.Append(frag.whereInner);
                sb.Append(" ");
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

            if (!string.IsNullOrWhiteSpace(frag.rowNumberOrderBy))
            {
                sb.Append("order by ");
                sb.Append(frag.rowNumberOrderBy);
                sb.Append(" ");
            }
            else if (!string.IsNullOrWhiteSpace(frag.orderbyInner))
            {
                sb.Append("order by ");
                sb.Append(frag.orderbyInner);
                sb.Append(" ");
            }

            if (frag.pageSize > -1) {
                int end = frag.pageSize * (frag.pageNum - 1);
                sb.Append("limit ");
                sb.Append(end);
                sb.Append(" ,");
                sb.Append(frag.pageSize);

            }
            else if (frag.toped > -1)
            {
                sb.Append("limit ");
                sb.Append(frag.toped);
                sb.Append(" ");
            }

            return sb.ToString();
        }

        /// <summary>
        /// 创建普通的插值语句
        /// </summary>
        /// <param name="frag"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
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
                sb.AppendFormat(" VALUES ({0})", string.Join("),(", frag.insertValues));
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
        /// <summary>
        /// 使mysql的update from 语句，完全支持sqlserver的格式。
        /// </summary>
        /// <param name="frag"></param>
        /// <returns></returns>
        public override string buildUpdateFrom(FragSQL frag)
        {
            /**
             * 创建MYSQL下的update from 必须使用inner join
             * @return update tablename inner join a on a.pid=tablename.id set ... where ...
             */
            var sb = new StringBuilder();
            // update a set a=b from ... where ...
            //将left join 更改为inner join 
            if (RegxUntils.test(frag.fromInner.ToLower(), @"\sleft\s+join\s"))
            {
                var reg = new Regex(@"\sleft\s+join\s", RegexOptions.IgnoreCase);
                frag.fromInner = reg.Replace(frag.fromInner, " inner join ");// .ToLower().Replace(@"\sleft\s+join\s"," inner join ");
            }
            sb.AppendFormat("update {0} set ", frag.fromInner);
            if (!string.IsNullOrWhiteSpace(frag.updateTo))
            {
                //如果设置了目标则作适配性修正。如果set列没有表前缀，增加表前缀。
                List<string> fiexedset = new List<string>();
                if (frag.setInner.Contains(","))
                {
                    var sets = frag.setInner.Split(',');

                    foreach (var s in sets)
                    {
                        var t = fixSetField(s, frag);
                        if (!string.IsNullOrWhiteSpace(t))
                        {
                            fiexedset.Add(t);
                        }

                    }

                }
                else
                {
                    var t = fixSetField(frag.setInner, frag);
                    if (!string.IsNullOrWhiteSpace(t))
                    {
                        fiexedset.Add(t);
                    }
                }
                //没有设置列信息，返回空
                if (fiexedset.Count == 0)
                {
                    return "";
                }
                sb.Append(" ");
                sb.Append(string.Join(",", fiexedset));
                sb.Append(" ");

            }
            else
            {

            }

            if (!string.IsNullOrWhiteSpace(frag.whereInner))
            {
                sb.AppendFormat(" where {0}", frag.whereInner);
            }
            return sb.ToString();
        }

        private string fixSetField(string setOne, FragSQL frag)
        {
            var fieldsp = setOne.Split('=');
            if (fieldsp.Length != 2)
            {
                return "";
            }

            var field = fieldsp[0];

            if (field.Contains(".") == false)
            {
                //要赋值的字段没有表前缀
                var t = frag.updateTo + "." + setOne.TrimStart();
                return t;
            }
            return setOne;
        }
    }
}
