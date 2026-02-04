// 基础功能说明：

using mooSQL.excel.context;
using mooSQL.utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace mooSQL.excel
{
    public abstract partial class ExcelRead
    {

        #region 核心写入循环



        /// <summary>
        /// 执行Excel数据体dataTable的写入循环。
        /// </summary>
        public void ReadDataRows()
        {
            workBeforeReadRows();
            var strSQLs = new StringBuilder();
            //为优化性能，更改为每10条进行一次提交。
            int sqlres = 0;//记录sql语句影响的行数
            //for (int i = 0; i < excelDt.Rows.Count; i++)
            foreach (var erow in excelRows)
            {
                this.readingRow = erow.Value;
                var i = erow.Value.dataRowIndex;
                //rowLog = "";
                int max = excelDt.Rows.Count;
                double percent = Convert.ToDouble(i) / Convert.ToDouble(max);
                string result = percent.ToString("0%");//得到6%
                string progress = "正在处理第" + i + "条，已完成" + result + "";
                setProgress(progress);
                //取得拼接结果
                exceptionIndex = i + 1;
                //var kvold = new Dictionary<string,string>(wkinfo.kvmap);
                strSQLs.Append(WriteExcelRow(erow.Value));
                bool toStop = getCacheValue("status").ToString() == "needStop";

                if (strSQLs.Length == 0 && !toStop)
                {
                    continue;
                }

                if (i % 10 == 0 || i == excelDt.Rows.Count - 1)
                {
                    foreach (var tbk in Writelist)
                    {
                        tbk.Value.ployUpdteSQL();
                    }
                }
                if (toStop)
                {
                    pushLog("导入取消！您手动结束了导入工作，前面的数据导入完成，您可以接下来的时候继续导入后面的数据。<br/>", "important");
                    //needStop = false;
                    break;
                }
            }
            this.readingRow = null;

            string lastInfo = "数据处理情况：共计处理" + excelDt.Rows.Count + "条记录。" +
                   "其中成功写入" + context.writelog[0].ToString() + "条记录，" + "数据格式错误而跳过计" + context.writelog[1].ToString() + "条记录，" +
                    "处理重复数据" + context.writelog[2].ToString() + "条记录，" + "未匹配到" + context.writelog[3].ToString() + "条记录。<br/>\n" +
                    "=========数据核查处理已结束=======<br/>\n<br/>";


            pushLog(lastInfo, "important");
            //trans.Commit();
            //最后检查Bulk数据并执行
            pushLog("数据核查处理已结束，正在保存数据。", "important");
            setProgress("数据核查处理已结束，正在保存数据。请稍候...");
            var domsg = doBulk();
            if (domsg == "")
            {
                domsg = "未添加任何数据；";
            }
            if (sqlres > 0)
            {
                domsg += string.Format("进行了{0}条数据更新；", sqlres);
            }
            var msg = string.Format("导入结束！最终执行结果统计：{0}<br/>\n<br/>", domsg);
            writeState = string.Format("本次导入总计发现Excel数据{0}条，尝试写入{1}条，处理重复{2}条", excelDt.Rows.Count, context.writelog[0].ToString(), context.writelog[2].ToString());

            pushLog(msg, "result");

            saveLog();
            setProgress(msg);
            this.setWorkState(true);
        }

        public virtual int saveLog()
        {
            return 0;
        }
        /// <summary>
        /// 执行一行ExcelDataTable的数据的处理。
        /// </summary>
        /// <param name="row">数据行</param>
        /// <param name="index">行指针</param>
        /// <returns></returns>
        public string WriteExcelRow(rowInfo row)
        {   //向主表内写入语句的DataRow row,int[] log,0成功 1数据错误，2重复 3未匹配到
            //excelRowNum dataRowNum;
            context.valueCollection.setInnerColValue("dataRowNum", row.dataRowIndex.ToString());
            readingRow = row;

            context.readingExcelRowIndex = row.excelRowNum;

            row.rowMark = "第" + context.readingExcelRowIndex.ToString() + "行";
            context.valueCollection.setInnerColValue("excelRowNum", context.readingExcelRowIndex.ToString());

            if (context.option.onBeforeParseRow != null)
            {
                var t = context.option.onBeforeParseRow(this, row);
                if (t == false)
                {
                    return "";
                }
            }
            //读取本行excel数据到库
            if (!context.valueCollection.loadRowData(row.dataRow))
            {
                return "";
            }
            if (context.option.onLoadRowTip != null)
            {
                row.rowMark = context.option.onLoadRowTip(this);
            }
            else
            {
                //完善本条记录的输出标识列，补入具体标识字段的信息
                string outv = "";

                var outCol = context.OutputCol;
                outv = outCol.writeValue;
                if (outv == "")
                {
                    outv = "空";
                }
                string cpname = outCol.caption;
                if (cpname == "")
                {
                    cpname = outCol.key;
                }
                row.rowMark += cpname + "为" + outv + "";
            }

            //循环逻辑变更，如果存在动态列，则开启动态列的循环取值。
            if (dynamicCols.Count > 0)
            {
                //多组动态列时，组间循环
                for (int d = 0; d < dynamicCols.Count; d++)
                {
                    //动态列的内部循环
                    var dkey = dynamicCols[d];
                    if (context.valueCollection.contain(dkey) == false) continue;
                    var dcol = context.valueCollection.getCol(dkey);
                    for (int c = 0; c < dcol.dynamicExcelCols.Count; c++)
                    {
                        //执行所有的动态列当前焦点列环境赋值。
                        var tarccode = dcol.dynamicExcelCols[c];
                        dcol.focusIndex = c;
                        context.valueCollection.loadDynamicColData(tarccode, row.dataRow, dcol);

                        //开始执行写入
                        foreach (var kv in Writelist)
                        {
                            var tbinfo = kv.Value;
                            if (tbinfo.customScope && tbinfo.rowScope.Contain(context.readingExcelRowIndex) == false) continue;
                            tbinfo.srcRow = row;
                            ////tbinfo.srcIndex = index;
                            tbinfo.dynamicIndex = d;

                            var dwres = this.doTableWrite(tbinfo);
                            checkClearConnectCol(tbinfo, dwres);
                            if (dwres == breakPoint.excelRowContine) { return ""; }
                            else if (dwres == breakPoint.tableBreak) { break; }
                        }
                    }
                }
            }
            else
            {
                //常规非动态列写入表的数据写入。
                foreach (var kv in Writelist)
                {
                    var tbinfo = kv.Value;
                    if (tbinfo.customScope && tbinfo.rowScope.Contain(context.readingExcelRowIndex) == false) continue;
                    //如果表使用了反转列，需要提前
                    if (tbinfo.option.dynamic == false)
                    {
                        tbinfo.srcRow = row;
                        //tbinfo.srcIndex = index;
                        tbinfo.dynamicIndex = -1;

                        var dwres = this.doTableWrite(tbinfo);
                        checkClearConnectCol(tbinfo, dwres);
                        if (dwres == breakPoint.excelRowContine) { return ""; }
                        else if (dwres == breakPoint.tableBreak) { break; }
                    }
                }
            }

            return "";
        }

        private void checkClearConnectCol(WriteTable tb, breakPoint next)
        {
            if (tb.inserting != 0 && (next == breakPoint.tableContinue || next == breakPoint.tableBreak))
            {
                if (tb.keyColInfo != null)
                {
                    tb.keyColInfo.writeValue = null;
                    tb.keyColInfo.parsedValue = null;
                }
            }
        }

        /// <summary>
        /// 执行一个写入表的写入循环。
        /// </summary>
        /// <param name="li"></param>
        /// <returns></returns>
        //表检查过后的所有数据读写直至形成结果的部分
        public breakPoint doTableWrite(WriteTable li)
        {
            var res = "";//返回值用来控制表的循环和上层代码的执行。这里return空即相当于continue，end则结束上层函数调用，break则结束上层表循环。
            li.clearRowData();
            string jump = "";
            string tablenam = li.option.name;
            //var outinfo = rowinfo;
            var tbinfo = li;
            DataTable liDt = getBaseDataTable(tbinfo.option.DBName);
            if (tablenam == null || (!li.canInsert && !li.canUpdate))
            {
                return breakPoint.tableContinue;
            }
            //处理检查结果
            breakPoint msg = breakPoint.none;
            var checkRows = CheckTable(li, out msg);
            if (msg == breakPoint.clear)
            {
                li.bulk.bulkTarget.Rows.Clear();
                li.updateSQL.Clear();
                //li.oldData.oldData.RejectChanges();
                return msg;
            }
            if (checkRows == null) { return breakPoint.tableContinue; }
            //开始抓取列值
            bool isInsert = checkRows.Length == 0;
            string updateWhere = "";
            //内存模式下需更新缓存数据。
            DataRow liRow;
            if (!isInsert)
            {
                updateWhere = tbinfo.option.keyCol + " ='" + checkRows[0][tbinfo.option.keyCol].ToString() + "'";
            }

            if (context.option.checkMode == "local")
            {
                //var cols = liDt.Columns;
                if (isInsert)
                {
                    liRow = liDt.NewRow();
                }
                else
                {
                    var selectRows = liDt.Select(updateWhere);
                    if (selectRows.Length > 0)
                    {
                        liRow = selectRows[0];
                    }
                    else
                    {
                        liRow = liDt.NewRow();
                    }
                }
            }
            else
            {
                liRow = null;
            }
            //遍历写入字段集合开始拼接**
            //创建bulk模式下的数据

            if (isInsert)
            {
                li.addingRow = li.bulk.newRow();
            }
            else
            {
                li.addingRow = li.oldData.table.NewRow();
                li.StartRow();
            }

            //常规写入列集合处理。
            foreach (var kvc in li.writeCols)
            {
                var col = kvc.Value;
                context.valueCollection.loadWriteColValue(li.srcRow.dataRow, col);

                var ptres = this.patchValueToWrite(col, context.valueCollection.getColVal(col), tbinfo, jump);
                if (ptres == "break" || ptres == "end")
                {
                    context.writelog[3]++;
                    return breakPoint.tableContinue;
                }
            }


            if (jump == "c")
            {
                context.writelog[3]++;
                return breakPoint.tableContinue;
            }
            else if (jump == "b")
            {
                context.writelog[3]++;
                return breakPoint.tableBreak;
            }
            doRowAdd(tbinfo);
            return breakPoint.none;
        }
        /// <summary>
        /// 写入查重的执行
        /// </summary>
        /// <param name="tbinfo"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        private DataRow[] CheckTable(WriteTable tbinfo, out breakPoint msg)
        {
            //从tableinfo对象中获取查询信息
            tbinfo.inserting = 0;
            var custtip = "{auto}";
            if (tbinfo.option != null)
            {
                custtip = tbinfo.option.repeatErrTip;
            }

            msg = breakPoint.none;

            if (tbinfo.option.onCheckRepeat != null)
            {
                tbinfo.checkResult = tbinfo.option.onCheckRepeat(tbinfo); ;
                return tbinfo.checkResult;
            }

            tbinfo.checkingWhere = "";
            //如果表的查重字符串设置为空。则永远执行插入。
            if (tbinfo.option != null && tbinfo.option.repeatWhere == "")
            {
                //return "insert";
                if (tbinfo.canInsert == false)
                {  //需要插入，但表只允许更新时，跳过 redo
                    msg = breakPoint.tableContinue;
                    return null;
                }
                tbinfo.checkResult = new DataRow[0];
                initTableToInsert(tbinfo);
            }

            string tbKey = tbinfo.option.key;
            string tablename = tbinfo.option.name;
            //string checkcols = tbinfo.checkCol;
            int errCount;
            string wherestr = context.valueCollection.formatFreeSQLValue(tbinfo.parsedRepeatWhere, out errCount);
            //如果拼接结果为空，直接报错返回空
            if (errCount > 0 || wherestr == "" || wherestr == null)
            {
                //return "fail";
                context.writelog[3]++;
                string tinfo = readingRow.rowMark + "的" + tbinfo.option.caption + "数据核验失败，处理下一条。<br/>";
                tinfo = this.relaceAndPushTip(custtip, tinfo, "error");
                errInfo += tinfo;
                msg = breakPoint.tableContinue;
                return null;
            }
            if (tbinfo.addedIds.ContainsKey(wherestr))
            {   //此时具体该行标识的数据已经被添加过。
                //回写标识信息到键值对。
                string[] writeback = tbinfo.addedIds[wherestr].Split(',');
                foreach (var w in writeback)
                {
                    string[] sw = w.Split('=');
                    if (sw.Length < 2) continue;
                    context.valueCollection.setColValue(tbinfo.getFieldKey(sw[0]), sw[1]);
                }
                if (tbinfo.option.failPolicy != checkFailAct.silent)
                {
                    this.pushLog(readingRow.rowMark + "检测到数据已经添加，放弃添加。<br/>", "tip");
                }
                msg = breakPoint.tableContinue;
                return null;
                //return "added";
            }
            else
            {
                tbinfo.checkingWhere = wherestr;
            }
            tbinfo.checkResult = getCheckRows(tbinfo.option.DBName, wherestr);
            if (tbinfo.checkResult == null)
            {
                msg = breakPoint.tableContinue;
                return null;
            }
            int coutn = tbinfo.checkResult.Length;
            if (coutn == 0)
            {
                //return "insert";
                if (tbinfo.canInsert == false)
                {  //需要插入，但表只允许更新时，跳过 redo
                    msg = breakPoint.tableContinue;
                    return null;
                }
                initTableToInsert(tbinfo);
            }
            else if (coutn == 1)
            {

                //不论是否允许更新，都需要将查出的结果进行环境返写。
                var oldRow = tbinfo.checkResult[0];
                if (tbinfo.writeBackInfo != null)
                {
                    foreach (var x in tbinfo.writeBackInfo)
                    {
                        string[] ek = x.Split('=');
                        if (ek.Length > 1)
                        {
                            var tarcol = ek[0];
                            if (context.valueCollection.contain(tarcol))
                            {
                                var t=context.valueCollection.getCol(tarcol);
                                t.writeValue = oldRow[ek[1]].ToString();
                                t.parsedValue = oldRow[ek[1]];
                            }
                        }
                    }
                }
                string mkey = tbinfo.keyColInfo.field;
                string mvalue = oldRow[mkey].ToString();
                //更新查出的值到键值结果。
                if (tbinfo.keyColInfo != null)
                {
                    tbinfo.keyColInfo.writeValue = oldRow[mkey].ToString();
                    tbinfo.keyColInfo.parsedValue = oldRow[mkey];
                }


                if (tbinfo.canUpdate == false)
                { //需要更新时，如果全局模式为插入，或者表格模式为插入，跳过
                    context.writelog[2]++;
                    this.relaceAndPushTip(custtip, readingRow.rowMark + "的" + tbinfo.option.caption + "中已存在本数据记录，继续下一个<br/>", "important");
                    errInfo += readingRow.rowMark + "的" + tbinfo.option.caption + "的查找到的记录已存在，跳过不处理<br/>";
                    msg = breakPoint.tableContinue;
                    return null;
                }
                //核查是否设置了更新校验
                if (tbinfo.option.onCheckUpdate != null)
                {
                    if (tbinfo.option.onCheckUpdate(tbinfo, tbinfo.srcRow.dataRow) == false)
                    {
                        return null;
                    }
                }

                else if (!string.IsNullOrWhiteSpace(tbinfo.parsedUpdateWhere))
                {
                    //设置了更新条件
                    string upWH = context.valueCollection.formatFreeSQLValue(tbinfo.parsedUpdateWhere, out errCount);
                    if (!string.IsNullOrWhiteSpace(upWH))
                    {
                        var upck = getCheckRows(tbinfo.option.DBName, upWH);
                        if (upck.Length == 0)
                        {
                            this.relaceAndPushTip(custtip, readingRow.rowMark + "的" + tbinfo.option.caption + "中已存在本数据记录，且未满足更新条件，停止更新操作<br/>", "important");
                            errInfo += readingRow.rowMark + "的" + tbinfo.option.caption + "的查找到的记录已存在，跳过不处理<br/>";
                            msg = breakPoint.tableContinue;
                            return null;
                        }
                    }
                }

                tbinfo.inserting = 2;



                //return mkey + "=" + mvalue;
                //存在一条记录时，返回的是OID，长度为36.

            }
            else
            {
                context.writelog[2]++;
                errInfo += readingRow.rowMark + "检测到存在多条重复记录，不处理<br/>";
                switch (tbinfo.option.failPolicy)
                {
                    case checkFailAct.self:
                        this.relaceAndPushTip(custtip, readingRow.rowMark + "检测到存在多条重复记录，跳过并处理下一个写入表。<br/>", "error");
                        msg = breakPoint.tableContinue;
                        break;
                    case checkFailAct.silent:
                        msg = breakPoint.tableContinue;
                        break;
                    case checkFailAct.next:
                        this.relaceAndPushTip(custtip, readingRow.rowMark + "检测到存在多条重复记录，放弃本行后续导入并处理下一行。<br/>", "error");
                        msg = breakPoint.tableBreak;
                        break;
                    case checkFailAct.row:
                        this.relaceAndPushTip(custtip, readingRow.rowMark + "检测到存在多条重复记录，放弃本行数据导入并处理下一行。<br/>", "error");
                        msg = breakPoint.excelRowContine;
                        break;
                    case checkFailAct.excel:
                        //strSQLTemp.Clear();
                        this.relaceAndPushTip(custtip, readingRow.rowMark + "检测到存在多条重复记录，放弃本次Excel导入。<br/>", "error");
                        msg = breakPoint.excel;
                        break;
                    default:
                        msg = breakPoint.tableContinue;
                        break;
                }
            }
            return tbinfo.checkResult;
        }
        /// <summary>
        /// 写入表查重发现为插入状态的处理。
        /// </summary>
        /// <param name="tbinfo"></param>
        private void initTableToInsert(WriteTable tbinfo)
        {
            tbinfo.inserting = 1;
            //校验记录不存在，且要执行插入，此时需要初始化主键列的值。
            tbinfo.keyColInfo.writeLocked = false;
            context.valueCollection.loadColData(tbinfo.srcRow.dataRow, tbinfo.keyColInfo);
            tbinfo.keyColInfo.writeLocked = true;
        }


        /// <summary>
        /// 将变化信息值，存入写入环境。
        /// </summary>
        /// <param name="col"></param>
        /// <param name="val"></param>
        /// <param name="tbinfo"></param>
        /// <param name="jump"></param>
        /// <returns></returns>
        public string patchValueToWrite(colInfo col, string val, WriteTable tbinfo, string jump)
        {
            /* 列循环--列值纳入  将写入所需数据从kvmap中取出，放置到写入的存储容器tabmap或者bulkrow中
             * 返回控制符：end=终止excel本行写入  break=终止本表的行插入  
             */
            var checkRows = tbinfo.checkResult;
            bool isInsert = checkRows.Length == 0;
            string colname = col.field;
            string res = "";

            //colInfo col = wkinfo.colMap[colname];
            if ((isInsert && col.canInsert == false) || (!isInsert && col.canUpdate == false))//插入模式
            {
                return "";
            }
            bool isneed = col.isNeed;

            string valstr = "";
            int errCount = 0;
            //值类型转换
            Object egg;
            valstr = context.valueCollection. parseValue(val, col.colType, col.caption, out res, out egg);
            bool isUnvalid = val == null || errCount > 0 || valstr == null || (col.colType == valueType.number && (val == "" || val == null));
            bool checkOk = true;
            if (context.valueCollection.isValid(col.rule))
            {
                checkOk = col.checkRule(val);
            }
            if (isUnvalid || checkOk == false)
            {
                if (isneed || checkOk == false)
                {
                    var rowtip = string.Format("{0}的{1}的列{2}值【{3}】", readingRow.rowMark, tbinfo.option.caption, col.caption, val);
                    if (isUnvalid)
                    {
                        rowtip += "值为空";
                    }
                    else
                    {
                        rowtip += "列值核验未通过";
                    }
                    switch (tbinfo.option.failPolicy)
                    {
                        case checkFailAct.self:
                            jump = "c";//继续下一写入表
                            pushLog(rowtip + "，继续处理下一部分。<br/>", "important");
                            break;
                        case checkFailAct.silent:
                            jump = "c";
                            break;
                        case checkFailAct.next://保留之前的导入表数据，丢弃本表和后续数据
                            jump = "b";
                            pushLog(rowtip + "，放弃本行后续导入并处理下一行。<br/>", "error");
                            break;
                        case checkFailAct.row://丢弃本行数据并扫描下一行excel
                            pushLog(rowtip + "，放弃本行数据导入并处理下一行。<br/>", "error");
                            return "end";
                        case checkFailAct.before://清除之前的并继续
                            jump = "c";
                            tbinfo.bulk.bulkTarget.RejectChanges();
                            tbinfo.updateSQL.Clear();
                            tbinfo.addedIds.Clear();
                            pushLog(rowtip + "，放弃本行先行数据导入。<br/>", "error");
                            res = "continue";
                            return res;
                    }
                    return "break";
                }
                else
                {   //因提示信息太多，筛查不便，放弃循环中的小提示的显示和记录
                    //this.pushLog(outinfo + "的" + tbinfo.cation + "的列" + ccname + "未找到对应值。<br/>", "tip");
                    return "continue";
                }
            }
            if (isInsert)
            {
                tbinfo.addingRow.BeginEdit();
                egg = DictExtension.shapeDataType(egg, col.dataType);
                tbinfo.addingRow[col.field] = egg;
                tbinfo.addingRow.EndEdit();
            }
            else
            {
                //在更新时，只对发生变化的列进行更新。

                var oldval = checkRows[0][colname].ToString();

                var issame = tool.compareValue(col.dataType.ToString(), oldval, val);
                if (issame == false)
                {
                    if (tbinfo.option.batchUpdate)
                    {//批量更新时，只需直接修改查询数据表的数据。
                        checkRows[0][colname] = egg;
                    }
                    else
                    {
                        tbinfo.set(colname, egg);
                        //tbinfo.updatekv.AddNotNull( colname, valstr);
                    }
                }
            }

            return res;
        }
        /// <summary>
        /// 执行一个写入表最终的写入工作。
        /// </summary>
        /// <param name="tbinfo"></param>
        /// <param name="checkRows"></param>
        /// <returns></returns>
        private string doRowAdd(WriteTable tbinfo)
        {
            //创建本表的最终sql语句
            if (tbinfo.option.onBeforeRowAdd != null)
            {
                var ckre = tbinfo.option.onBeforeRowAdd(tbinfo);
                if (ckre == false) return "";
            }
            if (context.option.onBeforeRowAdd != null)
            {
                var ckre = context.option.onBeforeRowAdd(tbinfo);
                if (ckre == false) return "";
            }
            var checkRows = tbinfo.checkResult;
            var res = new StringBuilder();
            string tablenam = tbinfo.option.name;
            if (checkRows.Length == 0 && tbinfo.canInsert)
            {  //插入 必定使用批量写入。不再保留SQL语句插入
                context.writelog[0]++;
                tbinfo.bulk.addRow(tbinfo.addingRow);

                if (context.option.checkMode == "local")
                {
                    //liDt.Rows.Add(liRow);
                    //liDt.AcceptChanges();
                    int errc;
                    //检查回写的字段标识信息。
                    string writeback = "";
                    var oidval = context.valueCollection.getColVal(tbinfo.getFieldKey(tbinfo.option.keyCol));
                    if (context.valueCollection.isValid(oidval))
                    {
                        writeback = tbinfo.option.keyCol + "=" + oidval;
                    }

                    if (tbinfo.writeBackInfo != null)
                    {
                        foreach (var x in tbinfo.writeBackInfo)
                        {
                            string[] ek = x.Split('=');
                            writeback += writeback == "" ? "" : ",";
                            writeback += ek[0] + "=" + context.valueCollection.getColVal(tbinfo.getFieldKey(ek[0]));
                        }
                    }
                    //更新查出的值到键值结果。
                    if (context.valueCollection.isValid(tbinfo.checkingWhere))
                        tbinfo.addedIds.AddNotNull(tbinfo.checkingWhere, writeback);
                }

            }
            else if (tbinfo.canUpdate && checkRows.Length == 1)
            {
                context.writelog[2]++;//重复标记
                pushLog(readingRow.rowMark + "的" + tbinfo.option.caption + "查到重复记录，尝试更新其信息字段<br/>", "important");

                //检查自由update项，混入拼接"DQ_Content = {colname=coltype}, dq = { colname2 = coltype}" 
                if (tbinfo.option.batchUpdate == false)
                {
                    var oid = checkRows[0][tbinfo.keyColInfo.field];
                    //var keycolUpdate = string.Format("{0}='{1}'", tbinfo.keyColInfo.field, oid);
                    tbinfo.rowKit.where(tbinfo.keyColInfo.field, oid);
                    tbinfo.rowKit.setTable(tbinfo.option.DBName);
                    tbinfo.EndUpdate();
                    //tbinfo.updateSQL.Append(tbinfo.DBInstance.expression.dealUpdate(tbinfo.option.DBName, tbinfo.updatekv, keycolUpdate));
                }
            }
            return res.ToString();
        }
        #endregion
    }
}
