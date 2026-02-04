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

        public string fileName;

        public string fileType;

        public Dictionary<int,XSheet> sheets;

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
