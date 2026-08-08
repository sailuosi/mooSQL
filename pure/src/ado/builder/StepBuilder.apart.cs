using System;

namespace mooSQL.data
{
    public partial class StepBuilder
    {
        /// <summary>
        /// 开启录播：返回独立影子 Builder，链式调用仅写入该影子，不污染当前实例；
        /// 以 <see cref="stop"/> 结束并得到 <see cref="SQLApart"/>，再通过门面 <c>useApart</c> 复用。
        /// </summary>
        public StepBuilder record()
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
        /// 内核侧重放：经 Attach 门面调用公开 API（供非门面路径）。
        /// </summary>
        public StepBuilder useApart(SQLApart apart)
        {
            if (apart == null)
                throw new ArgumentNullException(nameof(apart));
            EnsureApartCompatible(apart);
            var facade = SQLBuilder.Attach(this, materializing: true);
            apart.Script.ApplyTo(facade);
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
