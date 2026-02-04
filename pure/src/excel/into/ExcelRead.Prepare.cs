// 基础功能说明：

using mooSQL.excel.context;
using mooSQL.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace mooSQL.excel
{
    public abstract partial class ExcelRead
    {
        /// <summary>
        /// 在Excel的datatable行循环开始前，执行一些准备工作。
        /// </summary>
        public void workBeforeReadRows()
        {
            if (context.option.onBeforeReadTable != null)
            {
                var re = context.option.onBeforeReadTable(this);
            }
            this.setProgress("正在与系统环境数据通讯，导入即将开始...");
            //检查输出标识列
            if (context.valueCollection.contain(context.option.outInfoCol) == false)
            {
                throw new Exception("行输出标识列" + context.option.outInfoCol + "设置错误，不存在该列！请检查outInfoCol定义！ ");
            }
            //准备表的数据
            if (context.option.checkMode == "local")
            {
                foreach (var kv in baseTable)
                {
                    string tbKey = kv.Key;
                    var tb = kv.Value;

                    if (tb.Empty)
                    {

                        //检查备查数据范围是否妥当。
                        var checkScope = tb.whereInFields;
                        if (checkScope.Count > 0)
                        {
                            var csRes = getCheckWhereIns(tb);
                            if (csRes != "" && tb.checkWhere != "")
                            {
                                tb.checkWhere += " and " + csRes;
                            }
                        }
                        //处理where条件，解析格式化模板串。
                        var wherePart = tb.checkWhere;
                        int err;
                        wherePart = formatSqlKey(tb.checkWhere, out err);
                        tb.checkWhere = wherePart;
                        tb.readData();
                    }
                }
            }
            //检查是否使用BulkCopy，需要进行列信息准备
            prepareDynamic();
            //检查是否需要补齐写入列的列类型
            //格式化所有查询列的查询条件字符串
            foreach (var tb in Writelist)
            {
                var cos = tb.Value.oldData.table.Columns;
                foreach (var col in tb.Value.writeCols)
                {
                    var n = col.Value.field;
                    if (cos.Contains(n))
                    {
                        col.Value.dataType = cos[n].DataType;
                    }
                }
            }
            //readyBulk(wkinfo);
            if (context.option.onAfterReadTable != null)
            {
                var re = context.option.onAfterReadTable(this);
            }
            //添加环境列：excelRowNum dataRowNum;
            addInnerFixCol("excelRowNum");
            addInnerFixCol("dataRowNum");
            //创建日志表的bulk写入环境
            //this.readyLogBulk(wkinfo.loginfo,cmd,conn);
            //导入数据库前，写入当期导入日志,
            //该调用造成日志重复，移除20201206
            //writelog( info,"pretip");
        }
        /// <summary>
        /// 添加内部用的环境固定值列
        /// </summary>
        /// <param name="key"></param>
        private void addInnerFixCol(string key)
        {
            if (context.valueCollection.contain(key)) return;
            var ern = new colInfo(key, this);
            ern.root = this;
            ern.type = columnType.fix;
            ern.dynamic = false;
            ern.ID = key;
            context.valueCollection.addCol(key, ern);
        }
        /// <summary>
        /// 获取表的范围查询数据集。
        /// </summary>
        /// <param name="tb"></param>
        /// <returns></returns>
        public string getCheckWhereIns(checkTable tb)
        {
            var res = "";
            foreach (var kv in tb.whereInFields)
            {
                var dcol = tb.allCols[kv.Key];
                var colStr = new StringBuilder();
                var bag = kv.Value;
                var tarValues = new List<string>();
                foreach (var key in bag.srcFields)
                {
                    if (context.valueCollection.contain(key) && excelCheckColnames.Contains(key))
                    {
                        var col = context.valueCollection.getCol(key);
                        var index = col.ExcelIndex;

                        if (excelCheckColIndex.Contains(index) && excelCheckColData.ContainsKey(index))
                        {
                            var values = excelCheckColData[index];
                            foreach (var val in values)
                            {
                                string ctval = context.valueCollection.ColValueToCode(col, val);
                                if (tarValues.Contains(ctval) == false) tarValues.Add(ctval);
                            }
                        }
                        else if (col.uniValues.Count > 0)
                        {
                            foreach (var val in col.uniValues)
                            {
                                string ctval = context.valueCollection.ColValueToCode(col, val);
                                if (tarValues.Contains(ctval) == false) tarValues.Add(ctval);
                            }
                        }
                    }
                }

                foreach (var val in tarValues)
                {
                    //逻辑更改

                    bag.addStrValue(val);
                    /*
                    Object outres;
                    //先翻译代码值，后封装格式，caption传入空以禁用提示。
                    var msg = "";
                    var okval = val;
                    if (dcol != null)
                    {
                        var okv = shapeDataType(val, dcol.DataType);
                        if (okv != null)
                        {
                            okval = okv.ToString();
                        }
                    }
                    if (okval != null)
                    {
                        if (colStr.Length > 0)
                        {
                            colStr.Append(",");
                        }
                        colStr.Append("'" + okval + "'");
                    }*/
                }


                if (colStr.Length > 0)
                {
                    if (res != "") { res += " and "; }
                    bag.field = kv.Key;
                    res += bag.toPlainSQL();
                }
            }
            res = myUntils.SqlFilter(res, true);
            return res;
        }
        /// <summary>
        /// 在动态列执行数据体循环前，准备其信息。
        /// </summary>
        public void prepareDynamic()
        {
            //主要工作：生成对应的临时标题焦点列
            foreach (var cross in dynamicCols)
            {
                if (context.valueCollection.contain(cross))
                {
                    var col = context.valueCollection.getCol(cross);
                    addFocusCol(col);
                }
            }
        }
        public void addFocusCol(colInfo dynamicCol)
        {
            var scopeCodes = dynamicCol.dynamicExcelCols;
            excelTitleRow.ForEachClosed((ri) => {
                var key = "Head" + ri.ToString();
                if (isValid(dynamicCol.key))
                {
                    key = dynamicCol.key + key;
                }
                else
                {
                    key = dynamicCol.field + key;
                }
                if (context.valueCollection.contain(key) == false)
                {

                    var focusHead = new colInfo(key, this);
                    focusHead.root = this;
                    focusHead.type = columnType.focusHead;
                    focusHead.dynamic = true;
                    focusHead.rowIndex = ri;
                    focusHead.value = ri.ToString();
                    focusHead.ID = key;
                    focusHead.bossID = dynamicCol.ID;
                    context.valueCollection.addCol(key, focusHead);
                    dynamicCol.slaveHeads.AddNotRepeat( key);
                }
            });

            //创建焦点列的信息
            //使用动态列的key+Index/Code 来命名
            var indexCol = dynamicCol.key + "Index";
            if (context.valueCollection.contain(indexCol) == false)
            {

                var focusHead = new colInfo(indexCol, this);
                focusHead.root = this;
                focusHead.type = columnType.focusHead;
                focusHead.dynamic = true;
                focusHead.ID = indexCol;
                focusHead.bossID = dynamicCol.ID;
                context.valueCollection.addCol(indexCol, focusHead);
            }
            var codeCol = dynamicCol.key + "Code";
            if (context.valueCollection.contain(codeCol) == false)
            {

                var focusHead = new colInfo(codeCol, this);
                focusHead.root = this;
                focusHead.type = columnType.focusHead;
                focusHead.dynamic = true;
                focusHead.ID = codeCol;
                focusHead.bossID = dynamicCol.ID;
                context.valueCollection.addCol(codeCol, focusHead);
            }
        }






        /// <summary>
        /// 重设置标题行范围。
        /// </summary>
        /// <param name="configStr"></param>
        public void resetTitleScope(string configStr)
        {
            context.option.titleRowNum = configStr;
            excelTitleRow.readConfig(configStr);
        }
        /// <summary>
        /// 重设数据体行的范围
        /// </summary>
        /// <param name="configStr"></param>
        public void resetDataScope(string configStr)
        {
            context.option.dataRowNum = configStr;
            excelDataRow.readConfig(configStr);
        }

        private Table getDefaultTableConfig()
        {
            var tb = new Table();
            tb.position = 0;
            return tb;
        }

        private void addCheckCol(string tbname, string colname)
        {
            if (baseTable.ContainsKey(tbname) == false)
            {//添加字段名到查询表的列集合。
                var tb = new checkTable(tbname);
                tb.option = getDefaultTableConfig();
                tb.root = this;
                baseTable.Add(tbname, tb);
            }

            baseTable[tbname].addCheckCol(colname);
        }
    }
}
