using System.Collections.Generic;
using System.Threading.Tasks;

namespace mooSQL.data
{
    public partial class PrepareSQLBuilder
    {
        /// <inheritdoc />
        public override SQLCmd toUpdateUnion(IReadOnlyList<string> keyColumns)
        {
            runBuild();
            return _inner.toUpdateUnion(keyColumns);
        }

        /// <inheritdoc />
        public override int doUpdateUnion(IReadOnlyList<string> keyColumns)
        {
            runBuild();
            return _inner.doUpdateUnion(keyColumns);
        }

        /// <inheritdoc />
        public override Task<int> doUpdateUnionAsync(IReadOnlyList<string> keyColumns)
        {
            runBuild();
            return _inner.doUpdateUnionAsync(keyColumns);
        }
    }
}
