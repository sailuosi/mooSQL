

namespace mooSQL.excel.context
{
    /// <summary>
    /// 生命周期回调的处理。即事件。
    /// </summary>
    public class callbackInfo
    {
        /// <summary>
        /// 使用 BPO 名称与方法名构造一条回调描述。
        /// </summary>
        /// <param name="bpo">业务处理对象（BPO）名称。</param>
        /// <param name="method">要调用的方法名。</param>
        public callbackInfo(string bpo, string method)
        {
            this.BPOName = bpo;
            this.Method = method;
        }
        /// <summary>BPO 名称。</summary>
        public string BPOName;
        /// <summary>回调方法名。</summary>
        public string Method;
    }
}
