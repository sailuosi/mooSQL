
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
            _selectAutoIncrement = "SELECT Last_Insert_Id()";
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
                sb.Append("DISTINCT ");
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

            AppendLimitOffset(sb, frag);

            return sb.ToString();
        }

        public override string buildPagedSelect(FragSQL frag)
        {
            if (!HasSkipTakePaging(frag))
                return base.buildPagedSelect(frag);

            var skip = ResolveSkipNum(frag);
            var take = ResolveTake(frag);
            return buildPagedSelectTail(frag, sb =>
            {
                if (skip >= 0 && take >= 0)
                {
                    sb.Append("LIMIT ");
                    sb.Append(skip);
                    sb.Append(" ,");
                    sb.Append(take);
                }
                else if (take >= 0)
                {
                    sb.Append("LIMIT ");
                    sb.Append(take);
                    sb.Append(' ');
                }
            });
        }



    }
}
