using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using mooSQL.data.builder;

namespace mooSQL.data
{
    /// <summary>
    /// merge into 语句构建器，支持快速构建和复杂语句的构建。在不支持merge语句的数据库中，可以使用此构建器构建兼容的SQL。
    /// </summary>
    public class MergeIntoBuilder
    {

        private DBInstance DBLive { get; set; }
        /// <summary>
        /// 父构建器，用于构建SQL语句的主体部分。
        /// </summary>
        public SQLBuilder parent { get; set; }
        /// <summary>
        /// 目标表
        /// </summary>
        public string intoTable {  get; set; }
        /// <summary>
        /// 目标表别名
        /// </summary>
        public string intoAlias { get; set; }
        /// <summary>
        /// 源表
        /// </summary>
        public string usingTable { get; set; }

        /// <summary>
        /// 源表别名
        /// </summary>
        public string usingAlias { get; set; }
        /// <summary>
        /// 源表构建器
        /// </summary>
        public SQLBuilder srcBuilder { get; set; }
        /// <summary>
        /// 桥接条件
        /// </summary>
        public SQLBuilder onPart { get; set; }

        /// <summary>
        /// 分支条件，可以有多个，每个分支可以有更新和插入操作
        /// </summary>
        public List<MergeBranch> branches { get; set; }
        /// <summary>
        /// 更新分支，可以有多个字段的更新操作
        /// </summary>
        public MergeBranch updateBranch { get; set; }
        /// <summary>
        /// 插入分支，可以有多个字段的更新操作
        /// </summary>
        public MergeBranch insertBranch { get; set; }

        private bool _printSQL = false;
        private Action<string> onSQLPrint;
        /// <summary>
        /// 打印执行的SQL
        /// </summary>
        /// <param name="onPrint"></param>
        /// <returns></returns>
        public MergeIntoBuilder print(Action<string> onPrint)
        {
            this._printSQL = true;
            this.onSQLPrint = onPrint;
            return this;
        }
        /// <summary>
        /// 构造一个合并构建器，用于构建merge into语句
        /// </summary>
        /// <param name="db"></param>
        public MergeIntoBuilder(DBInstance db) { 
            this.DBLive = db;
            this.parent = db.useSQL();
            this.branches = new List<MergeBranch>();
            this.onPart= parent.getBrotherBuilder();
        }
        /// <summary>
        /// 重置合并构建器
        /// </summary>
        public void clear() { 
            this.branches.Clear();
            this.intoTable = null;
            this.usingTable = null;
            this.onPart = null;
            this.srcBuilder = null;
            this.intoAlias = null;
            this.usingAlias = null;

        }
        /// <summary>
        /// 设置目标表，可以是别名或者原名
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="alias"></param>
        /// <returns></returns>
        public MergeIntoBuilder into(string tbName, string alias = null) { 
            this.intoTable = tbName;
            if (alias != null) { 
                this.intoAlias = alias;
            }
            return this;
        }
        /// <summary>
        /// 设置源表
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="alias"></param>
        /// <returns></returns>
        public MergeIntoBuilder from(string tbName, string alias = null) { 
            this.usingTable = tbName;
            if (alias != null) { 
                this.usingAlias = alias;
            }
            return this;
        }

