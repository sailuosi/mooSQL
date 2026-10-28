using System.Collections.Generic;
using System.Threading.Tasks;

namespace mooSQL.data
{
    public partial class PrepareSQLBuilder
    {
        /// <inheritdoc />
        public override SQLCmd toUpdateBatch(IReadOnlyList<string> keyColumns)
        {
            runBuild();
            return _inner.toUpdateBatch(keyColumns);
        }

        /// <inheritdoc />
        public override int doUpdateBatch(IReadOnlyList<string> keyColumns)
        {
            runBuild();
            return _inner.doUpdateBatch(keyColumns);
        }

        /// <inheritdoc />
        public override Task<int> doUpdateBatchAsync(IReadOnlyList<string> keyColumns)
        {
            runBuild();
            return _inner.doUpdateBatchAsync(keyColumns);
        }
    }
}
