using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.excel
{
    /// <summary>
    /// excel文件顶级代表对象
    /// </summary>
    public class XWorkBook
    {
        /*
         本套类以X开头的原因，是因为本类的组织方式是按照xml的组织方式构建。
         */

        /// <summary>磁盘文件名（若已知）。</summary>
        public string fileName;

        /// <summary>文件类型或扩展名标识。</summary>
        public string fileType;

        /// <summary>工作表序号到工作表对象的映射。</summary>
        public Dictionary<int,XSheet> sheets;

        /// <summary>构造空工作簿（无工作表内容）。</summary>
        public XWorkBook() { 
            sheets = new Dictionary<int,XSheet>();
        }

        /// <summary>
        /// 获取sheet
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public XSheet getSheet(int index) {

            if(sheets !=null && sheets.ContainsKey(index)) return sheets[index];
            return null;
        }

        /// <summary>获取或创建指定索引的工作表。</summary>
        public XSheet getOrNew(int sheetIndex) {
            if (sheets.ContainsKey(sheetIndex) == false)
            {
                sheets.Add(sheetIndex, new XSheet());
            }
            var xsheet = sheets[sheetIndex];
            if (xsheet == null)
            {
                sheets[sheetIndex] = new XSheet();
                xsheet = sheets[sheetIndex];
            }
            return xsheet;
        }
    }
}
