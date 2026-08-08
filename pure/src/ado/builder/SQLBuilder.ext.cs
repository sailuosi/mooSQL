using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    public partial class SQLBuilder
    {
        /// <summary>
        /// 创建一个新的实例，默认会继承事务
        /// </summary>
        /// <returns></returns>
        public SQLBuilder useSQL(bool useTransaction=true)
        {
            var tar= this.DBLive.client.ClientFactory.useSQL(DBLive);
            if (useTransaction && this.Executor != null) {
                tar.useTransaction(this.Executor);
            }
            return tar;
        }
        /// <summary>
        /// 开始创建DDL构造器
        /// </summary>
        /// <returns></returns>
        public DDLBuilder useDDL() {
            return DBLive.useDDL();
        }
        /// <summary>
        /// 获取快捷查询功能语句
        /// </summary>
        /// <returns></returns>
        public SQLSentence useSentence()
        {
            return DBLive.dialect.sentence;
        }


    }
}
