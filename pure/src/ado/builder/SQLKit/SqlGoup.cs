
using mooSQL.auth;
using mooSQL.data.builder;
using mooSQL.utils;
using System;
using System.Collections;
using System.Collections.Generic;

using System.Text;



namespace mooSQL.data
{
    /// <summary>
    /// 一个SQL分组
    /// </summary>
    public class SqlGoup {
        
        /// <summary>
        /// 用于标识的名称
        /// </summary>
        public string key = "";
        /// <summary>
        /// 表名称，用于构建修改语句。
        /// </summary>
        public string tableName = "";
        /// <summary>
        /// 参数集合，用于构建修改语句。
        /// </summary>
        public Paras ps;
        /// <summary>
        /// 数据库连接位，已废弃
        /// </summary>
        public int position;
        private SQLBuilder root;
        /// <summary>
        ///  set碎片的计数，每创建一个+1
        /// </summary>
        private int setFragIndex=0;
        /// <summary>
        /// 数据库类型，用于构建修改语句。
        /// </summary>
        public DataBaseType dbType;
        /// <summary>
        /// 数据库变量字符
        /// </summary>
        public string dbstr
        {
            get {
                return root.expression.paraPrefix;
            }
        }
        /// <summary>
        /// where 条件项的默认连接模式。
        /// </summary>
        public string whereSeprator = "AND";
        /// <summary>
        /// 前置连接符。当union时使用。
        /// </summary>
        public string preConnector = "";
        /// <summary>
        /// 每个字段的修改项
        /// </summary>
        public List<SetFrag> columns { get; set; }
        /// <summary>
        /// 批量写入的字段
        /// </summary>
        public List<int> rows {  get; set; }
        /// <summary>
        /// where项
        /// </summary>
        //public List<WhereFrag> conditions = new List<WhereFrag>();

        public WhereCollection wherePart ;
        /// <summary>
        /// select 部分，用于构建select语句。
        /// </summary>
        public List<string> selectPart { get; set; }
        /// <summary>
        /// from 部分，用于构建from语句。
        /// </summary>
        public List<string> fromPart { get; set; }


        /// <summary>
        /// 前置SQL
        /// </summary>
        public string prefixSQL=string.Empty;
        /// <summary>
        /// 后置SQL
        /// </summary>
        public string suffixSQL=string.Empty;   
        /// <summary>
        /// 行转列的配置
        /// </summary>
        public List<PivotItem> pivotPart = null;
        /// <summary>
        /// 列转行的配置
        /// </summary>
        public List<UnpivotItem> unpivotPart = null;
        /// <summary>
        /// 设置行号字段时的别名。默认是 oonum。
        /// </summary>
        public string numField = "oonum";
        /// <summary>
        /// 设置行号字段时的行号名称。
        /// </summary>
        public string rowNumberFieldName;
        /// <summary>
        /// 行号字段的排序依据
        /// </summary>
        public string rowNumberOrderBy;
        /// <summary>
        /// order by 部分
        /// </summary>
        public List<string> orderPart {  get; set; }
        /// <summary>
        /// group by 部分
        /// </summary>
        public List<string> groupbyPart {  get; set; }
        /// <summary>
        /// having 部分，用于构建查询语句。
        /// </summary>
        public string havingPart = "";
        /// <summary>
        /// 分页大小，默认为10。
        /// </summary>
        public int pageSize = 10;
        /// <summary>
        /// 当前页码，默认为-1。小于0时查询全部数据。大于等于0时按分页查询。
        /// </summary>
        public int pageNum = -1;//如果页面小于0，查询全部
        private bool numSeted = false;
        private bool distincted = false;//是否含有distinct
        private string insertSQL = "";
        private string updateSQL = "";
        private string selectSQL = "";
        private string deleteSQL = "";
        private string preWhere = "";

        /// <summary>
        /// 用于 mergeinto 的 on 部分
        /// </summary>
        private string onInner = "";
        /// <summary>
        /// 用于 mergeinto 是否执行删除
        /// </summary>
        private bool mergeDeletable = false;

        private string mergeAsName = "";
        /// <summary>
        /// 批量插入的指针，每次调用 newRow 增长。
        /// </summary>
        private int setIndex = 0;
        /// <summary>
        /// 多行写入时的行指针
        /// </summary>
        public int RowIndex
        {
            get { return setIndex;}
        }
        /**
         * 查询的数量
         */
        public int toped = -1;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="key"></param>
        /// <param name="kit"></param>
        public SqlGoup(string Name,string key,SQLBuilder kit) {
            //this.tableName = Name;
            this.key = key;
            this.root = kit;
            var wherePrefix = string.Format("{0}wh_{1}_", root.paraSeed, this.key);
            this.wherePart = new WhereCollection(kit,wherePrefix);
            init();
        }

