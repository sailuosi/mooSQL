#if NET451 || NET462

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    internal class OracleBulkCopyee : DbBulkCopyFallback
    {
        public OracleBulkCopyee(DBInstance DB) : base(DB)
        {
        }
    }
}

#endif