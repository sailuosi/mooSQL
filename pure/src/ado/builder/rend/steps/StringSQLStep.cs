namespace mooSQL.data
{
    /// <summary>
    /// 仅接收单个 SQL/片段 string 的步骤基类。
    /// Kind 由子类声明；ContributeHash 用钩子覆盖门控 / 是否 Combine Sql / 是否产出。
    /// </summary>
    public abstract class StringSQLStep : StepBase
    {
        protected readonly string Sql;

        protected StringSQLStep(string sql)
        {
            Sql = sql;
        }

        /// <summary>是否受 ifs Opened 门控。默认 false。</summary>
        protected virtual bool GateByOpened { get { return false; } }

        /// <summary>Hash 是否 Combine Sql。WhereExist 为 false。</summary>
        protected virtual bool HashSql { get { return true; } }

        /// <summary>已通过门控（或不门控）时是否产出 SQL。默认 true；WhereControl 固定 false。</summary>
        protected virtual bool ResolveEmit(string paraRule) { return true; }

        public sealed override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            if (GateByOpened && !ConsumeOpened(ref opened))
            {
                hc.Add(Id);
                hc.Add(0);
                if (HashSql)
                    hc.Add(Sql);
                return;
            }

            hc.Add(Id);
            hc.Add(ResolveEmit(paraRule) ? 1 : 0);
            if (HashSql)
                hc.Add(Sql);
        }
    }
}