        private void init() {
            columns = new List<SetFrag>();
            rows = new List<int>();
            selectPart = new List<string>();
            fromPart = new List<string>();
            orderPart = new List<string>();
            groupbyPart = new List<string>();
            pivotPart = new List<PivotItem>();
            unpivotPart = new List<UnpivotItem>();
        }
        /// <summary>
        /// 清理配置
        /// </summary>
        /// <returns></returns>
        public SqlGoup clear()
        {
            this.ps.Clear();
            //重设 select 条件池
            this.columns.Clear();
            this.wherePart.Clear();
            this.selectPart.Clear();
            this.fromPart.Clear();
            this.groupbyPart.Clear();
            this.orderPart.Clear();
            this.pivotPart.Clear();
            this.unpivotPart.Clear();
            this.havingPart = "";
            this.numField = "oonum";
            //重设批插入的标识。
            this.setIndex = 0;
            this.setFragIndex = 0;
            this.rows.Clear();

            this.pageSize = 10;
            this.pageNum = -1;//如果页面小于0，查询全部
            this.numSeted = false;
            this.tableName = "";
            this.toped = -1;
            return this;
        }
        /// <summary>
        /// 生成一个新的参数前缀
        /// </summary>
        /// <returns></returns>
        public string getMyPrefixKey() { 
            var tar= string.Format("{0}wh_{1}_g{2}_", root.paraSeed, this.key,this._whfragIndex);
            _whfragIndex++;
            return tar;
        }
        /// <summary>
        /// 添加参数化
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public string addPara(string key, Object val) {
           string name = dbstr + key;
            addParaKV(key, val);
            return name;
        }

        private void addParaKV(string key, Object val) {
            if (ps == null) {
                ps = new Paras();
            }
            ps.AddByPrefix(key, val,dbstr);
        }
        /// <summary>
        /// 添加参数化列表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public List<string> addPara<T>(IEnumerable<T> list, string prefix)
        {
            if (ps == null)
            {
                ps = new Paras();
            }

            List<string> names = new List<string>();
            int i = 0;
            foreach (T li in list)
            {
                string name = prefix + i;
                names.Add(dbstr + name);
                ps.AddByPrefix(name, li, dbstr);
                i++;
            }
            return names;
        }
        /// <summary>
        /// 添加参数化列表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public List<string> addListPara<T>(IEnumerable<T> list,string prefix) {
            if (ps == null) {
                ps = new Paras();
            }

            List<string> names = new List<string>();

            int i = 0;
            foreach (T item in list)
            {
                string name = prefix + i;
                names.Add(dbstr + name);
                ps.AddByPrefix(name, item, dbstr);
                i++;
            }
            return names;
        }
        public List<string> addListPara(IEnumerable list, string prefix)
        {
            if (ps == null)
            {
                ps = new Paras();
            }

            List<string> names = new List<string>();

            int i = 0;
            foreach (var item in list)
            {
                string name = prefix + i;
                names.Add(dbstr + name);
                ps.AddByPrefix(name, item, dbstr);
                i++;
            }
            return names;
        }

        /// <summary>
        /// 添加一个新的插入行，以继续下一组set方法
        /// </summary>
        /// <returns></returns>
        public SqlGoup newRow() {
            //if (this.columns.Count > 0) {
                this.setIndex++;
                this.rows.Add(this.setIndex);
            //}
           
            return this;
        }
        /// <summary>
        /// 执行一行数据添加的保存，实际上，不需要此操作。
        /// </summary>
        /// <returns></returns>
        public SqlGoup addRow()
        {
            //执行一行数据添加的保存，实际上，不需要此操作。
            return this;
        }


        public SqlGoup top(int num) {
            this.toped = num;
            return this;
        }

        public SetFrag getSetFrag(string key,bool autoAdd=true) {
            for (int i = 0; i < columns.Count; i++)
            {
                if (columns[i].key == (key))
                {
                    return columns[i];
                }
            }
            if (!autoAdd) return null;
            var t= new SetFrag(key);
            t.paraPrefix = root.paraSeed + "cl_" + this.key + "_";
            t.fieldIndex = setFragIndex;
            setFragIndex++;
            return t;
        }

        

