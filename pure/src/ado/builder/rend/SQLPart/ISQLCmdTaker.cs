

namespace mooSQL.data
{
    /// <summary>
    /// 接口 ISQLCmdTaker。
    /// </summary>
    public interface ISQLCmdTaker
    {
        /// <summary>
        /// 内部成员说明。
        /// </summary>
        ISQLCmdTaker TakeOver(SQLCmd cmd);
    }
}