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

    /// <summary>
    /// 核心导入写入处理类型，在单次 Excel 导入会话中协调配置、数据校验、逐行解析与批量落库。
    /// </summary>
    /// <remarks>
    /// <para>设计要点：固定列与查询列等统一在查询列语义下配置；插入走批量写入；更新可走 SQL 或批量更新（见写入表配置）。支持多行标题、数据行区间、多表写入策略（<see cref="multiWritePolicy"/>）及 <see cref="beforeSave"/> / <see cref="afterSave"/> 回调。</para>
    /// <para>本类为 partial 抽象基类，具体读行与准备逻辑在其它分部文件中；子类需实现 <see cref="GetDBInstance"/>、<see cref="saveMsgToExcel"/> 等。</para>
    /// </remarks>
    public abstract partial class ExcelRead:ExcelBase
    {
        /// <summary>
        /// 使用工作令牌初始化读取上下文、日志通道与列值集合。
        /// </summary>
        /// <param name="token">标识本次导入作业的令牌（如 manToken）。</param>
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

        /// <summary>
        /// 按写入位序解析并返回数据库访问实例（多连接场景由子类实现）。
        /// </summary>
        /// <param name="position">写入目标或连接槽位序号。</param>
        /// <returns>用于执行查询与命令的 <see cref="DBInstance"/>。</returns>
        public abstract DBInstance GetDBInstance(int position);

        #region 工作环境字段


        /// <summary>
        /// 本次导入的运行时上下文：列映射、导入选项、备查值集合与日志等。
        /// </summary>
        public ReadingContext context;
        /// <summary>
        /// 与单个Excel行无关的消息
        /// </summary>
        public string totalMsg = "";
        /// <summary>
        /// 导入配置读取完成后的可选回调（由宿主注册）。
        /// </summary>
        public callbackInfo onReadConfig;
        /// <summary>
        /// 当前操作用户信息（可由前端传入 user 等字段填充）。
        /// </summary>
        public UserInfo author;
        /// <summary>
        /// 主业务表名或当前导入针对的表标识。
        /// </summary>
        public string tablena = "";
        /// <summary>
        /// 原始 Excel 文件名（展示或日志用）。
        /// </summary>
        public string excelName = "";

        /// <summary>
        /// 已上传到服务端的 Excel 文件物理路径。
        /// </summary>
        public string excelFilePath = "";
        /// <summary>
        /// 从工作簿载入后的 <see cref="DataTable"/>，校验与逐行处理均基于此表。
        /// </summary>
        public DataTable excelDt;
        /// <summary>
        /// 最近一次解析到的标题行行号（从 1 起计）。
        /// </summary>
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
        /// <summary>
        /// 上传文件保存目录或中间路径（与宿主配置一致）。
        /// </summary>
        public string savePath;

        /// <summary>
        /// 标题行自动扫描的 Excel 行范围。
        /// </summary>
        public IntSection titlsScanScope = new IntSection();//标题行扫描范围
        /// <summary>
        /// 列标题所在的 Excel 行区间。
        /// </summary>
        public IntSection excelTitleRow = new IntSection();//标题信息所在的行
        /// <summary>
        /// 数据体所在行区间，支持片段写法（如 8、10-11、200-）。
        /// </summary>
        public IntSection excelDataRow = new IntSection(); //数据体信息所在的行，[8,10-11,200-]
        //public List<int> readedRowIndex = new List<int>();//读取过程中记录已读取的行号。
        /// <summary>
        /// 读取过程中已处理过的列索引列表。
        /// </summary>
        public List<int> readedColIndex = new List<int>();//读取过程中记录已读取的列号。


        /// <summary>
        /// 开始执行导入处理时的当前时间（用于默认值、时间戳列等）。
        /// </summary>
        public System.DateTime currentTime = new System.DateTime();
        /// <summary>
        /// 按列索引缓存的待核查单元格文本，用于范围或重复性校验。
        /// </summary>
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

        /// <summary>
        /// 数据库连接字符串（宿主统一传入时使用）。
        /// </summary>
        public string strConn;



        //输出标识列名
        /// <summary>
        /// 工作消息输出委托（消息类型与 HTML 内容，供 UI 或外部日志消费）。
        /// </summary>
        public Action<string, string> workMsg;
        /// <summary>
        /// 在 <see cref="doBulk"/> 执行批量写库之前调用的业务回调。
        /// </summary>
        public callbackInfo beforeSave;//导入结束（dobulk）执行前，调用回调函数。
        /// <summary>
        /// 在 <see cref="doBulk"/> 全部表写入完成后调用的业务回调。
        /// </summary>
        public callbackInfo afterSave;//导入结束（dobulk）执行结束后，调用回调函数。
        /// <summary>
        /// 与构造函数传入一致的作业令牌。
        /// </summary>
        public string workToken = "";

        //public string outInfoCol;
        //public string mode = "insert";//write/insert/update  即写入模式
        /// <summary>
        /// 多表写入策略：none / solo / together 等，与导入配置一致。
        /// </summary>
        public string multiWritePolicy = "none"; //多表写入策略 none/solo/together,
        //public bool logtips = false;//输出日志时输出提示类的信息，默认关闭。
        //public bool batchUpdate = false;//是否启用批量更新

        /// <summary>
        /// 累积的 HTML 格式日志片段，同步写入缓存供前端轮询展示。
        /// </summary>
        public string info;

        /// <summary>
        /// 发生异常时已处理的数据条数或行序号，用于定位中断位置。
        /// </summary>
        public int exceptionIndex = 0;
        /// <summary>
        /// 最近一次严重错误的简要说明。
        /// </summary>
        public string errInfo = ""; //错误信息
        //public string rowinfo = "";  //行标识信息
        //public string rowLog = ""; //行读写日志
        /// <summary>
        /// 写入阶段的人类可读状态摘要（成功、失败原因等）。
        /// </summary>
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
        /// 内部辅助工具（字符串、列表等常用操作封装）。
        /// </summary>

        private myUntils tool = new myUntils();
        /// <summary>
        /// 日志输出委托
        /// </summary>
        public Action<string, string> onLog;
        #endregion



 

        #region Excel数据读取和分析
        /// <summary>
        /// 校验已经加载到 <see cref="DataTable"/> 中的 Excel 数据与列配置是否一致。
        /// </summary>
        /// <param name="excelDt">已载入的 Excel 数据表。</param>
        /// <returns>列映射与动态列等检查通过为 true，否则为 false。</returns>
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


        /// <summary>
        /// 收集所有「匹配列」配置的列名匹配表达式，用于标题行识别等场景。
        /// </summary>
        /// <returns>非空的 <c>excelCol</c> 匹配串列表。</returns>
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
        /// <summary>
        /// 导入主流程入口：校验 Excel、初始化写库统计并调用 <see cref="ReadDataRows"/>；异常时写日志与进度并结束作业状态。
        /// </summary>
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



        /// <summary>
        /// 由子类实现的业务回调调度，用于 <see cref="beforeSave"/>、<see cref="afterSave"/> 等反射或远程调用场景。
        /// </summary>
        /// <param name="callback">回调描述（模块名、方法名等）。</param>
        /// <param name="para">传递给目标方法的参数数组。</param>
        /// <returns>回调返回的文本信息；默认实现返回空字符串。</returns>
        public virtual string Invoke(callbackInfo callback, Object[] para) {
            return "";
        }

        #region 最终保存
        /// <summary>
        /// 执行导入结果的数据库批量保存：触发各 <see cref="Writelist"/> 表的 <c>save</c>，并串联保存前后钩子与可选回写 Excel。
        /// </summary>
        /// <returns>汇总的人类可读结果消息（插入/更新条数及回调附加文本）。</returns>
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
        /// 在本地备查表或数据库中，根据已格式化的 where 条件获取用于查重的数据行。
        /// </summary>
        /// <param name="tableName">核验表逻辑名（与 <see cref="baseTable"/> 键一致）。</param>
        /// <param name="sqlWhere">where 片段，支持占位符并由值集合格式化。</param>
        /// <returns>匹配行数组；条件非法或表不存在时返回 null。</returns>
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
        /// 将分号分隔的简单条件（如 <c>DbCol=ExcelKey=string</c>）转为可交由值集合格式化的 where 片段。
        /// </summary>
        /// <param name="oldStr">原始条件串，多条以分号分隔。</param>
        /// <param name="tableName">当前列所属核验表名，用于登记核查列。</param>
        /// <param name="srcCols">输出：从条件中解析出的 Excel 侧列键列表。</param>
        /// <returns>拼接后的 where 片段（尚未代入具体值）。</returns>
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
        /// 对查询类型列解析并填充 <see cref="colInfo.formatWhere"/> 及源列列表。
        /// </summary>
        /// <param name="col">列配置对象。</param>
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
        /// 将字符串中的 <c>${列键}</c> 占位符替换为当前行值集合中对应列的值。
        /// </summary>
        /// <param name="freeStr">含 <c>${...}</c> 的模板字符串。</param>
        /// <param name="errCount">输出：解析失败或列缺失时的错误次数。</param>
        /// <returns>替换后的字符串。</returns>
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
        /// <summary>
        /// 将单条日志写入持久化或外部通道；基类为空实现，由子类重写。
        /// </summary>
        /// <param name="type">日志级别（如 fatal、error、important、tip）。</param>
        /// <param name="content">日志 HTML 或纯文本内容。</param>
        public virtual void WriteLog(string type, string content)
        {

        }
        /// <summary>
        /// 获取可直接返回前端的进度与日志摘要；若作业已结束则清理相关缓存。
        /// </summary>
        /// <returns>包含进度文本、日志片段与是否已结束标志的结构。</returns>
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
        /// <summary>
        /// 清除进度、日志与作业状态等缓存键，通常在导入完成后调用。
        /// </summary>
        public void clearCache()
        {
            removeCache("workprogress");
            removeCache("workinfo");
            removeCache("workState");
        }
        /// <summary>
        /// 获取当前导入进度提示文案。
        /// </summary>
        /// <returns>缓存中的进度字符串。</returns>
        public string getProgress()
        {
            return getCacheValue("workprogress"); 
        }
        /// <summary>
        /// 获取累积的导入日志（HTML 片段）。
        /// </summary>
        /// <returns>缓存中的工作日志字符串。</returns>
        public string getWorkLog()
        {
            return getCacheValue("workinfo");
        }
        /// <summary>
        /// 判断导入作业是否已结束（依据缓存中的工作状态）。
        /// </summary>
        /// <returns>已结束或无状态记录时为 true；进行中为 false。</returns>
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
        /// <summary>
        /// 设置作业是否已完成，供前端轮询 <see cref="getIsDone"/> 使用。
        /// </summary>
        /// <param name="isdone">true 表示已完成，false 表示仍在处理。</param>
        public void setWorkState(bool isdone)
        {
            var st = isdone ? "YES" : "NO";
            setCacheValue("workState", st);
        }
        /// <summary>
        /// 更新当前进度提示并写入缓存。
        /// </summary>
        /// <param name="progress">展示给用户的进度文案。</param>
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
