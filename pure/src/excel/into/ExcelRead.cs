using System;
using System.Collections.Generic;
using System.Text;
using System.Data;


//using NPOI.SS.UserModel;
using System.Text.RegularExpressions;



using System.IO;

using mooSQL.utils;
using mooSQL.excel.context;
using mooSQL.data;

namespace mooSQL.excel
{
    /* 
     * 更新 2022-8-11 处理禁用更新时未回写环境列值，导致子表插入失败问题。
     * 更新 2022-4-25 在表数据处理失败时，清理其主键值
     * 更新 2021-10-9 修复服务端配置情况下主键未定义时，回写主键异常的问题。增加表行校验的更新条件updateWhere和自定义钩子onCheckUpdate 
     * 更新 2021-9-29 修复表的主键被定义key后，回写主键值异常的问题。
     * 更新 2021-8-8  调整代码结构，迁移主体读取代码saveDataBase 到本类，移除入口相应代码。增加导入Excel的消息写入功能。
     * 更新 20210714  修复同一实体表一行Excel多次写入错误的问题。
     * 新需求：全面剔除登录人信息的生成，由前端传入useroid,然后自己生成。
     * 更新 20210311：自定义行提示、错误提示。列校验错误提示。登录用户兼容性加强，可由客户端传入主键。导入的文件写入列标识供下载。导入列可定义替换正则，主要实现自动清除列值内的空格字符功能。excelRowNum dataRowNum;。
     * 更新 20210219  增加表的数据行范围定义功能，修复空标题行的无法导入的BUG。
     * 更新 20210130  修复行提示错误和更新计数错误的问题。
     * 更新 20210118  增加标题列所在行自动侦测、数据行格式预先校验、列范围组合匹配功能、rule变更为超前解析添加正则支持。。
     * 更新 20210114  服务配置动态列适配完成，在ImportOption类下增加一组助手类方法，便于添加配置信息。
     * 更新 20210107  初步完成服务端配置功能基础改造。兼容前端配置、服务端配置
     * 更新 20210103  大修，增加服务端读取配置和服务端配置的功能。分离出ImportOption类。
     * 更新 20201218  优化代码表解析，当值为代码值时，直接使用。
     * 更新 20201206  本版范围核查功能初步实装whereIn属性。
     * 更新 20201203  尝试在baseWhere中 新增 ${}形式的模板字符串功能。
     * 更新 20201112，增加服务端回调的功能，增加计算列下列切割的功能。
     * 创建 20200809 在之前导入版本的基础上，进行非兼容式的大幅修改。
     */
    /// <summary>核心导入写入处理对象。为导入处理期间的最高级别对象。
    /// 本版，移除计算列，合并固定列、查询列、计算列，并入查询列，统一采用对象语法，查询列老格式不再支持。合并固定值列到查询列中。
    /// 移除插入逻辑的SQL语句选项模式，插入状态全部使用批量写入。只保留更新的SQL项，批量更新使用updateTable。
    /// 重点新功能：指定标题行（可多行），指定数据行起止位。多行写入模式。自定义代码映射模式
    /// 
    /// 不再支持写入列的free模式。相关逻辑应在导入后再处理。
    /// 规范化是否格式的语法，统一为 YES/NO 
    /// </summary>


