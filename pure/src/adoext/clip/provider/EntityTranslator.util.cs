using mooSQL.data.linq;
using mooSQL.linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    public partial class EntityTranslator
    {

        /// <summary>
        /// 查找字段名
        /// </summary>
        /// <param name="DB"></param>
        /// <param name="keySelector"></param>
        /// <returns></returns>
        public static FastBusField FindFieldName(DBInstance DB,Expression keySelector) {
            var cont = new FastCompileContext();
            var kit = DB.useSQL();
            cont.initByBuilder(kit);
            var fiedv = new FieldVisitor(cont, false);
            var fid = fiedv.FindField(keySelector);
            if (fiedv.ParsedFields.Count == 1) { 
                var tf=fiedv.ParsedFields[0];
                if (!string.IsNullOrWhiteSpace(tf.CallerNick)) { 
                    var str= tf.CallerNick+tf.ColumnName;
                    return new FastBusField() { 
                        Column=tf.Column,
                        CallerNick=tf.CallerNick,
                    };
                }
            }
            return null;
        }
    }
}
