namespace mooSQL.data
{
    /// <summary>
    /// 热路径 CollectBind：产出本次请求的 <see cref="IDelayPara"/>（未 BindPlaceHolder）。
    /// 返回 null 表示本步不登记 Live（与 Apply 跳过对齐）。
    /// </summary>
    public interface ILiveBindStep
    {
        IDelayPara CollectLive(SQLBuilder builder);
    }
}
