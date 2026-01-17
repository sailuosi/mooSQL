using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    public class SooOption
    {


        #region LINQ部分
        public bool PreloadGroups = false;

        public bool IgnoreEmptyUpdate = false;

        public bool GenerateExpressionTest = false;

        public bool TraceMapperExpression = false;

        public bool DoNotClearOrderBys = false;

        public bool OptimizeJoins = true;

        public bool CompareNullsAsValues = true;

        public bool GuardGrouping = true;

        public bool DisableQueryCache = false;
        public TimeSpan? CacheSlidingExpiration = default;
        public bool PreferApply = true;

        public bool KeepDistinctOrdered = true;

        public bool ParameterizeTakeSkip = true;

        public bool EnableContextSchemaEdit = false;

        public bool PreferExistsForScalar = default;
        #endregion


        public SQLProviderFlags ProviderFlags;
    }
}