        private void addColumnNoRepeat(SetFrag field) {
            int i = this.getFieldByKey(field.key);
            if (i == -1) {
                this.columns.Add(field);
            }
            else {
                this.columns[i]= field;
            }
        }
        /// <summary>
        /// 获取一个列的set配置，不存在时新建
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public int getFieldByKey(string key) {
            for (int i = 0; i < columns.Count; i++) {
                if (columns[i].key==(key)) {
                    return i;
                }
            }
            return -1;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public SqlGoup set(SetFrag field) {
            if (field == null) return this;
            var pair = field.values[setIndex];
            if (pair.paramed && (pair.paramKey == null || pair.paramKey ==("")))
            {//参数cl_0__106487_49:
                pair.paramKey = root.paraSeed + "cl_" + this.key + "_" + field.fieldIndex+"_"+setIndex;
            }

            this.addColumnNoRepeat(field);
            return this;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Obsolete("已弃用，改为sink/rise")]

        public SqlGoup and() {
            wherePart.and();
            return this;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Obsolete("已弃用，改为sink/rise")]
        public SqlGoup or()
        {
            wherePart.or();
            return this;
        }
        /// <summary>
        /// 直接添加一个自行定义的where,最高自由度
        /// </summary>
        /// <param name="frag"></param>
        /// <returns></returns>
        public SqlGoup where(WhereFrag frag) {
            frag.root = this.root;
            wherePart.addFrag(frag);
            return this;
        }
        /// <summary>
        /// where 条件计数
        /// </summary>
        private int _whfragIndex=0;


        public SqlGoup where(string key, Object val,string op,bool  paramed,Type type=null) {

            WhereFrag field = new WhereFrag();
            field.key = key;
            field.value = val;
            field.paramed = paramed;
            field.op = op;

            where(field);
            return this;
        }


        public SqlGoup whereFormat(string template,params Object[] values) {
           string key = template;
            for (int i = 0; i < values.Length; i++) {
                string reg = "{" + i + "}";
                var v=values[i];
                if (v == null)
                {
                    key = key.Replace(reg, " null ");
                }
                else {
                    string ke = this.getMyPrefixKey() + "wf_" + ps.Count + "_" + i;
                    
                    key = key.Replace(reg, dbstr + ke);
                    addParaKV(ke, v);
                }
                
            }
            where(key, null, "", false);
            return this;
        }


        /// <summary>
        /// where in 的子查询模式
        /// </summary>
        /// <param name="key"></param>
        /// <param name="doselect"></param>
        /// <returns></returns>
        public SqlGoup whereIn(string key, Action<SQLBuilder> doselect)
        {
            return where(key, " in ", doselect);
        }

        public SqlGoup whereExist(string value)
        {
            return where("", " (" + value + ") ", " exists ", false);
        }
        /// <summary>
        /// where exist 的子查询模式
        /// </summary>
        /// <param name="key"></param>
        /// <param name="doselect"></param>
        /// <returns></returns>
        public SqlGoup whereExist( Action<SQLBuilder> doselect)
        {
            return where("", " exists ", doselect);
        }
        /// <summary>
        /// 获取一个除了配置项空白之外，作用区都相同的兄弟实例
        /// </summary>
        /// <returns></returns>

        /// <summary>
        /// 使用一个子项 SQLBuilder来创建一个 select语句，构建作为条件项。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="op"></param>
        /// <param name="doselect"></param>
        /// <returns></returns>
        public SqlGoup where(string key,string op, Action<SQLBuilder> doselect)
        {

            WhereFrag field = new WhereFrag();
            field.setConnect(whereSeprator);
            field.key = key;
            var builder = root.getBrotherBuilder();
            doselect(builder);
            field.value = " ("+ builder.toSelect().sql+") ";
            this.ps = builder.ps;
            field.paramed = false;
            field.op = op;

            where(field);
            return this;
        }
        /// <summary>
        /// 使用一个子项 SQLBuilder来创建一个 where条件，构建作为条件项，自动括号包裹，该子项仅where方法生效。
        /// </summary>
        /// <param name="doWhere"></param>
        /// <returns></returns>
        public SqlGoup where( Action<SQLBuilder> doWhere)
        {

            WhereFrag field = new WhereFrag();
            field.setConnect(whereSeprator);
            field.key = "";
            field.op = "";

            var builder = root.getBrotherBuilder();
            doWhere(builder);
            field.value = " (" + builder.buildWhereContent() + ") ";
            this.ps = builder.ps;
            field.paramed = false;

            where(field);
            return this;
        }
        /**
         * 包含 where关键字本身
         * @return
         */
        public string buildWhere() {
            if (wherePart.Count == 0) return "";
           string conditon = " WHERE " + this.buildWhereContent();
            this.preWhere = conditon;
            return conditon;
        }
        /// <summary>
        /// 编织 select SQL的内容体
        /// </summary>
        /// <returns></returns>
        public string buildSelectConent() { 
            return joinList(selectPart,"," );
        }
        /// <summary>
        /// 编织 where SQL的内容体
        /// </summary>
        /// <returns></returns>
        public string buildWhereContent() {
            if (wherePart.Count == 0) return "";
            if (this.ps == null) { 
                ps= new Paras();
            }
            string conditon = wherePart.Build();

            return conditon;
        }

        /**
         * 忽略 参数信息，创建一份开始不是where的纯条件字符串，用于在别处复用。
         */
        public string buildWhereNoPara() {
            return wherePart.BuildWithoutPara(dbstr);
        }

        public string buildInsertFields() {
            var cols = new List<string>();
            foreach (SetFrag field in columns)
            {
                cols.Add(field.key);
            }
            return  string.Join(",", cols);
        }
        public string buildInsertValOne() { 
            return this.buildInsertRowVal(setIndex);
        }
        public string buildInsertRowVal(int rowIndex) {
            var valueRow = new List<string>();
            foreach (SetFrag field in columns)
            {
                var pair = field.values[rowIndex];
                if (pair.insetable == false)
                {
                    continue;
                }
                if (pair.paramed)
                {
                    valueRow.Add(dbstr + pair.paramKey);
                    addParaKV(pair.paramKey, pair.value);
                }
                else
                {
                    var val = pair.value.ToString();
                    if (string.IsNullOrEmpty(val))
                    {
                        val = "NULL";
                    }
                    valueRow.Add(val);
                }
            }
            if (valueRow.Count == 0) { 
                return null;
            }
            //存入单行写入值
            return string.Join(",", valueRow);
        }

        /// <summary>
        /// 创建插入语句，自动根据上下文推断创建的语句类别
        /// </summary>
        /// <returns></returns>
        public string buildInsert() {
            var frag = new FragSQL();
            frag.insertInto = tableName;

            
            var cols= new List<string>();
            foreach (SetFrag field in columns)
            {
                cols.Add(field.key);
            }
            frag.insertCols = buildInsertFields();
            if (this.rows.Count > 0)
            {
                var valus = new List<string>();
                foreach (var index in rows)
                {
                    var valueRow = new List<string>();
                    var valsql= buildInsertRowVal(index);

                    if (valsql != null) { 
                        //存入本行的值
                        valus.Add(valsql);                    
                    }

                }
                //放入 frag
                frag.insertValues = valus;
            }
            else {
                //此时为单行插入，
                var valueRow = this.buildInsertRowVal(setIndex);
                //存入单行写入值
                frag.insertValue= valueRow;

                //调用 方言构建语句
                frag.fromInner = joinList(fromPart, ",");
                frag.whereInner = buildWhereContent();
                frag.selectInner = this.buildSelectConent();
                frag.distincted = distincted;
                if (this.groupbyPart.Count > 0)
                {
                    frag.groupByInner = joinList(groupbyPart, ",");

                    if (havingPart != null && havingPart.Trim().Length > 0)
                    {
                        frag.havingInner =  havingPart;
                    }
                }
            }


            this.insertSQL = checkPreSubfix( root.expression.buildInsert(frag));
            fireSQLEvent(insertSQL);
            return insertSQL;
        }

        //insert into gpm_groupmenu
        //()
        //select *
        //from gpm_groupmenu m
        //where m.FK_Menu=''

        public string buildInsertFrom() {
            var insertSQL = this.buildInsert();
            return insertSQL;
        }

        /// <summary>
        /// 不包含 set本身
        /// </summary>
        /// <returns></returns>

        public string buildSetPart() {
            StringBuilder sql = new StringBuilder();
            //sql.Append(" SET ");

           string values = "";
            foreach (var field in  columns) {

                if (values !="" ) {
                    values = values + ",";
                }
                var pair = field.values[setIndex];
                if (!pair.updatable) {
                    continue;
                }
                if (pair.paramed) {
                    values = values + field.key + "=" + dbstr + pair.paramKey;
                    addParaKV(pair.paramKey, pair.value);
                }
                else {
                    values = values + field.key + "=" + pair.value;
                }

            }
            sql.Append(values);
            sql.Append(" ");
            return sql.ToString();
        }
        public List<FragSetPart> buildSetFrag(int index)
        {
            var res= new List<FragSetPart>();
            foreach (var field in columns)
            {

                var pair = field.values[index];
                if (!pair.updatable)
                {
                    continue;
                }
                var setPair = new FragSetPart();
                setPair.field = field.key;
                if (pair.paramed)
                {
                    setPair.value = dbstr + pair.paramKey;
                    addParaKV(pair.paramKey, pair.value);
                }
                else if (pair.value is string str)
                {
                    setPair.value = str;
                }
                else { 
                    setPair.value = pair.value.ToString();
                }
                res.Add(setPair);
            }
            return res;
        }
        public List<FragSetPart> buildSetFragCur() { 
            return buildSetFrag(setIndex);
        }
        public string buildUpdate() {
            if (wherePart.Count == 0 || columns.Count==0) {
                return null;
            }
            var frag = new FragSQL();
            frag.updateTo = this.tableName;
            //frag.setInner = this.buildSetPart();
            frag.setPart = buildSetFragCur();
            frag.whereInner= this.buildWhereContent();
            if (this.fromPart.Count > 0) { 
                frag.fromInner = this.buildFromContent();
            }
            var sql= root.expression.buildUpdate(frag);
            sql = checkPreSubfix(sql);
            fireSQLEvent(sql);
            return sql;
        }

        public string buildUpdateFrom() {
            if (wherePart.Count == 0) {
                return null;
            }
            var frag = new FragSQL();
            frag.updateTo = this.tableName;
            //frag.setInner = this.buildSetPart();
            frag.setPart= buildSetFragCur();
            frag.whereInner = this.buildWhereContent();
            if (this.fromPart.Count > 0)
            {
                frag.fromInner = this.buildFromContent();
            }
            var sql= root.expression.buildUpdateFrom(frag);
            fireSQLEvent(sql);
            return sql;
        }

        /// <summary>
        /// 编织删除SQL
        /// </summary>
        /// <returns></returns>
        public string buildDelete() {
            if (wherePart.Count == 0) {
                return null;
            }
            var frag = new FragSQL();
            frag.deleteTar = this.tableName;
            frag.whereInner = this.buildWhereContent();
            if (this.fromPart.Count > 0)
            {
                frag.fromInner = this.buildFromContent();
            }
            deleteSQL = root.expression.buildDelete(frag);
            deleteSQL = checkPreSubfix(deleteSQL);
            fireSQLEvent(deleteSQL);
            return deleteSQL;
        }

        #region merge into 
        /// <summary>
        /// 将来源的from 部分 嵌套一层的 as 名称
        /// </summary>
        /// <param name="asName"></param>
        /// <returns></returns>
        public SqlGoup mergeAs(string asName)
        {
            this.mergeAsName = asName;
            return this;
        }
        /// <summary>
        /// merge into 语句的on 部分
        /// </summary>
        /// <param name="onPart"></param>
        /// <returns></returns>
        public SqlGoup mergeOn(string onPart) { 
            this.onInner = onPart;
            return this;
        }

        public SqlGoup mergeDelete(bool thenDelete)
        {
            this.mergeDeletable = thenDelete;
            return this;
        }
        /// <summary>
        /// merge into 语句，注意 MYSQL不支持。使用 from内容作为 using 内容，使用 mergeOn 内容作为 on 的内容，列由 set 配置，目标为 setTable
        /// </summary>
        /// <param name="src"></param>
        /// <param name="onPart"></param>
        /// <returns></returns>

        public string buildMerge() {
            int updateCount = 0;
            int insertCount = 0;
            foreach (var field in columns) {
                if (field.insetable) {
                    insertCount++;
                }
                if (field.updatable) {
                    updateCount++;
                }
            }
            if (updateCount == 0 && insertCount == 0 && mergeDeletable==false) return "";

            var mg= this.root.mergeInto(tableName);

            mg.on(this.onInner);
            if (this.selectPart.Count == 0 && wherePart.Count == 0)
            {
                mg.usingTable = this.buildFromContent();
            }
            else
            {
                mg.usingTable = this.buildSelect();
                
            }
            mg.usingAlias = this.mergeAsName;
            if (this.mergeDeletable) {
                mg.whenMatchThenDelete();
            }

            
            if (updateCount > 0)
            {
                var up = mg.when().whenMatched();
                foreach (var field in  columns) {
                    var pair = field.values[setIndex];
                    if (field.updatable) {
                        up.SetPart.current.set(field);
                    }
                }
            }
            if (insertCount > 0)
            {
                var add= mg.when().whenNotMatched();
                
                foreach (var field in columns) {
                    var pair = field.values[setIndex];
                    if (field.insetable) {
                        add.SetPart.current.set(field);
                    }
                }

            }

            var res= mg.buildMergeInto();
            fireSQLEvent(res);
            return res;
        }
        #endregion


        /// <summary>
        /// 清空表选择部分
        /// </summary>
        /// <returns></returns>
        public SqlGoup clearFrom() {
            this.fromPart.Clear();
            return this;
        }

        /// <summary>
        /// 清理所有信息，除了SQL参数体之外。
        /// </summary>
        /// <returns></returns>
        public SqlGoup clearToNext()
        {
            this.columns.Clear();
            this.wherePart.Clear();
            this.selectPart.Clear();
            this.fromPart.Clear();
            this.groupbyPart.Clear();
            this.orderPart.Clear();
            this.havingPart = "";
            this.numField = "oonum";
            //重设批插入的标识。
            this.setIndex = 0;
            this.rows.Clear();

            this.pageSize = 10;
            this.pageNum = -1;//如果页面小于0，查询全部
            this.numSeted = false;
            this.tableName = "";
            this.toped = -1;
            return this;
        }
        public SqlGoup clearToNext(string tableName) {
            this.clear();
            return setTable(tableName);
        }

        public SqlGoup setTable(string tbName) {
            this.tableName = tbName;
            return this;
        }

        /**
         * 清空 where条件构造器的所有成果。
         * @return
         */
        public SqlGoup clearWhere() {
            this.wherePart.Clear();
            return this;
        }

        /**
         * 重置翻页信息为默认的不翻页。
         * @return
         */
        public SqlGoup clearPage() {
            this.pageSize = 10;
            this.pageNum = -1;//如果页面小于0，查询全部
            this.numSeted = false;
            this.numField = "oonum";
            return this;
        }
        /// <summary>
        /// 在生成的SQL语句前方插入一段SQL
        /// </summary>
        /// <param name="SQLString"></param>
        /// <returns></returns>
        public SqlGoup prefix(string SQLString)
        {
            this.prefixSQL += SQLString;
            return this;
        }

        /// <summary>
        /// 在生成的SQL语句后方，插入一段SQL
        /// </summary>
        /// <param name="SQLString"></param>
        /// <returns></returns>
        public SqlGoup subfix(string SQLString)
        {
            this.suffixSQL= SQLString;
            return this;
        }

        //查询部分
        public SqlGoup select(string columns) {
            this.selectPart.Add(columns);
            return this;
        }

        public SqlGoup distinct() {
            this.distincted = true;
            return this;
        }

        public SqlGoup from(string fromPart) {
            this.fromPart.Add(fromPart);
            return this;
        }
        public SqlGoup fromAppend(string apendString)
        {
            var lastFrom = "";
            if (this.fromPart.Count > 0) {
                lastFrom =this.fromPart[fromPart.Count- 1];
            }
            lastFrom += " " + apendString;
            if (string.IsNullOrWhiteSpace(lastFrom)) { 
                return this;
            }
            if (this.fromPart.Count == 0)
            {
                fromPart.Add(lastFrom);
            }
            else {
                fromPart[fromPart.Count - 1]= lastFrom;
            }
            return this;
        }

        public SqlGoup pivot(PivotItem pivotConfig)
        {
            this.pivotPart.Add(pivotConfig);
            return this;
        }

        public SqlGoup unpivot(UnpivotItem unpivotConfig)
        {
            this.unpivotPart.Add(unpivotConfig);
            return this;
        }

        public SqlGoup groupBy(string groupField) {
            this.groupbyPart.Add(groupField);
            return this;
        }

        public SqlGoup having(string havingStr) {
            this.havingPart = havingStr;
            return this;
        }

        public SqlGoup orderby(string fromPart) {
            this.orderPart.Add(fromPart);
            return this;
        }

        /**
         * 设置翻页排序的依据
         * @return
         */
        public SqlGoup rowNumber() {
            //string sql = "ROW_NUMBER() over () as " + numField;
            //this.selectPart.Add(sql);
            this.numSeted = true;
            return this;
        }
        /// <summary>
        /// 使用一个自行定义的好的序号字段作为翻页依据
        /// </summary>
        /// <param name="numFieldName"></param>
        /// <returns></returns>
        public SqlGoup rowNumberUse(string numFieldName)
        {
            this.numField= numFieldName;
            this.numSeted = true;
            return this;
        }
        /// <summary>
        /// 设置翻页行号字段
        /// </summary>
        /// <param name="orderPart"></param>
        /// <returns></returns>
        public SqlGoup rowNumber(string orderPart) {
            //string sql = "ROW_NUMBER() over (order by " + orderPart + ") as " + numField;
            //this.selectPart.Add(sql);
            //this.rowNumberFieldName = "";
            this.rowNumberOrderBy = orderPart;
            this.numSeted = true;
            return this;
        }
        /// <summary>
        /// 设置翻页序号列
        /// </summary>
        /// <param name="asName"></param>
        /// <param name="orderPart"></param>
        /// <returns></returns>
        public SqlGoup rowNumber(string asName,string orderPart) {
            this.numField = asName;
            this.rowNumberFieldName = asName;
            this.rowNumberOrderBy= orderPart;
            //string sql = "ROW_NUMBER() over (order by " + orderPart + ") as " + numField;
            //this.selectPart.Add(sql);
            this.numSeted = true;
            return this;
        }

        public SqlGoup setPage(int size, int num) {
            this.pageNum = num;
            this.pageSize = size;
            return this;
        }
        /// <summary>
        /// 调用SQL创建成功的切面事件。
        /// </summary>
        /// <param name="SQL"></param>
        private void fireSQLEvent(string SQL) {
            if (root.Client != null) {
                root.Client.fireCreatedSQL(SQL, root);
            }
        }

        /// <summary>
        /// 创建 计数SQL
        /// </summary>
        /// <returns></returns>
        public string buildCountSQL() {
            var frag = this.buildSelectFragNoPage(false);
            var res= root.expression.buildSelectCount(frag);
            fireSQLEvent(res);
            return res;
        }

        public string buildOrderBy() {
            if (this.orderPart.Count == 0) return "";
            return " order by " + joinList(this.orderPart, ",");
        }
        public string buildOrderByConent() {
            if (this.orderPart.Count == 0) return "";
            return joinList(this.orderPart, ",");
        }
        /**
         * 包含from 关键字本身
         * @return
         */
        public string buildFromPart() {
            //未设置from部分时，直接使用表名
            return " from " + buildFromContent();
        }

        /**
         * 不包含from关键字
         * @return
         */
        public string buildFromContent() {
            //未设置from部分时，直接使用表名
           string res = "";
            if (this.fromPart.Count == 0) {
                res += tableName;
            }
            else {
                res += joinList(fromPart, ",");
            }
            return res;
        }

        public string buildGroupBy() {
           string res = "";
            if (this.groupbyPart.Count > 0) {
                res += " group by ";
                res += joinList(groupbyPart, ",");

                if (havingPart != null && havingPart.Trim().Length > 0) {
                    res += " having " + havingPart;
                }
            }
            else {
                return res;
            }
            return res;
        }

        private FragSQL buildSelectFragNoPage(bool hasOrder) {
            FragSQL sql = new FragSQL();
            //        if(distincted){
            //            res += " distinct ";
            //        }
            sql.distincted = distincted;
            sql.toped = this.toped;
            sql.pivots = this.pivotPart;
            sql.unpivots = this.unpivotPart;

            bool wrapnum = false;
            string rownumField = "";
            if (this.selectPart.Count == 0)
            {
                //res +="*";
                sql.selectInner = "*";
            }
            else if (this.distincted)
            {
                //当含有 distinct时，需要先移除行号列
                StringBuilder arr = new StringBuilder();
                foreach (string li in selectPart)
                {
                    
                    if (li.Contains("ROW_NUMBER()"))
                    {
                        //直接拼入了序号列，并且也需要distinct。则先把它剔除出来。
                        if (numSeted == false) { 
                            numSeted = true;
                        rownumField = li;
                        }
                        //wrapnum = true;
                        
                        continue;
                    }
                    if (arr.Length > 0)
                    {
                        arr.Append(",");
                    }
                    arr.Append(li);
                }
                sql.selectInner = arr.ToString();
                //res += arr.ToString();
            }
            else
            {
                sql.selectInner = this.joinList(this.selectPart, ",");
                //res += this.joinList(this.selectPart,",");
            }

            //未设置from部分时，直接使用表名
            //res +=this.buildFromPart();
            sql.fromInner = this.buildFromContent();
            //检查where部分
            //       stringwh=this.buildWhere();
            //        if(wh!=null && wh.length()>0){
            //            res += " "+wh;
            //        }
            sql.whereInner = this.buildWhereContent();
            if (this.groupbyPart.Count > 0)
            {
                sql.groupByInner = joinList(groupbyPart, ",");

                if (havingPart.Trim().Length > 0)
                {
                    sql.havingInner = havingPart;
                }
            }
            if (hasOrder)
            {
                sql.orderbyInner =this.buildOrderByConent();
            }
            return sql;
        }

        public string buildSelectNoPage(bool  hasOrder) {
            //String res= "select ";

            //String groupPart=this.buildGroupBy();
            //        if(groupPart.length()>0){
            //            res +=" "+groupPart;
            //        }
            
            //检查是否需要再包裹一层，当含义序号列且含义distinct时。
            var frag = this.buildSelectFragNoPage(hasOrder);
            //如果在非翻页的情况下设置了行号，需要再纳入到普通的查询里。
            //设置行号，但未设置别名时，加上默认的别名
            if (this.numSeted && string.IsNullOrWhiteSpace(this.rowNumberFieldName))
            {
                rowNumberFieldName = this.numField;
            }
            //补上翻页参数

            frag.rowNumberFieldName = this.rowNumberFieldName;
            frag.rowNumberOrderBy = this.rowNumberOrderBy;
            frag.hasRowNumber = this.numSeted;
            string res = root.expression.buildSelect(frag);
            /*
            if (wrapnum) {
                res = "select *," + rownumField + " from (" + res + ") ditnumwrap";
            }*/
            return res;
        }

        /// <summary>
        /// 创建带翻页的SQL语句
        /// </summary>
        /// <returns></returns>
        public string buildSelectPaged() {
            //如果没有设置排序列，则自动设置
            var frag = this.buildSelectFragNoPage(true);
            //设置行号，但未设置别名时，加上默认的别名
            if (this.numSeted && string.IsNullOrWhiteSpace(this.rowNumberFieldName)) {
                rowNumberFieldName = this.numField;
            }
            //补上翻页参数
            frag.pageSize = this.pageSize;
            frag.pageNum = this.pageNum;
            frag.rowNumberFieldName = this.rowNumberFieldName;
            frag.rowNumberOrderBy= this.rowNumberOrderBy;
            frag.hasRowNumber = this.numSeted;

            var cksql= root.expression.buildPagedSelect(frag);

            /*
            if (!this.numSeted) {
                //this.rowNumber();
            }
           string cksql = this.buildSelectNoPage(false);
            cksql = root.expression.wrapPaged(numField, cksql, this.pageSize, this.pageNum, buildOrderByConent());

            */
            return cksql;
        }

        private string checkPreSubfix(string sql) {

            if (!string.IsNullOrWhiteSpace(prefixSQL))
            {
                sql = prefixSQL+" " +sql;
            }
            if (!string.IsNullOrWhiteSpace(suffixSQL))
            {
                sql = sql+" "+ suffixSQL;
            }
            return sql;
        }
        /// <summary>
        /// 创建Select SQL语句
        /// </summary>
        /// <returns></returns>
        public string buildSelect() {
            //此时不分页，直接查询
            string sql = "";

            if (this.pageNum <= 0) {
                sql = this.buildSelectNoPage(true);
            }
            else {
                //执行分页查询
                sql = this.buildSelectPaged();
            }


            selectSQL = checkPreSubfix( sql);
            fireSQLEvent(selectSQL);
            return selectSQL;
        }


        public string joinList(List<string> list,string seperator) {
            StringBuilder res = new StringBuilder();
            foreach (string li in list) {
                if (string.IsNullOrWhiteSpace(li)) {
                    continue;
                }
                if (res.Length > 0) {
                    res.Append(seperator);
                }
                res.Append(li);
            }
            return res.ToString();
        }

        public SqlGoup copyList<T>(List<T> src, List<T> tar) {
            foreach (T t in src) {
                if (tar.Contains(t) == false) { 
                    tar.Add(t);
                }
            }
            return this;
        }

        public SqlGoup copySelect(SqlGoup src) {
            this.copyList(selectPart, src.selectPart);
            return this;
        }

        public SqlGoup copyFrom(SqlGoup src) {
            this.copyList(fromPart, src.fromPart);
            return this;
        }

        public SqlGoup copyWhere(SqlGoup src) {
            this.wherePart.Copy( src.wherePart);
            return this;
        }
    }
}