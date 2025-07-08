using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mooSQL.data;

namespace mooSQL.linq
{
    internal class EntityOrigin:OriginTable
    {

        public EntityInfo EntityInfo { get; set; }

        public List<EntityColumn> UsedColumns { get; set; }

        public override string build(DBInstance DB, LayerRunType type)
        {
            var tbname = this.EntityInfo.DbTableName;
            if (type== LayerRunType.select && !string.IsNullOrWhiteSpace(NickName)) { 
                return tbname+" as "+NickName;
            }
            return tbname;
        }

        public string nickName;

        public string SQL;
    }
}
