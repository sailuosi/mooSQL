

namespace mooSQL.excel.context
{
    /// <summary>
    /// 生命周期回调的处理。即事件。
    /// </summary>
    public class callbackInfo
    {
        public callbackInfo(string bpo, string method)
        {
            this.BPOName = bpo;
            this.Method = method;
        }
        public string BPOName;
        public string Method;
    }
}