    /*
    * 生命周期
    * 上传Excel及导入参数后。
    * 到达 ExcelFileUpload.apsx.cs初始化
    * 根据mantoken实例化工作环境类ExcelRead
    ->是查询，转入导入进度获取
    ->是导入，
    * 读取导入配置  
    * 保存Excel文件
    * 读取Excel到dataTable中  wkinfo.readExcelData(workbook)
    * 开启异步线程， saveToDatabase
    * 核查dataTable数据，关联配置  checkExcelData
    * 连接数据库
    * 开启表体读取  wkinfo.ReadDataRows
    * 读取环境准备，如读取备查数据，读取动态类配置，添加环境列等 workBeforeReadRows
    * 开启行循环
    * 读取一行Excel数据 WriteExcelRow
    * 加载行数据到列集合 loadRowData
    * 开启动态列写入循环或者写入表循环
    * 对某个写入表进行写入 doTableWrite
    * 执行查重核验 CheckTable
    * 循环读写写入列集合 loadWriteColValue->patchValueToWrite
    * 添加写入行 doRowAdd
    */
    /* 多行写入模式下，列的目标应用单元格在一个范围内发生变动。使用 dynamic  动态关键字。
    * 标题行的列名匹配使用 crosshead1,head2 格式 即 head+行号，
    /// 范围核查wherein 条件设置语法：核查列可以取到 固定值列、cell列、表值列，不可取到查询列、计算列、动态列（因为查询尚未执行，行循环尚未开始）
    /// { field:"要核查的字段名",excelCol:"要核查使用的excel列名",cell:"",src:"其他的列的键"}
    //数据查询模式，分为local内存查询和database数据库查询。内存查询优点是速度快，缺点是准备时间长，数据量越大越明显。数据库查询优点是无前摇，但执行写入速度慢
    */

    public abstract partial class ExcelRead:ExcelBase
    {
        public ExcelRead(string token)
        {
            this.workToken = token;
            //this.author = new UserInfo();

            this.context = new ReadingContext();
            context.logger = new MsgOutput();
            context.logger.onLogging = (msg, type) => { 
                this.pushLog(msg, type);
            };  

            context.valueCollection= new ReadyValueCollection();
            context.valueCollection.context = context;
        }

        public abstract DBInstance GetDBInstance(int position);

        #region 工作环境字段


        public ReadingContext context;
        /// <summary>
        /// 与单个Excel行无关的消息
        /// </summary>
        public string totalMsg = "";
        public callbackInfo onReadConfig;
        public UserInfo author;
        public string tablena = "";
        public string excelName = "";

        public string excelFilePath = "";
        public DataTable excelDt;
        public int lastTitleNum = 1;
        /// <summary>
        /// 已读取到的excel行信息
        /// </summary>
        public Dictionary<int, rowInfo> excelRows = new Dictionary<int, rowInfo>();
        /// <summary>
        /// excel中的列编号和列标题信息
        /// </summary>
        public Dictionary<string, ExcelCol> excelCols = new Dictionary<string, ExcelCol>();
        //记录数据体与原先excel表格中的位置的关系。

        //public Dictionary<int, int> excelRowMap = new Dictionary<int, int>();
        public string savePath;

        public IntSection titlsScanScope = new IntSection();//标题行扫描范围
        public IntSection excelTitleRow = new IntSection();//标题信息所在的行
        public IntSection excelDataRow = new IntSection(); //数据体信息所在的行，[8,10-11,200-]
        //public List<int> readedRowIndex = new List<int>();//读取过程中记录已读取的行号。
        public List<int> readedColIndex = new List<int>();//读取过程中记录已读取的列号。


        public System.DateTime currentTime = new System.DateTime();
        public Dictionary<int, List<string>> excelCheckColData = new Dictionary<int, List<string>>();
        /// <summary>
        /// 需要执行whereIn操作的 列key。
        /// </summary>
        public List<string> excelCheckColnames = new List<string>();
        /// <summary>
        /// 需要执行whereIn操作的 Excel列。
        /// </summary>
        public List<int> excelCheckColIndex = new List<int>();
        /// <summary>
        /// 超前核验数据的excel表格列和其正则表达式。
        /// </summary>
        public Dictionary<int, List<colInfo>> excelPreMatches = new Dictionary<int, List<colInfo>>();
        /// <summary>
        /// 正在执行处理的excel数据dataTable的行记录。
        /// </summary>
        public rowInfo readingRow= new rowInfo();

        public string strConn;



        //输出标识列名
        public Action<string, string> workMsg;
        public callbackInfo beforeSave;//导入结束（dobulk）执行前，调用回调函数。
        public callbackInfo afterSave;//导入结束（dobulk）执行结束后，调用回调函数。
        public string workToken = "";

        //public string outInfoCol;
        //public string mode = "insert";//write/insert/update  即写入模式
        public string multiWritePolicy = "none"; //多表写入策略 none/solo/together,
        //public bool logtips = false;//输出日志时输出提示类的信息，默认关闭。
        //public bool batchUpdate = false;//是否启用批量更新

