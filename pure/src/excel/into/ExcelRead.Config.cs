// 基础功能说明：

using mooSQL.excel.context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.excel
{
    public abstract partial class ExcelRead
    {
        #region 作业环境解析
        /// <summary>
        /// 读取参数并准备好执行。
        /// </summary>
        /// <param name="config"></param>
        public void readInfo(InConfig config)
        {
            readParams(config);
        }
        /// <summary>
        /// 配置导入参数，但不执行解析。
        /// </summary>
        /// <param name="geneConfig"></param>
        /// <returns></returns>
        public ExcelRead setConfig(Func<InConfig> geneConfig)
        {
            if (context.option == null)
            {
                context.option = new ImportOption(this);
            }
            var config = geneConfig();
            if (string.IsNullOrWhiteSpace(config.dataColNum))
            {
                config.dataColNum = "A-CU"; //默认99列
            }
            context.option.param = config;
            return this;
        }
        /// <summary>
        /// 配置导入参数，并执行参数解析。
        /// </summary>
        /// <param name="doOptionConfig"></param>
        /// <returns></returns>
        public ExcelRead setOption(Action<ImportOption> doOptionConfig)
        {
            if (context.option == null)
            {
                context.option = new ImportOption(this);
            }

            if (this.context.option.param != null) {
                context.option.readInfo(context.option.param);
            }
            doOptionConfig(context.option);

            this.readOption();
            return this;
        }

        /// <summary>
        /// 读取前端传入的导入配置参数
        /// </summary>
        /// <param name="pram"></param>
        public void readParams(InConfig pram)
        {
            if (context.option == null)
            {
                context.option = new ImportOption(this);
            }

            context.option.readInfo(pram);
            var lccb = pram.loadConfig;
            if (lccb != null)
            {

                var bpo = lccb.BPOName;
                var met = lccb.method;
                if (!isValid(bpo) || !isValid(met))
                {
                    this.pushLog("定义保存前回调时，回调的页面名BPOName和方法名method均不得为空！请检查导入配置！", "fatol");
                }
                else
                {
                    this.onReadConfig = new callbackInfo(bpo, met);
                    try
                    {
                        var pams = new object[] { context.option };
                        string cbmsg = this.Invoke(onReadConfig, pams);
                    }
                    catch (Exception e)
                    {
                        pushLog(string.Format("配置参数解析的业务处理逻辑调用失败！请核查模块{0}下的方法{1}<br/>", onReadConfig.BPOName, onReadConfig.Method), "important");
                    }
                }
            }
            this.readOption();
        }
        /// <summary>
        /// 读入配置信息
        /// </summary>
        public void readOption()
        {
            if (context.option == null) return;
            readGlobalOption();
            //读入表定义、列定义、固定键值列
            readTableOption(context.option.tables);
            readColOption(context.option.KVs);
            readShareCol();
            string tbstring = "";
            //读取完之后，如果表内定义的字段，不是引用的列集合的值，则给与创建信息到列集合中，在列集合中不存在
            foreach (var x in Writelist)
            {
                tbstring += x.Value.option.name;
                var tb = x.Value;
                foreach (var wkv in tb.writeCols)
                {
                    var wcol = wkv.Value;
                    if (wcol.dataType == null || wcol.colType == null)
                    {
                        var res = wcol.setColTypeByDB(tb.bulk.bulkTarget);
                        if (!res) { wcol.colType = valueType.stringi; }
                    }
                    context.valueCollection.addCol(wcol.ID, wcol);
                    //mapAdd(colMap, wcol.ID, wcol);
                }
            }
            tablena = tbstring;
            //格式化所有查询列的查询条件字符串
            foreach (var kv in context.colMap)
            {
                var col = kv.Value;
                if (col.type == columnType.select)
                {
                    formatColWherePart(col);
                    addCheckCol(col.selectTable, col.selectCol);
                }
                else if (col.type == columnType.fix)
                {
                    col.writeValue = col.value;

                }

                if (isValid(col.codeTable))
                {
                    addCodeTable(col.codeTable);
                }
            }
        }
        /// <summary>
        /// 读取全局配置信息
        /// </summary>
        public void readGlobalOption()
        {

            var bscb = context.option.beforeSave;
            if (bscb != null)
            {
                var bpo = bscb.BPOName;
                var met = bscb.method;
                if (!isValid(bpo) || !isValid(met))
                {
                    this.pushLog("定义保存前回调时，回调的页面名BPOName和方法名method均不得为空！请检查导入配置！", "fatol");
                }
                else
                {
                    this.beforeSave = new callbackInfo(bpo, met);
                }
            }
            var ascb = context.option.afterSave;
            if (ascb != null)
            {
                var bpo = ascb.BPOName;
                var met = ascb.method;
                if (!isValid(bpo) || !isValid(met))
                {
                    this.pushLog("定义保存后回调时，回调的页面名BPOName和方法名method均不得为空！请检查导入配置！", "fatol");
                }
                else
                {
                    this.afterSave = new callbackInfo(bpo, met);
                }
            }
            if (isValid(context.option.titleRowNum))
            {
                var ttn = context.option.titleRowNum;
                excelTitleRow.readConfig(ttn);
            }
            if (isValid(context.option.dataRowNum))
            {
                excelDataRow.readConfig(context.option.dataRowNum);
            }
            if (context.option.scanTitle && isValid(context.option.titleScanScope))
            {
                titlsScanScope.readConfig(context.option.titleScanScope);
            }
        }
        /// <summary>
        /// 从传入的参数中，获取表格信息
        /// </summary>
        /// <param name="tables"></param>
        public void readTableOption(List<context.Table> tables)
        {
            //从传入的参数中，获取表格信息

            int count = 0;
            foreach (var table in tables)
            {
                var Jkname = table.name;
                if (Jkname == null)
                {
                    this.pushLog("表定义时，表名不得为空！请检查导入配置！", "fatol");
                    continue;
                }
                var tb = new WriteTable(table);// tool.AutoCopy<ImportOption.Table,WriteTable>(table);
                tb.root = this;
                tb.checkConfig(this);

                /*根据读取的信息，自动补全或完善*/



                checkTable tar;
                //创建表格基本对象
                if (baseTable.ContainsKey(tb.option.DBName))
                {
                    //已存在，则直接给即可。
                    tar = baseTable[tb.option.DBName];
                }
                else
                {
                    //---
                    tar = new checkTable(tb.option.DBName);
                    tar.root = this;
                    baseTable.AddNotNull( tb.option.DBName, tar);
                }
                tar.option = table;
                tar.readFromConfig(tb, this);
                tb.oldData = tar;
                tb.tableIndex = count;

                //备查信息储存结束，开始检测写入信息。
                if (tb.canInsert || tb.canUpdate)
                {   //如果没传入任何核验信息，将置为查询表
                    if (isValid(tb.option.repeatWhere) == false) this.pushLog("发现导入的目标表" + tb.option.caption + "未设置校验数据信息,请务必设置，否则不予导入！", "important");
                    if (tb.writeCols.Count == 0)
                    {
                        this.pushLog("发现导入的目标表" + tb.option.caption + "未设置要处理的列映射信息【KVs】,请务必定义该属性，否则无法导入！", "important");
                    }
                }
                if (tb.canInsert || tb.canUpdate)
                {

                    if (tb.writeBackInfo != null)
                    {//回写部分

                        foreach (var t in tb.writeBackInfo)
                        {
                            string[] tt = t.Split('=');
                            if (tt.Length > 1) tb.oldData.addCheckCol(tt[1]);
                        }
                    }
                    //处理条件部分
                    if (string.IsNullOrWhiteSpace(tb.option.repeatWhere) == false)
                    {
                        var usedCols = new List<string>();
                        tb.parsedRepeatWhere = this.formatWhereStr(tb.option.repeatWhere, tb.option.DBName, out usedCols);
                    }

                    if (string.IsNullOrWhiteSpace(tb.option.updateWhere) == false)
                    {
                        var usedCols = new List<string>();
                        tb.parsedUpdateWhere = this.formatWhereStr(tb.option.updateWhere, tb.option.DBName, out usedCols);
                    }

                    //存入表格信息映射表
                    Writelist[tb.option.key] = tb;
                }
                count++;
            }
        }
        /// <summary>
        /// 从传入的参数中，获取与列信息
        /// </summary>
        /// <param name="KVs"></param>
        public void readColOption(List<Column> KVs)
        {
            //从传入的参数中，获取列信息
            if (KVs == null) return;
            foreach (var tar in KVs)
            {
                //根据字段名，依次读入各属性。所有的列，必须包含key属性
                if (tar == null || tar.key == null)
                {
                    pushLog("非表下定义列集合时，列的key必须定义，否则将忽视。请检查列【" + tar.caption + "】配置", "tip");
                    continue;
                }

                var col = new colInfo(tar); //tool.AutoCopy<ImportOption.Column,colInfo>(tar);
                col.ID = col.key;
                col.root = this;
                col.chekConfig();

                //自动获取列的数据类型
                if (col.tableName != null && baseTable.ContainsKey(col.tableName))
                {
                    var res = col.setColTypeByDB(baseTable[col.tableName].table);
                    if (res) { col.colType = valueType.stringi; }
                }
                //其他信息，如代码表、列名等处理
                if (isValid(col.tableName))
                {
                    //addcolToDtlist(Writelist, col.tableName, col.key);//添加列名到所属表的列集合
                    addCheckCol(col.tableName, col.key);
                    //添加列信息到写入列集合。
                    Writelist[col.tableName].addCol(col);
                }
                //mapAdd(kvTitle, col.key, col.caption);  //添加列中文名映射

                context.valueCollection.addCol(col.key, col);//存储列映射
            }
        }
        /// <summary>
        /// 读取共享列的配置
        /// </summary>
        public void readShareCol()
        {
            var JkExcol = context.option.shareFields;
            if (JkExcol == null) return;
            foreach (var y in JkExcol)
            {

                var col = new colInfo(y);
                col.root = this;
                //tool.AutoCopy<ImportOption.Column, colInfo>(y,col);

                col.chekConfig();
                //写入所有的写入表中
                foreach (var kv in Writelist)
                {
                    if (kv.Value.option.useShareCol)
                    {
                        kv.Value.oldData.addCheckCol(col.key);//查询列集合
                        kv.Value.addCol(col);
                    }
                }
            }
        }
        #endregion
    }
}