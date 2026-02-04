// 基础功能说明：

using mooSQL.excel.context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.excel
{
    /// <summary>
    /// 读取期间的上下文
    /// </summary>
    public class ReadingContext
    {
        // 前置成员，不可为null
        /// <summary>
        /// 导入选项配置信息。
        /// </summary>
        public ImportOption option;
        /// <summary>
        /// 准备好的值集合。
        /// </summary>
        public ReadyValueCollection valueCollection;
        /// <summary>
        /// 日志输出。
        /// </summary>
        public MsgOutput logger;



        //工作期间的成员
        /// <summary>
        /// 正在执行处理的excel数据dataTable的行记录。
        /// </summary>
        public rowInfo readingRow = new rowInfo();

        public int readingExcelRowIndex;//正在处理的Excel所在行

        public int[] writelog = new int[4] { 0, 0, 0, 0 };


        public string RowMark
        {
            get
            {
                return readingRow.rowMark;
            }
        }

        public Dictionary<string, colInfo> colMap
        {
            get
            {
                return valueCollection.colMap;
            }
        }
        /// <summary>
        /// 输出标识列
        /// </summary>
        public colInfo OutputCol
        {
            get
            {
                return valueCollection.getCol(option.outInfoCol);
            }
        }



    }
}