        public string info;

        public int exceptionIndex = 0;
        public string errInfo = ""; //错误信息
        //public string rowinfo = "";  //行标识信息
        //public string rowLog = ""; //行读写日志
        public string writeState = "";
        /// <summary>
        /// 记录下交叉列，以便于核验excel时进行解析。
        /// </summary>
        public List<string> dynamicCols = new List<string>();
        /// <summary>
        /// 数据核验表信息
        /// </summary>
        public Dictionary<string, checkTable> baseTable = new Dictionary<string, checkTable>();
        /// <summary>
        /// 写入数据库的表信息
        /// </summary>
        public Dictionary<string, WriteTable> Writelist = new Dictionary<string, WriteTable>();

        /// <summary>
        /// 日志写入对象
        /// </summary>

        private myUntils tool = new myUntils();
        /// <summary>
        /// 日志输出委托
        /// </summary>
        public Action<string, string> onLog;
        #endregion



 

        #region Excel数据读取和分析
        /// <summary>
        /// 校验已经加载到dataTable中的数据
        /// </summary>
        /// <param name="excelDt"></param>
        /// <returns></returns>
        public Boolean checkExcelData(DataTable excelDt)
        {
            this.setProgress("正在检查excel数据...");
            //创建列名的值索引
            var columns = excelDt.Columns;
            for (int i = 0; i < columns.Count; i++)
            {
                //将基本列定义的部分获取到列指针。
                string cname = columns[i].Caption;
                string ccode = columns[i].ColumnName;
                foreach (var kv in context.colMap)
                {
                    var col = kv.Value;
                    if (col.type != columnType.match || col.ExcelIndex != -1) continue;
                    var matchstr = col.excelCol;
                    if (isMatch(cname, col.excelCol))
                    {
                        col.ExcelIndex = i;
                        col.excelCode = ccode;
                    }
                }
                //检查动态列参数
                foreach (var c in dynamicCols)
                {
                    if (context.valueCollection.contain(c) == false) continue;
                    var co = context.valueCollection.getCol(c);
                    if (string.IsNullOrWhiteSpace(co.reg) == false)
                    {
                        if (isMatch(cname, co.reg))
                        {
                            co.dynamicExcelCols.AddNotRepeat( ccode);
                            continue;
                        }
                    }
                    if (string.IsNullOrWhiteSpace(co.range) == false)
                    {
                        if ( ExcelUntil.checkInColRange(co.range, ccode))
                        {
                            co.dynamicExcelCols.AddNotRepeat( ccode);
                            continue;
                        }

                    }
                }
            }
            //检查基本列的信息完备性。
            return context.valueCollection.check();
        }

        /// <summary>
        /// 添加列标题信息
        /// </summary>
        /// <param name="row">表格中的行标号，从1开始</param>
        /// <param name="excelCode">表格中的列号,从A开始</param>
        /// <param name="title">标题信息。</param>
        public void addExcelTitleInfo(int row, string excelCode, string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return;
            if (excelCols.ContainsKey(excelCode) == false)
            {
                var eco = new ExcelCol();
                eco.code = excelCode;
                excelCols.Add(excelCode, eco);
            }

            excelCols[excelCode].titles.AddNotNull(row, title);
            
        }

        /// <summary>
        /// 预处理超前检查数据的信息，主要是excel的列号和正则。
        /// </summary>
        public void preparePreExcelMatch()
        {
            foreach (var kv in context.colMap)
            {
                if (kv.Value.option.preMatch)
                {
                    var ie = kv.Value.ExcelIndex;
                    if (excelPreMatches.ContainsKey(ie) == false)
                    {
                        excelPreMatches.Add(ie, new List<colInfo>());
                    }
                    excelPreMatches[ie].Add(kv.Value);
                }
            }
        }


        /// 获取所有的匹配行的匹配名称。拼接成一个正则表达式。


        public List<string> getAllTitleMatchReg()
        {
            var res = new List<string>();
            foreach (var kv in context.colMap)
            {
                if (kv.Value.type == columnType.match && isValid(kv.Value.excelCol))
                {
                    tool.ListAdd(res, kv.Value.excelCol);
                }
            }
            return res;
        }
        #endregion