        public MergeIntoBuilder from(string aliasName, Action<SQLBuilder> doSelect)
        {
            this.usingAlias = aliasName;
            if (this.srcBuilder == null) {
                this.srcBuilder = parent.getBrotherBuilder();
            }
            
            doSelect(this.srcBuilder);
            return this;
        }
        /// <summary>
        /// 构建源表和目标表的桥接条件，例如：a.id=b.id or a.name = b.name
        /// </summary>
        /// <param name="onPart"></param>
        /// <returns></returns>
        public MergeIntoBuilder on(string onPart) {

            this.onPart.where(onPart);
            return this;
        }
        /// <summary>
        /// 获取一个新的分支条件，可以有多个分支条件
        /// </summary>
        /// <returns></returns>
        public MergeBranch when() {
            var branch = new MergeBranch(this);
            branches.Add(branch);
            return branch;
        }
        /// <summary>
        /// 当匹配时的额外条件
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public MergeBranch whenMatched(Action<SQLBuilder> action)
        {
            return this.when().whenMatched(action);
        }
        /// <summary>
        /// 当不匹配时的额外条件
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public MergeBranch whenNotMatch(Action<SQLBuilder> action)
        {
            return this.when().whenNotMatched(action);
        }
        /// <summary>
        /// 当匹配时更新
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public MergeIntoBuilder whenMatchThenUpdate(Action<SQLBuilder> action) { 
            this.when()
                .whenMatched()
                .thenUpdate(action);
            return this;
        }
        /// <summary>
        /// 当不匹配时插入
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public MergeIntoBuilder whenNotMatchThenInsert(Action<SQLBuilder> action) { 
            this.when()
                .whenNotMatched()
                .thenInsert(action);
            return this;
        }
        /// <summary>
        /// 当匹配时删除
        /// </summary>
        /// <returns></returns>
        public MergeIntoBuilder whenMatchThenDelete() { 
            this.when()
                .whenMatched()
                .thenDelete();
            return this;
        }
        /// <summary>
        /// 设置插入时的字段，将自动创建插入分支
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <param name="paramed"></param>
        /// <returns></returns>
        public MergeIntoBuilder setI(string key, Object val, bool paramed = true) { 
            if(this.insertBranch == null)
            {
                this.insertBranch = this.when().whenNotMatched();
                insertBranch.ThenAction = MergeAction.insert;
            }
            insertBranch.SetPart.set(key, val, paramed);
            return this;
        }
        /// <summary>
        /// 设置更新时的字段，将自动创建更新分支
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <param name="paramed"></param>
        /// <returns></returns>
        public MergeIntoBuilder setU(string key, Object val, bool paramed = true)
        {
            if (this.updateBranch == null)
            {
                this.updateBranch = this.when().whenNotMatched();
                updateBranch.ThenAction = MergeAction.update;
            }
            updateBranch.SetPart.set(key, val, paramed);
            return this;
        }
        /// <summary>
        /// 设置字段，同时生效于新增和更新，将自动创建更新和插入分支
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <param name="paramed"></param>
        /// <returns></returns>
        public MergeIntoBuilder set(string key, Object val, bool paramed = true)
        {
            this.setI(key, val, paramed);
            this.setU(key, val, paramed);
            return this;
        }
        /// <summary>
        /// 输出片段对象，用于自定义输出格式。
        /// </summary>
        /// <returns></returns>
        public FragMergeInto toFrag()
        {
            var frag = new FragMergeInto();
            frag.intoTable = this.intoTable;
            frag.intoAlias = this.intoAlias;
            frag.usingTable = this.usingTable;
            frag.usingAlias = this.usingAlias;
            frag.onPart = this.onPart.buildWhereContent();

            if (this.srcBuilder != null) {
                frag.usingTable = "("+srcBuilder.current.buildSelect()+")";
            }

            frag.mergeWhens = new List<FragMergeWhen>();
            if (branches.Count == 0)
            {
                return frag;
            }
            foreach (var branch in branches) { 
                frag.mergeWhens.Add(branch.toFrag());
            }
            return frag;

        }
        /// <summary>
        /// 构建最终的SQL语句，但不包含参数部分。
        /// </summary>
        /// <returns></returns>
        public string buildMergeInto()
        {
            var frag = this.toFrag();
            return parent.Dialect.expression.buildMergeInto(frag);
        }
        /// <summary>
        /// 构建最终的SQL语句，并执行。
        /// </summary>
        /// <returns></returns>
        public SQLCmd toMergeInto()
        {
            string sql = buildMergeInto();
            return new SQLCmd(sql, parent.ps);
        }
        /// <summary>
        /// 执行最终的SQL语句。
        /// </summary>
        /// <returns></returns>
        public int doMergeInto()
        {
            var cmd = toMergeInto();
            if (this._printSQL)
            {
                var sql= cmd.toRawSQL(DBLive.dialect.expression.paraPrefix);
                this.onSQLPrint(sql);
            }
            return parent.DBLive.ExeNonQuery(cmd);
        }
    }
}
