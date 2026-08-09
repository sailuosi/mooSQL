namespace mooSQL.data
{
    /// <summary>延迟参数 PlaceHolder 格式常量。</summary>
    public static class LiveParaMarks
    {
        /// <summary>生成 @@{{moo.lp:{index}}}。</summary>
        public static string Format(int delayParaIndex)
        {
            return "@@{{moo.lp:" + delayParaIndex + "}}";
        }
    }
}
