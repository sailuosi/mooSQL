using System;


namespace mooSQL.excel.context
{
    /// <summary>
    /// 描述嵌入在 Excel 中的一张图片及其在工作表上的锚定行列范围。
    /// </summary>
    public class PicturesInfo
    {
        /// <summary>锚区最小行号（从 0 或库约定起始，与同模块其它行列索引一致）。</summary>
        public int MinRow { get; set; }
        /// <summary>锚区最大行号。</summary>
        public int MaxRow { get; set; }
        /// <summary>锚区最小列号。</summary>
        public int MinCol { get; set; }
        /// <summary>锚区最大列号。</summary>
        public int MaxCol { get; set; }
        /// <summary>图片二进制内容。</summary>
        public Byte[] PictureData { get; private set; }

        /// <summary>
        /// 使用行列边界与图片字节构造图片信息。
        /// </summary>
        /// <param name="minRow">最小行。</param>
        /// <param name="maxRow">最大行。</param>
        /// <param name="minCol">最小列。</param>
        /// <param name="maxCol">最大列。</param>
        /// <param name="pictureData">图片数据。</param>
        public PicturesInfo(int minRow, int maxRow, int minCol, int maxCol, Byte[] pictureData)
        {
            this.MinRow = minRow;
            this.MaxRow = maxRow;
            this.MinCol = minCol;
            this.MaxCol = maxCol;
            this.PictureData = pictureData;
        }
    }
}