        #region 主体写入循环前的准备工作
        public void saveToDatabase()
        {
            //DataTable dataTb,string savePath
            setWorkState(false);
            try
            {
                currentTime = System.DateTime.Now;
                var checkresult = checkExcelData(excelDt);
                if (!checkresult)
                {
                    pushLog("检测到Excel文件数据不符，请检查后重新导入！导入结束。\n<br/> ", "error");
                    setProgress("Excel文件格式不符合要求，请核查后重试，导入结束。");
                    setWorkState(true);
                    return;
                }

                //此处开始连接数据库

                


                var strSQLs = new StringBuilder();
                //异常统计数组，依次为：正常、数据格式错误、人员重复、未找到此人。
                context.writelog = new int[4] { 0, 0, 0, 0 };
                try
                {
                    ReadDataRows();
                    this.setWorkState(true);
                }
                catch (Exception exc)
                {
                //如果某个环节出现问题，则将整个事务回滚

                    //trans.Rollback();
                    var mark = "";
                    if (readingRow != null) {
                        mark = readingRow.rowMark;
                    }
                        

                    pushLog("导入过程中发生错误:'" + exc.Message + "'。" + "<br/>" + exc.StackTrace, "fatal");
                    pushLog("操作中断于第" + exceptionIndex + "条记录，行标识：" + mark + "请检查数据后重新操作。" + ".\n<br/>", "error");
                    writeState = "写入错误，请检查" + mark + "条数据记录，修复后重新导入！导入结束。";
                    string doneinfo = writeState;
                    setProgress(doneinfo);
                }
                finally
                {
                    //cmd.Dispose();
                    //conn.Close();
                }
                
            }
            catch (Exception ex)
            {
                pushLog("导入处理已结束:" + ex.Message + "，您可以关闭窗口后重新尝试。" + ".<br/>", "");
                this.setWorkState(true);
            }
            finally
            {
                setCacheValue("status", "stoped");
                /*
                //关闭连接
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
                conn.Dispose();
                //删除上传的Excel文件
                if (System.IO.File.Exists(savePath))
                {
                    //System.IO.File.Delete(wkinfo.savePath);
                }
                */

                this.setWorkState(true);
                //this = new ExcelRead(Guid.NewGuid().ToString());
                ////
            }
        }

        #endregion



        public virtual string Invoke(callbackInfo callback, Object[] para) {
            return "";
        }

        #region 最终保存
        /// <summary>
        /// 执行导入处理结果的数据库保存动作。
        /// </summary>
        /// <returns></returns>
        public string doBulk()
        {
            var msg = new StringBuilder();
            if (context.option.onBeforeSave != null)
            {
                var bsv = context.option.onBeforeSave(this,msg);
            }
            //对每个写入表，检查并执行写入
            long cc = 0;

            if (this.beforeSave != null)
            {
                try
                {
                    var pams = new object[] { this };
                    var cbmsg = this.Invoke(beforeSave, pams);
                    //cbtool.getSolutionValue(out cbreturn, beforeSave.BPOName, beforeSave.Method, pams, out cbmsg);
                    msg .Append( cbmsg);
                }
                catch (Exception e)
                {
                    pushLog(string.Format("导入后的业务处理逻辑调用失败！请核查模块{0}下的方法{1}<br/>", beforeSave.BPOName, beforeSave.Method), "important");
                }

            }
            foreach (var kv in Writelist)
            {   //获取基准列类型数据
                var tb = kv.Value;
                tb.save();
                if (tb.canInsert)
                {
                    cc += tb.insertCount;
                    if (tb.insertCount > 0) msg .AppendFormat("表【{0}】成功写入{1}条数据;",tb.option.caption, tb.insertCount);
                }
                if (tb.canUpdate)
                {

                    if (tb.updateCount>0) msg.AppendFormat("表【{0}】成功更新{1}条数据;", tb.option.caption, tb.updateCount);
                }
            }
            if(this.afterSave != null)
            {
                try
                {
                    var pams = new object[] { this };
                    var cbmsg=this.Invoke(afterSave, pams);
                    msg .Append( cbmsg);
                }
                catch(Exception e)
                {
                    pushLog(string.Format("导入后的业务处理逻辑调用失败！请核查模块{0}下的方法{1}<br/>", afterSave.BPOName, afterSave.Method), "important");
                }
            }
            if (context.option.saveMsgToExcel)
            {
                this.saveMsgToExcel();
            }
            if (context.option.onAfterSave!=null) {
                msg .Append( context.option.onAfterSave(this));
            }
            return msg.ToString();
        }
        /// <summary>
        /// 保存消息到表格
        /// </summary>
        public abstract void saveMsgToExcel();




