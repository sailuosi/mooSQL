namespace mooSQL.data
{
    /// <summary>
    /// 将已解析的 <see cref="Parameter"/> 写入内核 <c>ps</c>。
    /// 供 Clause 转译在 Visit 期登记参数：必须入队，否则 <see cref="SQLBuilder.runBuild"/> 的 clear 会冲掉直接 <c>ps.Add</c>。
    /// </summary>
    public sealed class AddParaStep : StepBase
    {
        public override int Id { get { return 458790; } }
        public override StepKind Kind { get { return StepKind.Other; } }

        private readonly Parameter _para;

        public AddParaStep(Parameter para)
        {
            _para = para ?? throw new System.ArgumentNullException(nameof(para));
        }

        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            hc.Add(Id);
            hc.Add(_para.key ?? "");
            hc.Add(_para.raw ? 1 : 0);
        }

        public override void Apply(SQLBuilder builder)
        {
            // 每次 Apply 拷一份，避免多轮 runBuild 共享可变 Parameter
            var copy = new Parameter(_para.key, _para.val, _para.varPrefix)
            {
                dbType = _para.dbType,
                raw = _para.raw,
                rawKey = _para.rawKey,
                rawHolder = _para.rawHolder,
                holder = _para.holder
            };
            builder.Inner.ps.Add(copy);
        }
    }
}
