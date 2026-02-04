// 基础功能说明：

using mooSQL.excel.context;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace mooSQL.excel
{
    /// <summary>
    /// 备好的可用来写入的列值集合。
    /// </summary>
    public class ReadyValueCollection : BaseUtil
    {
        /// <summary>
        /// 处理上下文
        /// </summary>
        public ReadingContext context;

        /// <summary>
        /// 列信息集合，包含所有的列。
        /// </summary>
        public Dictionary<string, colInfo> colMap = new Dictionary<string, colInfo>();
        /// <summary>
        /// 代码表信息数据
        /// </summary>
        public Dictionary<string, Dictionary<string, string>> codeTableMap = new Dictionary<string, Dictionary<string, string>>();


        public bool check()
        {
            foreach (var kv in colMap)
            {
                var col = kv.Value;
                if (col.type == columnType.match && (col.ExcelIndex == -1 || isValid(col.excelCode) == false))
                {
                    if (col.isNeed)
                    {
                        context.logger.logFatal("必填项：列<span class='colcap'>“" + col.caption + "”</span>在Excel中未找到值！请补全数据后重新导入！<br/>");
                        return false;
                    }
                    if (kv.Value.defaultValue != null)
                    {
                        context.logger.logImportant("列<span class='colcap'>“" + col.caption + "”</span>在Excel中未找到值，将自动使用默认值！<br/>");
                    }
                    else
                    {
                        context.logger.logImportant("列<span class='colcap'>“" + col.caption + "”</span>在Excel中未找到值！<br/>");
                    }
                }
            }
            return true;

        }

        /// <summary>
        /// 添加一个列，自动判空判重
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void addCol(string key, colInfo value)
        {
            colMap.AddNotNull(key, value);
        }

        public bool contain(string key)
        {
            return colMap.ContainsKey(key);
        }
        public colInfo getCol(string key)
        {
            if (colMap.ContainsKey(key)) return colMap[key];
            return null;
        }
        public string getColCaptionByName(string name)
        {
            if (colMap.ContainsKey(name))
            {
                return colMap[name].caption;
            }
            else
            {
                return name;
            }
        }

        /// <summary>
        /// 对一行Excel数据的dataRow进行列集合取值。
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public bool loadRowData(DataRow row)
        {
            /*先清除所有的赋值信息*/
            foreach (var kv in colMap)
            {
                var col = kv.Value;
                if (col.state == "done" || col.type == columnType.fix) continue;
                col.writeValue = null;
                col.parsedValue = null;
            }
            //第一轮遍历获取，出现依赖项时，把依赖项先行取值 。只有查询、计算列会出现。
            foreach (var kv in colMap)
            {
                var col = kv.Value;
                //当列是写入字段，且写入表定义了写入行范围时，如果此时不在行范围内，不读取
                if (col.table != null && col.table.customScope && col.table.rowScope.Contain(context.readingExcelRowIndex) == false) continue;
                //固定列、主键列、其它已固定值的列，不读取。
                if (col.state == "done" || col.type == columnType.fix || col.isPrimaryKey) continue;

                loadColData(row, col);

                if (!isValid(col.writeValue) && col.isNeed && col.type != columnType.dynamic && !col.dynamic)
                {
                    //只有列的所属表要求行撤销写入时，才执行。
                    if (col.table == null || col.table.option == null || col.table.option.failPolicy != checkFailAct.row) { continue; }
                    if (col.showTip) { context.logger.logError(context.RowMark + "必填项" + col.caption + "为空，不处理<br/>"); }
                    context.writelog[3]++;
                    return false;
                }

            }
            return true;
        }
        /// <summary>
        /// 在动态列的列循环中，加载需要读取数据的列值。
        /// </summary>
        /// <param name="colname"></param>
        /// <param name="row"></param>
        /// <param name="dcol"></param>
        /// <returns></returns>
        public bool loadDynamicColData(string colname, DataRow row, colInfo dcol)
        {
            var coreValue = row[colname];
            dcol.focusColCode = colname;
            dcol.writeValue = coreValue.ToString();
            dcol.parsedValue = coreValue;
            //分2次循环，基础前置的标题列先行取值，基于其的查询列再取值。
            foreach (var slave in dcol.slaveHeads)
            {
                if (colMap.ContainsKey(slave))
                {
                    var col = colMap[slave];
                    if (col.type == columnType.focusHead && col.bossID == dcol.ID)
                    {   //列的键=  excel列编号+行号
                        var tarkey = colname + col.rowIndex;
                        if (colMap.ContainsKey(tarkey))
                        {
                            col.writeValue = getColVal(tarkey);
                        }
                        else
                        {
                            context.logger.logTip(string.Format("列{0}在获取动态列值{1}时，对应的列标题未找到！", col.key, tarkey));
                        }
                    }
                }

            }
            //跟随取值的列值赋值
            var indexKey = dcol.key + "Index";
            var codeKey = dcol.key + "Code";
            if (colMap.ContainsKey(indexKey) && colMap[indexKey].bossID == dcol.ID)
            {
                colMap[indexKey].writeValue = dcol.focusIndex.ToString();
                colMap[indexKey].parsedValue = dcol.focusIndex;
            }
            if (colMap.ContainsKey(codeKey) && colMap[codeKey].bossID == dcol.ID)
            {
                colMap[codeKey].writeValue = dcol.focusColCode;
                colMap[codeKey].parsedValue = dcol.focusColCode;
            }

            foreach (var kv in colMap)
            {
                var col = kv.Value;
                if (col.dynamic && col.type != columnType.focusHead)
                {
                    //执行列的取值
                    if (isValid(col.src))
                    {
                        //特索情况，切割列的格式[0]
                        col.writeValue = getColVal(col.src);
                    }
                    else
                    if (col.type == columnType.select)
                    {
                        getSelCol(col, row);
                    }
                    else if (col.type == columnType.reckon)
                    {
                        reckonValue(col, row);
                    }
                    else if (col.type == columnType.function)
                    {
                        col.getFuncValue();
                    }
                    else
                    {
                        context.logger.logTip(string.Format("动态值列{0}在获取动态列值时，类型信息未定义！", col.key));
                    }
                }
            }
            return true;
        }
        /// <summary>
        /// 加载列的数据
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        public void loadColData(DataRow row, colInfo col)
        {
            if (col.writeLocked) return;
            if (col.option.onLoadData != null)
            {
                var re = col.option.onLoadData(row, col);
                if (re == false) return;
            }
            if (col.state == "done" || col.type == columnType.fix) return;
            //清空之前的赋值结果。
            //col.writeValue = null;
            //col.parsedValue = null;
            if (col.srcCols.Count > 0)
            {
                foreach (var src in col.srcCols)
                {
                    if (colMap.ContainsKey(src) && isValid(colMap[src].writeValue) == false)
                    {
                        //来源列在列集合中，且尚未取值。则先执行取值。
                        loadColData(row, colMap[src]);
                    }
                }
            }

            if (col.type == columnType.match && col.ExcelIndex != -1)
            {
                //改用列名编码进行匹配
                if (row.Table.Columns.Contains(col.excelCode)) col.writeValue = row[col.excelCode].ToString();
            }
            else if (col.type == columnType.select)
            {
                getSelCol(col, row);
            }
            else if (col.type == columnType.reckon)
            {
                reckonValue(col, row);
            }
            else if (col.type == columnType.function)
            {
                col.getFuncValue();
            }
            if (col.writeValue == null)
            {

                var dfval = getColDefaultVal(col);
                if (dfval != null)
                {
                    if (col.showTip) { context.logger.logTip(context.RowMark + "列" + col.caption + "为空，置为默认值。<br/>"); }
                    col.writeValue = dfval;
                }
                else
                {
                    col.writeValue = null;
                }
            }
            if (col.option != null)
            {
                if (col.option.replaceReg != null)
                {
                    Regex.Replace(col.writeValue, col.option.replaceReg, col.option.replaceAs);
                }
                if (col.option.onAfterLoadData != null)
                {
                    col.option.onAfterLoadData(row, col);
                }
            }

        }

        public void loadWriteColValue(DataRow row, colInfo col)
        {
            //写入的列，分为2种，如果是key不为空即引用其他列的列值。否则，查询自己的列值
            if (col.type == columnType.dynamic) return;
            if (isValid(col.src) && colMap.ContainsKey(col.src))
            {
                var srcCol = colMap[col.src];
                col.writeValue = getColVal(col.src);
                if (srcCol.parsedValue != null) col.parsedValue = srcCol.parsedValue;
            }
            else
            {
                loadColData(row, col);
            }
        }

        /// <summary>
        /// 获取列的值，当列设置了切割功能时，支持数组格式形如title[0]
        /// </summary>
        /// <param name="colname"></param>
        /// <returns></returns>
        public string getColVal(string colname)
        {
            var arrReg = new Regex(@"[\[][\d+][\]]");
            if (arrReg.IsMatch(colname))
            {
                var index = arrReg.Match(colname).Value;
                index = index.Replace("[", "");
                index = index.Replace("]", "");
                int ind = int.Parse(index);
                var tarCo = colname.Replace(index, "");
                if (colMap.ContainsKey(tarCo))
                {
                    var tar = colMap[tarCo];
                    if (tar.splitValues.Count > ind)
                    {
                        return tar.splitValues[ind];
                    }
                }
                else
                {
                    return tarCo;
                }
            }
            if (colMap.ContainsKey(colname))
            {
                var col = colMap[colname];
                var res = getColVal(col);
                return res;
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 获取在列集合中的某个列的值
        /// </summary>
        /// <param name="col">列对象</param>
        /// <returns></returns>
        public string getColVal(colInfo col)
        {
            var res = "";
            if (isValid(col.writeValue))
            {
                res = col.writeValue;
            }
            else
            {
                res = getColDefaultVal(col);
            }
            return ColValueToCode(col, res);
        }
        /// <summary>
        /// 获取列的缺省值
        /// </summary>
        /// <param name="col"></param>
        /// <returns></returns>
        public string getColDefaultVal(colInfo col)
        {
            string res = "";
            //优先关联列
            if (isValid(col.defaultCol) && colMap.ContainsKey(col.defaultCol) && isValid(colMap[col.defaultCol].writeValue))
            {
                return colMap[col.defaultCol].writeValue;
            }
            else if (isValid(col.option.defaultValue))
            {
                return col.option.defaultValue;
            }
            else if (isValid(col.option.codeTable) && isValid(col.option.failCode))
            {
                return col.option.failCode;
            }
            return null;
        }
        /// <summary>
        /// 代码值解析和转换
        /// </summary>
        /// <param name="col"></param>
        /// <param name="captionValue"></param>
        /// <returns></returns>
        public string ColValueToCode(colInfo col, string captionValue)
        {
            var res = captionValue;
            if (isValid(col.codeTable))
            {
                res = getCodeValueByCaption(res, col);
            }
            if (col.option.codeMap.Count > 0 && isValid(res))
            {
                res = col.getValueByCodeMap(res);
            }
            return res;
        }
        /// <summary>
        /// 设置列的写入值。
        /// </summary>
        /// <param name="colKey"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool setColValue(string colKey, string value)
        {
            if (colMap.ContainsKey(colKey))
            {
                colMap[colKey].writeValue = value;
                if (colMap[colKey].type == columnType.fix)
                {
                    colMap[colKey].value = value;
                    colMap[colKey].parsedValue = value;
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// 设置列的写入值。
        /// </summary>
        /// <param name="colKey"></param>
        /// <param name="value"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public bool setColValue(string colKey, string value, Object val)
        {
            if (colMap.ContainsKey(colKey))
            {
                colMap[colKey].writeValue = value;
                colMap[colKey].parsedValue = val;
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// 设置内置列的值，为直接设置writeValue
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void setInnerColValue(string key, string value)
        {
            if (colMap.ContainsKey(key) == false) return;
            colMap[key].writeValue = value;
        }
        /// <summary>
        /// 解析缺省值
        /// </summary>
        /// <param name="val"></param>
        /// <param name="errcount"></param>
        /// <returns></returns>
        public string decodeDefaultValue(string val, out int errcount)
        {
            errcount = 0;
            if (val == null)
            {
                errcount++;
                return null;
            }
            string res = "";

            if (val == "newID")
            {
                res = Guid.NewGuid().ToString();
            }
            else if (val.IndexOf('{') != -1)
            {
                res = this.formatFreeSQLValue(val, out errcount);
            }
            else
            {
                res = val;
            }
            return res;
        }
        /// <summary>
        /// 加载计算列的数据
        /// </summary>
        /// <param name="col"></param>
        /// <param name="row"></param>
        /// <returns></returns>
        public string reckonValue(colInfo col, DataRow row)
        {
            var result = "";
            if (!isValid(col.value)) { return ""; }
            string[] oppart = col.value.Split(';');
            if (col.option.reckonType == "string")
            {
                //字符串运算模式下，只支持拼接，即运算符只支持+
                foreach (var x in oppart)
                {
                    if (x == "+")
                    {
                        continue;
                    }
                    string tres;

                    if (colMap.ContainsKey(x))
                    {
                        tres = this.getColVal(x);
                        if (isValid(tres) == false)
                        {
                            loadColData(row, colMap[x]);
                            tres = this.getColVal(colMap[x]);
                        }
                    }
                    else
                    {
                        tres = x;
                    }

                    result += tres;
                }
            }
            else if (col.option.reckonType == "number")
            {   //使用经典的操作符后置格式，如3+5 用 35+表示。
                var os = new Stack<string>();//存储操作数
                var of = new Stack<string>();//存储操作符
                var nums = new Stack<Double>();//存储计算值
                string[] fs = new string[] { "+", "-", "*", "/" };
                foreach (var x in oppart)
                {
                    int index = Array.IndexOf(fs, x);
                    if (index == -1)
                    {
                        //这里要确定是常数还是列名
                        Regex reg = new Regex(@"^[+-]?/d*[.]?/d*$");
                        if (reg.IsMatch(x))
                        {
                            nums.Push(double.Parse(x));
                        }
                        else if (colMap.ContainsKey(x))
                        {
                            string res = this.getColVal(x);
                            if (isValid(res) == false)
                            {
                                loadColData(row, colMap[x]);
                                res = this.getColVal(colMap[x]);
                            }
                            if (isValid(res) == false)
                            {
                                continue;
                            }
                            double n;
                            if (double.TryParse(res, out n))
                            {
                                nums.Push(n);
                            }
                        }
                    }
                    else
                    {
                        //此时是操作符，执行计算，逻辑：退2，写1。
                        Double num1 = nums.Pop();
                        Double num2 = nums.Pop();
                        Double num = 0;
                        switch (x)
                        {
                            case "+":
                                num = num1 + num2;
                                break;
                            case "-":
                                num = num2 - num1;
                                break;
                            case "*":
                                num = num1 * num2;
                                break;
                            case "/":
                                if (num1 == 0) break;
                                num = num2 / num1;
                                break;
                        }
                        if (num != null)
                        {
                            nums.Push(num);
                        }
                    }
                }
                result = nums.Pop().ToString();
            }
            else if (col.option.reckonType == "join")
            {//opt = ExoptA; ExoptB; ExoptC; ExoptD; ExoptE; ExoptF = join=||||
             //在拼接模式下，如果分隔的不是列名，直接拼接，否则获取其值然后拼接。
                foreach (var x in oppart)
                {
                    string tres;
                    if (colMap.ContainsKey(x))
                    {

                        tres = this.getColVal(colMap[x]);
                        if (isValid(tres) == false)
                        {
                            loadColData(row, colMap[x]);
                            tres = this.getColVal(colMap[x]);
                        }
                    }
                    else
                    {
                        tres = x;
                    }
                    if (tres != "" && tres != null)
                    {
                        if (result != "")
                        {
                            result += col.seprator;
                        }
                        result += tres;
                    }
                }
            }
            else if (col.option.reckonType == "split")
            {
                //切割模式下，对值的来源value进行切割处理。当其值是一个key时，取其值，否则直接使用它。
                var tar = col.value;
                if (colMap.ContainsKey(tar))
                {
                    var tarCol = colMap[tar];
                    if (isValid(tarCol.writeValue) == false)
                    {
                        loadColData(row, tarCol);
                    }
                    tar = tarCol.writeValue;
                }
                if (isValid(tar))
                {
                    if (isValid(col.seprator))
                    {
                        var splitRes = Regex.Split(tar, col.seprator);
                        foreach (var s in splitRes)
                        {
                            col.splitValues.Add(s);
                        }
                    }
                    else
                    {//未设置分隔符时，不进行切割。
                        result = tar;
                    }
                }
            }
            col.writeValue = result;
            return result;
        }
        /// <summary>
        /// 加载查询列的数据
        /// </summary>
        /// <param name="selcol"></param>
        /// <param name="row"></param>
        /// <returns></returns>
        private string getSelCol(colInfo selcol, DataRow row)
        {
            var result = "";
            //将已经预处理好的格式进行查询即可。
            if (selcol.state == "done")
            {
                return selcol.writeValue;
            }
            int errCont = 0;
            DataRow[] rows = context.option.workor.getCheckRows(selcol.selectTable, selcol.formatWhere);
            if (rows != null && rows.Length == 1)//刚好一条记录
            {
                var tar = rows[0][selcol.selectCol];
                selcol.writeValue = tar.ToString();
                selcol.parsedValue = tar;
                return rows[0][selcol.selectCol].ToString();
            }
            if (rows != null && rows.Length > 1)//多条重复
            {
                //result = rows[0][strArr[0]].ToString();
                context.logger.logTip(context.RowMark + "列" + selcol.caption + "匹配查询时发现多个重复记录，请检查数据是否正确！<br/>");
            }

            if (isValid(selcol.defaultValue))//当查询列传入了缺省值；
            {
                context.logger.logTip(context.RowMark + "列" + selcol.caption + "查询时未找到数据，已启用默认值！<br/>");
                return this.decodeDefaultValue(selcol.defaultValue, out errCont);
            }

            //if (errCont>0){result = "";}
            return result;
        }
        /// <summary>
        /// 转换代码表的值，支持多选代码表，间隔符为分号逗号
        /// </summary>        
        public string getCodeValueByCaption(string caption, colInfo col)
        {
            string res = "";
            if (col.codeTable == "" || string.IsNullOrWhiteSpace(caption))
            {
                return caption;
            }
            if (caption != "")
            {
                var caps = Regex.Split(caption, @"[;；,，]{1}");
                if (caps.Length != 1)
                {
                    //使用复选值的代码表，需要逐个处理。
                    //string[] cap = caption.Split(';');
                    for (int i = 0; i < caps.Length; i++)
                    {
                        string cv = this.getSimpleCodeValueByCaption(caps[i], col);
                        res += res == "" ? "" : ";";
                        res += cv;
                    }
                }
                else
                {
                    res = this.getSimpleCodeValueByCaption(caption, col);
                }

            }
            return res;
        }
        /// <summary>
        /// 将一个代码名 转换为对应的代码值
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        public string getSimpleCodeValueByCaption(string caption, colInfo col)
        {

            string res = "";
            Boolean seen = false;
            var codetable = codeTableMap[col.codeTable];

            var cap = Regex.Replace(caption, @"[\s]", "");
            if (codetable.ContainsKey(cap))
            {
                res = codetable[cap];
                seen = true;
            }
            //优化当用户直接传入代码值时的处理。
            else if (codetable.ContainsValue(cap))
            {
                res = cap;
                seen = true;
            }
            if (seen == false)
            {
                var fio = context.RowMark + "的列" + col.caption + "格式不规范！";
                if (col.option.failCode != null)
                {
                    res = col.option.failCode;
                    fio += "置为" + (res == "" ? "空" : res);
                }
                else
                {
                    res = caption;
                    fio += "将直接使用该值，这可能导致本列显示异常！<br/>";
                }
                if (col.showTip)
                {
                    context.logger.logTip(fio);
                }
            }

            return res;
        }

        /// <summary>
        ///将自由串中{colname=coltype}格式的占位符替换成对应的值 
        /// </summary>
        public string formatFreeSQLValue(string freeStr, out int errCount)
        {

            const string regs = @"{.*?}";
            errCount = 0;
            string res = freeStr;
            MatchCollection matches = Regex.Matches(freeStr, regs);
            foreach (Match x in matches)
            {
                var tem = x.Value;
                if (tem.Length < 4) continue;
                var tem2 = tem.Substring(1, tem.Length - 2);//大括号的内容体。
                var tarVal = "";//真正作为条件的部分
                if (!tem2.Contains("="))
                {   //当大括号内不含等号，先检查是否为函数模式，未找到时直接作为字符串使用。
                    tarVal = tem2;
                }
                else
                {
                    var temsp = tem2.Split('=');

                    var colType = "string";
                    if (temsp.Length > 1)
                    {
                        colType = temsp[1];
                    }
                    var colname = temsp[0];
                    var cap = context.valueCollection.getColCaptionByName(colname);
                    tarVal = context.valueCollection.getColVal(colname);
                    if (tarVal == null)
                    { //此时列名不合法。
                        if (!context.valueCollection.contain(colname))
                        {
                            context.logger.logError(context.RowMark + "解析列" + colname + "时发现异常！列集合中不存在该列，请检查列的key是否已定义<br/>");
                            errCount++;
                        }
                        else
                        {
                            context.logger.logTip(context.RowMark + "列" + cap + "的值为空，请检查表格是否填写~<br/>");
                            errCount++;
                        }
                        //
                    }

                    Object egg;
                    string msg = "";
                    tarVal = this.parseValue(tarVal, colType, cap, out msg, out egg);
                    if (msg != "") context.logger.logTip(context.RowMark + msg);
                    if (tarVal == null)
                    {//此时必须放弃拼接，否则可能导致异常
                        context.logger.logError(context.RowMark + "列" + cap + "的值不正确，条件校验失败~<br/>");
                        return "";
                    }
                }

                //tarVal = this.sqltool.SqlFilter(tarVal, true);
                res = res.Replace(tem, tarVal);
            }
            return res;
        }
    }
}