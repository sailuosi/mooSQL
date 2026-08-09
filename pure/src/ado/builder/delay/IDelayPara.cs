namespace mooSQL.data
{
    /// <summary>
    /// 可运行参数转换体：登记进 <see cref="Paras.DelayParas"/>，
    /// 在 <see cref="Paras.ResolveDelayParas"/> 时 <see cref="Run"/> 产出最终 SQL 片段。
    /// </summary>
    public interface IDelayPara
    {
        /// <summary>由 <see cref="Paras.AddDelayPara"/> 按集合序号固化的占位 SQL。</summary>
        string PlaceHolder { get; }

        /// <summary>登记时调用：PlaceHolder = @@{{moo.lp:{index}}}。</summary>
        void BindPlaceHolder(int delayParaIndex);

        /// <summary>
        /// 绑定所属 <see cref="Paras"/>（Copy 后须重绑到新实例）。
        /// <see cref="DelayWhereFormat"/> 等在 <see cref="Run"/> 中写 KV 时使用。
        /// </summary>
        void BindOwner(Paras owner);

        /// <summary>解析：产出替换 PlaceHolder 的最终文本；必要时写 Owner KV。</summary>
        string Run();
    }
}
