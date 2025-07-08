using mooSQL.data.context;
using mooSQL.data.Mapping;
using mooSQL.data.slave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// mooSQL的用户侧顶级实例构建器
    /// </summary>
    public class DBClientBuilder : BaseClientBuilder
    {
        /// <summary>
        /// mooSQL的客户端构建器
        /// </summary>
        public DBClientBuilder() : base()
        {

        }
        /// <summary>
        /// 执行构建
        /// </summary>
        /// <returns></returns>
        protected override void buildingCash()
        {

            if (this.youCash.dialectFactory == null) {
                useDialectFactory( new DialectFactory());
            }

            this.useEnityAnalyser(new InternalEntityParser());
        }
    }
}
