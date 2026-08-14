using System;
using System.Collections.Generic;
using System.Text;
using System.Data;


using mooSQL.excel.context;


namespace mooSQL.excel
{
    /// <summary>
    /// Excel 导入的全局选项与生命周期钩子（与 <see cref="ExcelRead"/> 绑定，可由 <see cref="InConfig"/> 填充）。
    /// </summary>
    public partial class ImportOption
    {
        /*
         * 主要提供对导入参数的服务端配置的功能
         * 更新 20210930 增加导入正则匹配忽视大小写的配置
         * 更新 20210808 增加导入信息输出的列配置。
         * 更新 20200107 常规导入格式兼容通过，服务端基本配置通过，已添加服务端配置的助手方法。
         * 更新 20200103 初步完成基本功能改造 
        */
        /// <summary>
        /// 绑定到指定的导入执行对象。
        /// </summary>
        /// <param name="wk">导入工作类实例。</param>
        public ImportOption(ExcelRead wk)
        {
            this.workor = wk;
        }
        /// <summary>当前导入任务宿主。</summary>
        public ExcelRead workor;
        /// <summary>最近一次 <see cref="readInfo"/> 读入的原始配置。</summary>
        public InConfig param;
        /// <summary>
        /// 数据查询模式，默认为local:local/database。一般不需要修改。 分为local内存查询和database数据库查询。内存查询优点是速度快，缺点是准备时间长，数据量越大越明显。数据库查询优点是无前摇，但执行写入速度慢。
        /// </summary>
        public string checkMode = "local";
        /// <summary>
        /// 输出日志时输出[提示]类的信息，默认NO:YES/NO。因提示性的涉及的太多，提示信息过多会导致用户难以找到关键性的信息。
        /// </summary>
        public bool logtips = false;
        /// <summary>
        /// 是否启用批量更新,启用时，将使用StrSQLMaker类的UpdateTable进行批量提交，否则，使用dealUpdate方法生成的SQL逐行更新
        /// </summary>
        public bool batchUpdate = false;
        /// <summary>
        /// 标题，导入日志的数据库表中，将以它作为标题列的值。
        /// </summary>
        public string title = "未命名导入";
        /// <summary>
        /// 必填，其值为列集合中的某一列的key。行日志下的列标识，输出列信息
        /// </summary>
        public string outInfoCol = string.Empty;
        /// <summary>
        /// 默认insert:write/insert/update。分别指代执行同时更新和插入，只插入，只更新。
        /// </summary>
        public writeMode mode = writeMode.insert;

        /// <summary>是否在导入前按 <see cref="titleScanScope"/> 等自动扫描表头。</summary>
        public bool scanTitle = false;
        /// <summary>
        /// 标题行自动侦测的范围，默认只扫描前10行。
        /// </summary>
        public string titleScanScope = "1-10";
        /// <summary>
        /// 标题行扫描使用的正则。默认应该是match类必填列的excelCol集合。
        /// </summary>
        public List<string> titleScanReg= new List<string>();
        /// <summary>
        /// 默认为 1 ，格式为数字。导入表格的标题所在的excel行号，其值与excel中所显示的行号相同（从1开始）。多个以逗号分隔，如果"1,2,3,5"。
        /// </summary>
        public string titleRowNum = "1";
        /// <summary>
        /// 默认为 2-，即从第2行开始，支持类似于 3,4,9-15,56-这样的格式，即区间以“-”分隔，其值与excel中所显示的行号
        /// </summary>
        public string dataRowNum = "2-";
        /// <summary>
        /// 导入状态信息的列位置，默认为根据最后一个标题定位。
        /// </summary>
        public string logColNum = "";
        /// <summary>
        /// 是否保存消息到Excel文件中，默认保存。
        /// </summary>
        public bool saveMsgToExcel = true;
        /// <summary>
        /// 自定义行提示，参为工作类实例，行号为工作类的readingExcelRowIndex属性，数据行为readingRow属性
        /// </summary>
        public Func<ExcelRead,string> onLoadRowTip;

