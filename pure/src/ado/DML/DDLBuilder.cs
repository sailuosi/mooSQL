using mooSQL.data.builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// DML语句构造器
    /// </summary>
    public partial class DDLBuilder
    {
        /// <summary>
        /// 数据库实例
        /// </summary>
        public DBInstance DBLive {  get; set; }
        /// <summary>
        /// 创建某个数据库的DDL编制器
        /// </summary>
        /// <param name="DB"></param>
        public DDLBuilder(DBInstance DB) { 
            this.DBLive = DB;
            this.ps= new Paras();
        }
        /// <summary>
        /// 最底层的SQL方言转译官，构建出SQL碎片
        /// </summary>
        public SQLExpression SQLPit
        {
            get {
                return DBLive.dialect.expression;
            }
        }
        /// <summary>
        /// 构造出SQL命令
        /// </summary>
        public SQLSentence SQLVerse
        {
            get
            {
                return DBLive.dialect.sentence;
            }
        }
        /// <summary>
        /// 参数体
        /// </summary>
        public Paras ps { get; set; }
        /// <summary>
        /// 复制表结构
        /// </summary>
        /// <returns></returns>
        public int doCreateTable() {
            var cmd=this.toCreateTable();
            var cc= DBLive.ExeNonQuery(cmd);
            return cc;
        }
        public DDLBuilder clear() {
            if (this.ps != null) { 
                ps.Clear();
            }
            this._targetTable= null;
            this._targetView= null;
            if (this._ddlFields != null) { 
                this._ddlFields.Clear();
            }
            if (this._ddlIndexes != null) { 
                this._ddlIndexes.Clear();
            }
            return this;
        }
        /// <summary>
        /// 构建建表语句
        /// </summary>
        /// <returns></returns>
        public SQLCmd toCreateTable() {
            var frag= this.buildFrag();
            var str = this.DBLive.dialect.expression.buildCreateTable(frag);
            var cmd= new SQLCmd(str,ps);
            return cmd;
        }

        private DDLFragSQL buildFrag() {
            var frag = new DDLFragSQL();
            if (_targetTable == null)
            {
                throw new Exception("数据库表名定义不得为空！");
            }
            frag.Table = this._targetTable;
            frag.Columns = this._ddlFields;
            frag.TableCaption = this._tableCaption;
            return frag;
        }

        /// <summary>
        /// 添加字段
        /// </summary>
        /// <returns></returns>
        public int doAddColumn()
        {
            var cmd = this.toAddColumn();
            var cc = DBLive.ExeNonQuery(cmd);
            return cc;
        }
        /// <summary>
        /// 构造添加字段的SQL
        /// </summary>
        /// <returns></returns>
        public SQLCmd toAddColumn()
        {
            var frag = this.buildFrag();
            var str = this.DBLive.dialect.expression.buildAddColumn(frag);
            var cmd = new SQLCmd(str, ps);
            return cmd;
        }
        /// <summary>
        /// 修改字段
        /// </summary>
        /// <returns></returns>
        public int doAlterColumn() {
            var cmd = this.toAlterColumn();
            var cc = DBLive.ExeNonQuery(cmd);
            return cc;
        }
        /// <summary>
        /// 修改字段SQL
        /// </summary>
        /// <returns></returns>
        public SQLCmd toAlterColumn()
        {
            var frag = this.buildFrag();
            var str = this.DBLive.dialect.expression.buildAlterColumn(frag);
            var cmd = new SQLCmd(str, ps);
            return cmd;
        }
        /// <summary>
        /// 删除字段
        /// </summary>
        /// <returns></returns>
        public int doDropColumn() {
            var cmd = this.toDropColumn();
            var cc = DBLive.ExeNonQuery(cmd);
            return cc;
        }
        /// <summary>
        /// 删除字段SQL创建
        /// </summary>
        /// <returns></returns>
        public SQLCmd toDropColumn()
        {
            var frag = this.buildFrag();
            var str = this.DBLive.dialect.expression.buildDropColumn(frag);
            var cmd = new SQLCmd(str, ps);
            return cmd;
        }

        /// <summary>
        /// 表删除
        /// </summary>
        /// <returns></returns>
        public int doDropTable()
        {
            var cmd = this.toDropTable();
            var cc = DBLive.ExeNonQuery(cmd);
            return cc;
        }
        /// <summary>
        /// 获取表删除语句
        /// </summary>
        /// <returns></returns>
        public SQLCmd toDropTable()
        {
            var frag = this.buildFrag();
            var str = this.DBLive.dialect.expression.buildDropTable(frag);
            var cmd = new SQLCmd(str, ps);
            return cmd;
        }


        private SQLBuilder _selectBuilder;


        /// <summary>
        /// 创建视图
        /// </summary>
        /// <returns></returns>
        public SQLCmd toCreateView()
        {
            var frag = new DDLFragSQL();
            if (_targetTable == null)
            {
                throw new Exception("数据库表名定义不得为空！");
            }
            frag.Table = this._targetTable;
            var sql = this._selectBuilder.toSelect();
            if (sql == null || string.IsNullOrWhiteSpace(sql.sql)) {
                throw new Exception("视图定义的SQL不能为空！");
            }
            frag.SelectSQL = sql.sql;
            var str = this.DBLive.dialect.expression.buildCreateView(frag);
            var cmd = new SQLCmd(str, ps);
            return cmd;
        }
        /// <summary>
        /// 执行视图创建
        /// </summary>
        /// <returns></returns>
        public int doCreateView()
        {
            var cmd = this.toCreateView();
            var cc = DBLive.ExeNonQuery(cmd);
            return cc;
        }

        /// <summary>
        /// 删除视图的SQL获取
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public SQLCmd toDropView() {
            var frag = new DDLFragSQL();
            if (_targetTable == null)
            {
                throw new Exception("数据库表名定义不得为空！");
            }
            var str = this.DBLive.dialect.expression.buildDropView(frag);
            var cmd = new SQLCmd(str, ps);
            return cmd;
        }
        public SQLCmd toDropView(string viewName) { 
            this.setTable(viewName);
            return toDropView();
        }
        /// <summary>
        /// 删除视图
        /// </summary>
        /// <returns></returns>
        public int doDropView()
        {
            var cmd = this.toDropView();
            var cc = DBLive.ExeNonQuery(cmd);
            return cc;
        }
        public int doDropView(string viewName) { 
            this.setTable(viewName);
            return doDropView();
        }
        /// <summary>
        /// 修改视图
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public SQLCmd toAlterView()
        {
            var frag = new DDLFragSQL();
            if (_targetTable == null)
            {
                throw new Exception("数据库表名定义不得为空！");
            }
            frag.Table = this._targetTable;
            var sql = this._selectBuilder.toSelect();
            if (sql == null || string.IsNullOrWhiteSpace(sql.sql))
            {
                throw new Exception("视图定义的SQL不能为空！");
            }
            frag.SelectSQL = sql.sql;
            var str = this.DBLive.dialect.expression.buildAlterView(frag);
            var cmd = new SQLCmd(str, ps);
            return cmd;
        }
        /// <summary>
        /// 修改视图执行
        /// </summary>
        /// <returns></returns>
        public int doAlterView()
        {
            var cmd = this.toAlterView();
            var cc = DBLive.ExeNonQuery(cmd);
            return cc;
        }
        /// <summary>
        /// 创建索引
        /// </summary>
        /// <returns></returns>
        public SQLCmd toCreateIndex() {
            var frag = new DDLFragSQL() { 
                Table=this._targetTable,
                Indexes=this._ddlIndexes
            };
            var sql=DBLive.dialect.expression.buildCreateIndex(frag);
            var cmd=new SQLCmd(sql, ps);
            return cmd;
        }
        /// <summary>
        /// 创建索引
        /// </summary>
        /// <returns></returns>
        public int doCreateIndex()
        {
            var cmd = this.toCreateIndex();
            var cc = DBLive.ExeNonQuery(cmd);
            return cc;
        }
        /// <summary>
        /// 删除索引
        /// </summary>
        /// <returns></returns>
        public SQLCmd toDropIndex() {
            var frag = new DDLFragSQL()
            {
                Table = this._targetTable,
                Indexes = this._ddlIndexes
            };
            var sql = DBLive.dialect.expression.buildCreateIndex(frag);
            var cmd = new SQLCmd(sql, ps);
            return cmd;
        }
        /// <summary>
        /// 执行索引删除
        /// </summary>
        /// <returns></returns>
        public int doDropIndex()
        {
            var cmd = this.toCreateIndex();
            var cc = DBLive.ExeNonQuery(cmd);
            return cc;
        }
    }
}
