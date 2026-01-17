using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    public class DbBulkFieldMap
    {
        public BulkMapType type;
        public int srcIndex=-1;
        public int tarIndex=-1;

        public string srcName;
        public string tarName;
    }

    public enum BulkMapType
    {

        none=0,
        index=1,
        name=2
    }
}