        #endregion



        #region SQL配置的解析和读取



        /// <summary>
        /// 根据解析好的where条件，获取查询结果。
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="sqlWhere"></param>
        /// <returns></returns>
        public DataRow[] getCheckRows(string tableName, string sqlWhere)
        {
            DataRow[] rows;
            int errcout;
            string wherestr = context.valueCollection.formatFreeSQLValue(sqlWhere, out errcout);
            if (wherestr == "" || errcout > 0)
            {
                return null;
            }
            if (context.option.checkMode == "local")
            {
                DataTable tardt = this.getBaseDataTable(tableName);
                rows = tardt.Select(wherestr);
            }
            else
            {
                string wherepart = wherestr;
                if (!baseTable.ContainsKey(tableName))
                {
                    return null;
                }
                var tb = baseTable[tableName];
                if (tb.checkWhere != "")
                {
                    wherepart += wherepart == "" ? "" : " and ";
                    wherepart += tb.checkWhere;
                }
                if (wherepart != "")
                {
                    wherepart = " where " + wherepart;
                }
                string colnames = tb.readCols.JoinNotEmpty( ",");
                string findSQLs = string.Format("select {0} from {1} {2}", colnames, tb.DBName, wherepart);
                DataTable temptdt = tb.DBInstance.ExeQuery(findSQLs,new data.Paras());
                rows = temptdt.Select();
            }
            return rows;
        }
        /// <summary>
        /// 格式化形如PC_CertNum=PC_CertNum=string的条件字符串。
        /// </summary>
        /// <param name="oldStr"></param>
        /// <param name="tableName"></param>
        /// <param name="srcCols"></param>
        /// <returns></returns>
        private string formatWhereStr(string oldStr, string tableName, out List<string> srcCols)
        {
            string[] wht = oldStr.Split(';');
            srcCols = new List<string>();
            var whereStr = "";
            foreach (var wh in wht)
            {
                if (isValid(wh) == false) continue;
                if (wh.IndexOf('{') == -1)
                { //简单模式  PC_CertNum=PC_CertNum=string
                    var strArr = wh.Split('=');
                    //拼接where条件
                    if (strArr.Length < 2) { continue; }
                    string coltypee = strArr.Length > 2 ? strArr[2] : "string";
                    string sval = "{" + strArr[1] + "=" + coltypee + "}";
                    srcCols.Add(strArr[1]);
                    whereStr += whereStr != "" ? " and " : " ";
                    whereStr += string.Format(" {0} = {1}", strArr[0], sval);
                    addCheckCol(tableName, strArr[0]);
                }
                else
                { //自由项模式，直接拼接，等待格式化。
                    whereStr += whereStr != "" ? " and " : " ";
                    whereStr += wh;
                }
            }

            return whereStr;
        }
        /// <summary>
        /// 格式化列的查询列条件。
        /// </summary>
        /// <param name="col"></param>
        private void formatColWherePart(colInfo col)
        {
            //查询列时，完备性检查
            if (col.type == columnType.select)
            {
                if (isValid(col.selectCol) == false)
                {   //无选取列，错误语法
                    this.pushLog("导入设置的" + col.key + "语句不合法:查询列未设置查询选取字段名，请检查导入信息设置！<br/>", "important");
                    return;
                }
                if (isValid(col.selectWhere) == false)
                {   //无选取列，错误语法
                    this.pushLog("导入设置的" + col.key + "语句不合法：查询列未设置查询选取条件，请检查导入信息设置！<br/>", "important");
                    return;
                }
                col.formatWhere = formatWhereStr(col.selectWhere, col.selectTable, out col.srcCols);
            }
        }

