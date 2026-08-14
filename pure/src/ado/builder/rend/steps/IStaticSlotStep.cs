namespace mooSQL.data
{
    /// <summary>
    /// 编排期静态槽位步：热路径按 <see cref="StaticSlotId"/> 收值重绑。
    /// </summary>
    public interface IStaticSlotStep
    {
        /// <summary>编排期分配的槽；null 表示本步不写静态参。</summary>
        int? StaticSlotId { get; }

        /// <summary>
        /// 编排期烘焙的物理参数名（含 paraSeed / group）；有 <see cref="StaticSlotId"/> 时非空。
        /// </summary>
        string StaticSlotName { get; }

        /// <summary>本次请求的逻辑值（不进缓存）。</summary>
        object StaticSlotValue { get; }
    }
}
