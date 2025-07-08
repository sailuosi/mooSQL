using System;
using System.Data;

namespace mooSQL.data
{
    /// <summary>
    /// 数据库特性支持
    /// </summary>
    internal class FeatureSupport
    {
        private static readonly FeatureSupport
            Default = new FeatureSupport(false),
            Postgres = new FeatureSupport(true),
            ClickHouse = new FeatureSupport(true);



        private FeatureSupport(bool arrays)
        {
            Arrays = arrays;
        }

        /// <summary>
        /// True if the db supports array columns e.g. Postgresql
        /// </summary>
        public bool Arrays { get; }
    }
}
