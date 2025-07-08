/*
 * 表达式的转换结果，打入builder后，自身转换为Builder的holder，否则，转为持有SQL的字符串
 * 原则上除了关键SQL语句节点，其它均返回 SQLFragClause
 */
using mooSQL.data;
using mooSQL.data.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq
{
    /// <summary>
    /// 将SQL模型转换为可直接执行的准SQL，利用SQLBuilder
    /// </summary>
    public partial class ClauseTranslateVisitor:ClauseVisitor
    {

        /// <summary>
        /// 根方言实例
        /// </summary>
        public Dialect dialect;
        /// <summary>
        /// SQL解释器
        /// </summary>
        /// <param name="parent"></param>
        public ClauseTranslateVisitor(Dialect parent) {
            this.dialect = parent;
        }
        /// <summary>
        /// SQL解释器
        /// </summary>
        /// <param name="DB"></param>
        public ClauseTranslateVisitor(DBInstance DB) { 
            this.init(DB);
        }
        /// <summary>
        /// 初始化数据库环境
        /// </summary>
        /// <param name="DB"></param>
        /// <returns></returns>
        public ClauseTranslateVisitor Prepare(DBInstance DB) {
            this.init(DB);
            return this;
        }
        /// <summary>
        /// 初始化环境
        /// </summary>
        /// <param name="DB"></param>
        protected void init(DBInstance DB) {
            this.DB = DB;
            var builder = new SQLBuilder();
            builder.setDBInstance(DB);

            this.rootBuilder = builder;
            this.builder = builder;
        }
        /// <summary>
        /// 数据库
        /// </summary>
        protected DBInstance DB;
        /// <summary>
        /// 根编制器
        /// </summary>
        protected SQLBuilder rootBuilder;
        /// <summary>
        /// 当前的编制器
        /// </summary>
        protected SQLBuilder builder;

        private Dictionary<SQLBuilder,SQLBuilderClause> _builderMapper = new Dictionary<SQLBuilder, SQLBuilderClause>();
        /// <summary>
        /// 已废弃
        /// </summary>
        protected SQLBuilderClause CurBuilderWrap
        {
            get {
                if(!_builderMapper.ContainsKey(builder)){ 
                    _builderMapper.Add(builder,new SQLBuilderClause(builder));
                }
                return _builderMapper[builder];
            }
        }

        /// <summary>
        /// 清空当前编织器的所有作业环境
        /// </summary>
        /// <returns></returns>
        public virtual ClauseTranslateVisitor Reset() {
            init(DB);
            return this;
        }


        /// <summary>
        /// 转译一个SQL字面量，以使它符合SQL语法，避免关键字。
        /// </summary>
        /// <param name="value"></param>
        /// <param name="convertType"></param>
        /// <returns></returns>
        public virtual string TranslateValue(string value, ConvertType convertType)
        {
            return value;
        }
        /// <summary>
        /// 执行转译
        /// </summary>
        /// <param name="sentence"></param>
        /// <returns></returns>
        public virtual SentenceCmds Translate(BaseSentence sentence) {

            var res = new SentenceCmds();
            var tar= Visit(sentence);
            //简单结果
            if (tar is SQLBuilderClause build) {
                var cmd = build.ToCmd();
                res.Add(cmd);
                return res;
            }

            if (tar is SQLBuildersClause builders) {
                foreach (var builder in builders.Builders) {
                    var cmd = builder.ToCmd(builder);
                    res.Add(cmd);
                }
            }
            // 为SQL文本结果
            if (tar is SQLFragClause frag) {
                res.Add(new SQLCmd(frag.ToString(),frag.Para));
            }

            return res;
        }
    }
}
