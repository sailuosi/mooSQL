// 本部分是承载由DB快捷获取其衍生的上层功能类的功能。

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    public partial class DBInstance
    {

        /// <summary>
        /// 创建绑定了当前数据库的编织器
        /// </summary>
        /// <returns></returns>
        [Obsolete("请使用useSQL代替")]
        public SQLBuilder newKit()
        {
            return client.ClientFactory.useSQL(this);
        }

        /// <summary>
        /// 获取绑定的批量SQL执行器
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public BatchSQL newBatchKit(int position)
        {

            var builder = new SQLBuilder();
            builder.setDBInstance(this);
            var res = new BatchSQL(builder);
            return res;
        }
        /// <summary>
        /// 格式化SQL模板，并返回一个SQL命令对象。
        /// </summary>
        /// <param name="SQLTemplate"></param>
        /// <param name="paras"></param>
        /// <returns></returns>
        public SQLCmd Format(string SQLTemplate, params object[] paras)
        {
            int cc = 0;
            var ps= new Paras();
            string key = SQLTemplate;
            for (int i = 0; i < paras.Length; i++)
            {
                string reg = "{" + i + "}";
                var v = paras[i];
                if (v == null)
                {
                    key = key.Replace(reg, " null ");
                }
                else
                {
                    string ke =  "fmt_" + cc + "_" + i;

                    key = key.Replace(reg, this.dialect.expression.paraPrefix + ke);
                    ps.Add(ke, v);
                }

            }
            var cmd = new SQLCmd(key,ps);

            return cmd;

        }
    }
}
