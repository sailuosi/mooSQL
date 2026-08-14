using mooSQL.excel.context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.excel
{
    /// <summary>
    /// 读取范围配置
    /// </summary>
    public class ReadScopeConfig
    {
        /// <summary>
        /// 是否读取全部
        /// </summary>
        public bool readAll = false;

        /// <summary>
        /// [A1-I99]
        /// </summary>
        public string configString = "";
        /// <summary>
        /// 工作表范围配置
        /// </summary>
        public Dictionary<int, SheetScopes> sheetScopes;

        /// <summary>
        /// 检查是否需要读取某个sheet
        /// </summary>
        /// <param name="sheetIndex"></param>
        /// <returns></returns>
        public bool containSheet(int sheetIndex) {
            if (this.readAll) { 
                return  true;
            }
            if (sheetScopes.ContainsKey(sheetIndex)) { 
                return true;
            }
            return false;
        }

        /// <summary>
        /// 获取读取配置，如果未配置，则读取全部
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public SheetScopes getSheetScope(int id) {
            if (sheetScopes == null) { 
                sheetScopes = new Dictionary<int, SheetScopes>();
            }
            if (sheetScopes.ContainsKey(id)) { 
                return sheetScopes[id];
            }

            var defat = new SheetScopes();
            defat.readAll = true;
            sheetScopes.Add(id, defat);
            return defat;
        
        }
        /// <summary>
        /// 读取列配置
        /// </summary>
        /// <param name="sheetIndex"></param>
        /// <param name="configString">格式为A-E,D-G</param>
        public void ReadColScope(int sheetIndex,string configString) {
            if (sheetScopes == null)
            {
                sheetScopes = new Dictionary<int, SheetScopes>();
            }
            if (!sheetScopes.ContainsKey(sheetIndex)) { 
                sheetScopes[sheetIndex] = new SheetScopes();
            }


            sheetScopes[sheetIndex].readAZConfig(configString);
        }
        /// <summary>
        /// 读取行范围配置
        /// </summary>
        /// <param name="sheetIndex"></param>
        /// <param name="configString"></param>
        public void ReadRowScope(int sheetIndex, string configString)
        {
            if (sheetScopes == null)
            {
                sheetScopes = new Dictionary<int, SheetScopes>();
            }
            if (!sheetScopes.ContainsKey(sheetIndex))
            {
                sheetScopes[sheetIndex] = new SheetScopes();
            }


            sheetScopes[sheetIndex].readRowConfig(configString);
        }

    }
    /// <summary>
    /// 表范围配置
    /// </summary>
    public class SheetScopes {
        /// <summary>
        /// 是否读取全部
        /// </summary>
        public bool readAll = false;
        /// <summary>
        /// 范围集合
        /// </summary>
        public List<CellScope> scopes = new List<CellScope>();
        /// <summary>
        /// 列范围循环
        /// </summary>
        /// <param name="onRead"></param>
        public void ForEachAZ(Action<int> onRead) {
            foreach (CellScope scope in scopes) { 
                scope.colScope.ForEachClosed(onRead);
            }
        }

        /// <summary>
        /// 读取列范围配置
        /// </summary>
        /// <param name="config"></param>
        public void readAZConfig(string config) {
            if (scopes.Count == 0) { 
                scopes.Add(new CellScope());
            }

            var so = scopes[0];
            so.colScope.readConfig(config);
        }
        /// <summary>
        /// 读取行
        /// </summary>
        /// <param name="config"></param>
        public void readRowConfig(string config)
        {
            if (scopes.Count == 0)
            {
                scopes.Add(new CellScope());
            }

            var so = scopes[0];
            so.rowScope.readConfig(config);

        }
        /// <summary>
        /// 是否包含某行
        /// </summary>
        /// <param name="rowIndex"></param>
        /// <returns></returns>
        public bool containsRow(int rowIndex) {

            if (this.readAll) { 
                return true;
            }
            foreach (CellScope scope in scopes) {
                if (scope.rowScope.Contain(rowIndex)) { 
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// 是否包含某列
        /// </summary>
        /// <param name="colIndex"></param>
        /// <returns></returns>
        public bool containsCol(int colIndex)
        {

            if (this.readAll)
            {
                return true;
            }
            foreach (CellScope scope in scopes)
            {
                if (scope.colScope.Contain(colIndex))
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// 获取最大列索引
        /// </summary>
        /// <returns></returns>
        public int getMaxAZ() {
            var res = 0;
            foreach (CellScope scope in scopes) {
                var t = scope.colScope.Max();
                if (t!=null&& t > res) {
                    res = t.Value;
                }
            }
            return res;
        }
    }
    /// <summary>
    /// 单元格范围
    /// </summary>
    public class CellScope {
        /// <summary>
        /// 单元格范围
        /// </summary>
        public CellScope() { 
            this.rowScope = new IntSection();
            this.colScope = new AZSection();
        }
        /// <summary>
        /// 需要读取的行范围
        /// </summary>
        public IntSection rowScope;
        /// <summary>
        /// 需要读取的列范围
        /// </summary>
        public AZSection colScope;        
    }
}
