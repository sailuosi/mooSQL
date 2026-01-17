
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



    }
}
