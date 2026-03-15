using mooSQL.data;
using mooSQL.linq.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq
{
    public class ProviderContext
    {
        public Type entityType;
        public DBInstance DbContext { get; set; } = null!;
        internal object?[]? Preambles;

        internal object?[]? Parameters;

        internal SentenceBag? Info;
    }
}