        /// <summary>
        /// 格式化形如${key}格式的字符串。
        /// </summary>
        /// <param name="freeStr"></param>
        /// <param name="errCount"></param>
        /// <returns></returns>
        public string formatSqlKey(string freeStr, out int errCount)
        {

            const string regs = @"${.*?}";
            errCount = 0;
            string res = freeStr;
            MatchCollection matches = Regex.Matches(freeStr, regs);
            foreach (Match x in matches)
            {
                var tem = x.Value;
                if (tem.Length < 4) continue;
                tem = tem.Substring(2, tem.Length - 3);//大括号的内容体。
                var colname = Regex.Replace(tem, @"\s", "");
                var tarVal = context.valueCollection.getColVal(colname);
                if (tarVal == null)
                { //此时列名不合法。
                    if (!context.valueCollection.contain(colname))
                    {
                        this.pushLog(readingRow.rowMark + "解析列" + colname + "时发现异常！列集合中不存在该列，请检查列的key是否已定义<br/>", "error");
                        errCount++;
                    }
                    else
                    {
                        this.pushLog(readingRow.rowMark + "解析列" + colname + "的值发现异常，请检查定义<br/>", "error");
                        errCount++;
                    }
                }
                else
                {
                    res = res.Replace(x.Value, tarVal);
                }
            }
            return res;
        }
        #endregion

        #region 日志处理与输出
        public virtual void WriteLog(string type, string content)
        {

        }
        /// <summary>
        /// 获取可以直接返回前端的进度信息，如果已结束，会清理环境
        /// </summary>
        /// <returns></returns>
        public InWorkProgress getWorkInfo()
        {
            var res = new InWorkProgress();
            var progerss = getProgress();

            res.progress = progerss;
            string wklog = getWorkLog();
            var isDone = getIsDone();
            if ( progerss.Contains("导入结束"))
            {
                isDone = true;
            }
            if (isDone)
            {
                clearCache();
                res.isdone= true;
            }
            else
            {
                res.isdone = false;
                if (wklog.Length > 2000)
                {
                    wklog = wklog.Substring(0, 1900);
                }
            }
            res.log = wklog;
            return res;
        }
        public void clearCache()
        {
            removeCache("workprogress");
            removeCache("workinfo");
            removeCache("workState");
        }
        /// <summary>
        /// 获取导入进度信息
        /// </summary>
        /// <returns></returns>
        public string getProgress()
        {
            return getCacheValue("workprogress"); 
        }
        /// <summary>
        /// 获取导入的日志
        /// </summary>
        /// <returns></returns>
        public string getWorkLog()
        {
            return getCacheValue("workinfo");
        }
        /// <summary>
        /// 获取是否已经完成导入
        /// </summary>
        /// <returns></returns>
        public bool getIsDone()
        {
            var st= getCacheValue("workState");
            if (string.IsNullOrWhiteSpace(st))
            {
                return true;
            }
            if (st == "NO")
            {
                return false;
            }
            return true;
        }
        public void setWorkState(bool isdone)
        {
            var st = isdone ? "YES" : "NO";
            setCacheValue("workState", st);
        }
        public void setProgress(string progress)
        {
            this.setCacheValue("workprogress", progress);
        }
        /// <summary>
        /// 推送消息到日志和前端页面中
        /// </summary>
        /// <param name="msg">消息内容</param>
        /// <param name="type">重要程度，包含fatal/error/important/tip</param>
        public void pushLog(string msg, string type)
        {
            this.WriteLog(type, msg);
            if (onLog != null) {
                onLog(type, msg);
            }
            if (this.readingRow != null && !readingRow.empty)
            {
                readingRow.rowMsg += msg;
            }
            else
            {
                this.totalMsg += msg;
            }
            var fm = string.Format("<div class=\"{1}\">{0}</div>", msg, type);
            //if (type !="tip") {
            info = fm + info;
            this.setCacheValue("workinfo", info);
            if (workMsg != null)
            {
                workMsg.Invoke(type, msg);
            }

        }
        #endregion


    }
}
