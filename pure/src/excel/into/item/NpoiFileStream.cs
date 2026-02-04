
using System.IO;


namespace mooSQL.excel.context
{
    /// <summary>
    /// 导入读取流
    /// </summary>
    public class NpoiFileStream : FileStream
    {
        /// <summary>
        /// 导入读取流
        /// </summary>
        /// <param name="path"></param>
        /// <param name="mode"></param>
        /// <param name="access"></param>
        public NpoiFileStream(string path, FileMode mode, FileAccess access) : base(path, mode, access)
        {
            AllowClose = true;

        }
        /// <summary>
        /// 是否可关闭
        /// </summary>
        public bool AllowClose { get; set; }
        /// <summary>
        /// 关闭流
        /// </summary>
        public override void Close()
        {
            if (AllowClose)
                base.Close();
        }
    }
}
