using mooSQL.data.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    public class EntityOrder
    {
        public int Idx { get; set; }
        public string Nick { get; set; }

        public string Field { get; set; }

        public OrderType OType { get; set; }
    }
}
