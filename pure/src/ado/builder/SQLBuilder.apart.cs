using System;

namespace mooSQL.data
{
    public partial class SQLBuilder
    {
        /// <summary>
        /// 开启录播：返回独立影子 Builder，链式调用仅写入该影子，不污染当前实例；
        /// 以 <see cref="stop"/> 结束并得到 <see cref="SQLApart"/>，再通过 <see cref="useApart"/> 复用。
        /// </summary>
        /// <example>
        /// var seg = kit.record().where("status", 1).stop();
        /// kit.select("*").from("users").useApart(seg).toSelect();
        /// </example>
        public SQLBuilder record()
        {
            this.current.wherePart.steps.start();
            return this;
        }

        /// <summary>
        /// 结束 <see cref="record"/> 录播链，将期间步骤捕获为 <see cref="SQLApart"/>。
        /// </summary>
        public SQLApart stop()
        {
            this.current.wherePart.steps.stop();
            return toApart();
        }

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
