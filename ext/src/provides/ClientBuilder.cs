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
        /// 创建默认 Soo 实体解析器（注册名 SooDefault）。业务可 override 返回子类以替换。
        /// </summary>
        protected override IEntityAnalyser CreateDefaultEntityAnalyser()
        {
            return new SooEntityAnalyser();
        }

        /// <summary>
        /// 执行构建
        /// </summary>
        /// <returns></returns>
        protected override void buildingCash()
        {
            useDialectFactory(new DialectFactory());
            // 默认解析器由 BaseClientBuilder.EnsureDefaultEntityAnalyserQueued 在 buildingCash 之后注册
        }
    }
}
