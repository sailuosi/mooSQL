namespace mooSQL.data
{
    /// <summary>
    /// Format 类 ParaMold.Value：模板 + 实参。
    /// </summary>
    public sealed class MoldFormatValue
    {
        /// <summary>string.Format 风格模板（含 {0}…）。</summary>
        public string Template { get; set; }

        /// <summary>实参（可含 null）。</summary>
        public object[] Args { get; set; }

        /// <summary>select / from / join / where。</summary>
        public string Kind { get; set; }

        /// <summary>
        /// 创建 Format 值袋。
        /// </summary>
        public MoldFormatValue(string kind, string template, object[] args)
        {
            Kind = kind ?? "fmt";
            Template = template ?? "";
            Args = args ?? new object[0];
        }
    }
}
