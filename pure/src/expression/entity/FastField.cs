using mooSQL.data.model.affirms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.linq
{
    public class FastBusField
    {
        public EntityColumn Column { get; set; }

        public string CallerNick { get; set; }

        /// <summary>
        /// 转为字段的SQL表示形式
        /// </summary>
        /// <param name="withNick"></param>
        /// <param name="DBLive"></param>
        /// <returns></returns>
        public string ToSQLField(bool withNick,DBInstance DBLive) {
            var fname = Column.DbColumnName;
            if (Column.Kind == model.FieldKind.Base) { 
                if (withNick && !string.IsNullOrWhiteSpace(CallerNick)) {
                    return $"{CallerNick}.{fname}";
                }            
            }
            else if (Column.Kind == model.FieldKind.Join)
            {

                //如果字段为空，则忽略该字段
                if (string.IsNullOrWhiteSpace(Column.SrcField))
                {
                    return null;
                }
                var fie = DBLive.dialect.expression.wrapField(Column.SrcField);
                if (!string.IsNullOrWhiteSpace(Column.SrcTable))
                {
                    fie = string.Format("{0}.{1}", Column.SrcTable, fie);
                }
                return fie;
            }
            else if (Column.Kind == model.FieldKind.Free)
            {
                var fie = Column.FreeSQL;
                return fie;
            }


            return fname;
            
        }
    }
}
