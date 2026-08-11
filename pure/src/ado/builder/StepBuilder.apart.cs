using System;

namespace mooSQL.data
{
    /// <summary>
    /// Apart 兼容入口：碎片以 SQLBuilder 编排磁带为准；内核侧仅支持 useApart 物化重放。
    /// </summary>
    public partial class StepBuilder
    {
        /// <summary>
        /// 内核侧重放：经 Attach 门面将编排步骤 Apply 到本实例。
        /// </summary>
        public StepBuilder useApart(SQLApart apart)
        {
            if (apart == null)
                throw new ArgumentNullException(nameof(apart));
            EnsureApartCompatible(apart);
            var facade = SQLBuilder.Attach(this, materializing: true);
            facade.useApart(apart);
            return this;
        }

        internal SqlCTE ApartGetCte() => CTECollection;

        internal void EnsureApartCompatible(SQLApart apart)
        {
            var target = ResolveDbType();
            if (apart.SourceDbType != target)
                throw new ApartIncompatibleException(apart.SourceDbType, target);
        }

        private DataBaseType ResolveDbType()
        {
            if (DBLive?.config != null)
                return DBLive.config.dbType;
            return DataBaseType.MSSQL;
        }
    }
}
