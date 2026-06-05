using System;

namespace mooSQL.data
{
    public partial class SQLBuilder
    {
        /// <summary>
        /// 将当前构建状态捕获为可复用碎片（API 步骤脚本）。
        /// </summary>
        public SQLApart toApart()
        {
            var script = ApartEmitter.Emit(this);
            var dbType = ResolveDbType();
            return new SQLApart(script, dbType);
        }

        /// <summary>
        /// 合并复用碎片：按录制顺序在目标 Builder 上重放 select/from/where 等公开 API。
        /// 一阶段仅允许与捕获时相同 <see cref="DataBaseType"/> 的数据库。
        /// </summary>
        public SQLBuilder useApart(SQLApart apart)
        {
            if (apart == null)
                throw new ArgumentNullException(nameof(apart));
            EnsureApartCompatible(apart);
            apart.Script.ApplyTo(this);
            return this;
        }

        internal SqlCTE ApartGetCte() => CTECollection;

        private void EnsureApartCompatible(SQLApart apart)
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