        /// <summary>
        /// 使用正则匹配时，是否忽视大小写
        /// </summary>
        public bool ignoreCase = false;
        /// <summary>
        /// 导入模板的地址。
        /// </summary>
        public string demoUrl = string.Empty;
        /// <summary>配置备注说明。</summary>
        public string note = string.Empty;
        /// <summary>待导入的表配置列表。</summary>
        public List<context.Table> tables=new List<context.Table>();
        /// <summary>全局键值型列（不隶属于单表）。</summary>
        public List<Column> KVs= new List<Column>();
        /// <summary>多表共享列定义。</summary>
        public List<Column> shareFields= new List<Column>();
        /// <summary>全部表写入完成前调用的服务端 BPO。</summary>
        public InBPOMethod beforeSave;
        /// <summary>全部表写入完成后调用的服务端 BPO。</summary>
        public InBPOMethod afterSave;
        /*生命周期钩子部分：
         * 读取excel时刻：
         * 检查excel时刻：
         * 写入循环开始前
         * 行读取前
         *     列循环前
         *         表读取前
         *              字段循环前
         *         表写入前
         * 行读取后
         */
        /// <summary>工作簿已打开、尚未解析工作表前；返回 false 可中止导入。</summary>
        public Func<XWorkBook,DataTable,ExcelRead,bool> onBeforeReadExcel { get; set; }
        /// <summary>每个工作表读取前。</summary>
        public Func<XSheet, DataTable, ExcelRead, bool> onBeforeReadSheet { get; set; }
        /// <summary>工作簿全部读取完成后。</summary>
        public Func<XWorkBook, DataTable,ExcelRead, bool> onAfterReadExcel { get;set; }
        /// <summary>进入表数据行循环前。</summary>
        public Func<ExcelRead, bool> onBeforeReadTable { get; set; }
        /// <summary>表数据行循环结束后。</summary>
        public Func<ExcelRead, bool> onAfterReadTable { get; set; }
        /// <summary>
        /// 表行数据写入前时刻。返回false时将停止插入动作。
        /// </summary>
        public Func<WriteTable, bool> onBeforeRowAdd;
        /// <summary>
        /// 表数据的读取时刻，在表循环之前。返回false时整行excel数据不导入。
        /// </summary>
        public Func<ExcelRead,rowInfo,bool> onBeforeParseRow;
        /// <summary>
        /// 数据保存前的处理操作，返回false将停止保存。2参字符串为消息
        /// </summary>
        public Func<ExcelRead, StringBuilder, bool> onBeforeSave;
        /// <summary>
        /// 数据保存结束后的操作，返回的字符串消息，将推送到前端
        /// </summary>
        public Func<ExcelRead, string> onAfterSave;
        /// <summary>
        /// 读取单元格数据的时刻，1参读取到的值，2参cell对象
        /// </summary>
        public Func<object, XCell, object> onLoadCellValue;
        /// <summary>
        /// 读取Excel的行数据前时刻，如果返回false,则不再继续读取。
        /// </summary>
        public Func<DataTable, XRow, int,bool> onBeforeReadExcelRow;
        /// <summary>
        /// 行集匹配结束时刻，可以手动修正匹配结果。
        /// </summary>
        public Action<XWorkBook> onAfterMatchTitle;
        /// <summary>
        /// 从服务端配置一次性加载全局项、表定义、列定义与共享列。
        /// </summary>
        /// <param name="reda">导入配置根对象。</param>
        public void readInfo(InConfig reda)
        {
            this.param = reda;
            readGlobalInfo(reda);
            //读入表定义、列定义、固定键值列
            readTableInfo(reda);
            readColInfo(reda);
            readShareCol(reda);

        }
        /// <summary>解析全局导入参数（校验模式、标题行、写入模式等）。</summary>
        /// <param name="reda">配置根对象。</param>
        public void readGlobalInfo(InConfig reda)
        {
            if (!string.IsNullOrWhiteSpace( reda.checkMode ))
            {
                checkMode = reda.checkMode;
            }
            //是否显示输出提示类日志
            if (!string.IsNullOrWhiteSpace(reda.logtips))
            {
                logtips = reda.logtips=="YES";
            }//是否使用Bulk进行数据的插入
            if (!string.IsNullOrWhiteSpace(reda.batchUpdate))
            {
                batchUpdate = reda.batchUpdate=="YES";
            }
            if(!string.IsNullOrWhiteSpace(reda.outInfoCol))   outInfoCol = reda.outInfoCol;//输出列信息

            if (!string.IsNullOrWhiteSpace(reda.saveMsgToExcel))
            {
                saveMsgToExcel = reda.saveMsgToExcel == "YES";
            }
            //匹配时是否忽视大小写
            if (!string.IsNullOrWhiteSpace(reda.ignoreCase))
            {
                ignoreCase = reda.ignoreCase == "YES";
            }
            if (!string.IsNullOrWhiteSpace(reda.logColNum))
            {
                logColNum= reda.logColNum;
                saveMsgToExcel = true;
            }

            if (!string.IsNullOrWhiteSpace(reda.mode))
            {
                mode =(writeMode) Enum.Parse( typeof(writeMode) ,reda.mode);
            }
            else
            {
                mode = writeMode.insert;
            }
            if (!string.IsNullOrWhiteSpace(reda.title))
            {
                title = reda.title;
            }
            if (!string.IsNullOrWhiteSpace(reda.titleScanScope))
            {
                titleScanScope = reda.titleScanScope;
                scanTitle = true;
            }

            var ttreg = reda.titleScanReg ;
            if (ttreg != null)
            {
                foreach(var ttr in ttreg)
                {
                    titleScanReg.Add(ttr);
                }
                scanTitle = true;
            }

            beforeSave = reda.beforeSave;

            afterSave = reda.afterSave;

            if (reda.titleRowNum != null)
            {
               this.titleRowNum = reda.titleRowNum.ToString();
            }
            if (reda.dataRowNum != null)
            {
                this.dataRowNum = reda.dataRowNum.ToString();
            }
        }
        /// <summary>读取并填充 <see cref="shareFields"/>。</summary>
        /// <param name="reda">配置根对象。</param>
        public void readShareCol(InConfig reda)
        {
            var JkExcol = reda.shareCol ;
            if (JkExcol == null) return;
            foreach (var y in JkExcol)
            {
                var key = y.field;
                var col = new Column(this);
                col.field = key;
                col.readConfig(y );
                //写入所有的写入表中
                shareFields.Add(col);
            }
        }
        /// <summary>读取并填充 <see cref="tables"/>。</summary>
        /// <param name="reda">配置根对象。</param>
        public void readTableInfo(InConfig reda)
        {
            //从传入的参数中，获取表格信息
            var tbs = reda.tables;
            if (tbs == null) return;
            foreach (var x in tbs)
            {
                var Jkname = x.name;
                if (Jkname == null)
                {
                    workor.pushLog("表定义时，表名不得为空！请检查导入配置！", "fatol");
                    continue;
                }
                var tb = new context.Table(this);
                tb.readConfig(x);
                if (tb.mode==writeMode.none)
                {
                    tb.mode = mode;
                }
                tables.Add(tb);
            }
        }
        /// <summary>读取并填充全局 <see cref="KVs"/>。</summary>
        /// <param name="reda">配置根对象。</param>
        public void readColInfo(InConfig reda)
        {
            //从传入的参数中，获取列信息
            var kvSolo = reda.KVs ;
            if (kvSolo == null) return;
            foreach (var tar in kvSolo)
            {
                //根据字段名，依次读入各属性。所有的列，必须包含key属性

                if (tar == null || tar.key == null) continue;
                var key = tar.key;
                var col = new Column(this);
                col.readConfig(tar);
                KVs.Add(col);
            }
        }



 
  
    }
}
