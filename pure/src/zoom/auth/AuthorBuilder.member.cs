// 基础功能说明：

using mooSQL.auth;
using mooSQL.data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.auth
{
    /// <summary>
    /// 需要访问顶级实例或者方言实例的方法，一律注册到这里
    /// </summary>
    public abstract partial class AuthorBuilder<RealDialect> where RealDialect : AuthDialect
    {

        /// <summary>
        /// 自定义的数据范围解析器，角色加载时刻
        /// </summary>
        private List<Action<AuthWord, RealDialect>> customPreWorkReads = new List<Action<AuthWord, RealDialect>>();

        private List<Func<AuthWord, RealDialect, string>> customLazyWorkReads = new List<Func<AuthWord, RealDialect, string>>();

        /// <summary>
        /// 数据范围加载时刻，解析参数到 数据范围体中。。
        /// </summary>
        /// <param name="parser"></param>
        /// <returns></returns>
        public AuthorBuilder<RealDialect> onLoadWord(Action<AuthWord, RealDialect> parser)
        {
            customPreWorkReads.Add(parser);
            return this;
        }
        /// <summary>
        /// 数据范围解析时刻，此时，需要直接把结果生成到最终的条件中
        /// </summary>
        /// <param name="parser"></param>
        /// <returns></returns>
        public AuthorBuilder<RealDialect> onParseWord(Func<AuthWord, RealDialect, string> parser)
        {
            customLazyWorkReads.Add(parser);
            return this;
        }

        /// <summary>
        /// 唤起最终构建时刻的解析器。
        /// </summary>
        /// <param name="wh"></param>
        /// <returns></returns>
        internal List<string> invokeLazyReador(List<string> wh, SQLBuilder kit)
        {
            if (customLazyWorkReads.Count > 0)
            {
                foreach (var reader in customLazyWorkReads)
                {
                    foreach (var kv in wordBag.groups) { 
                        foreach (var word in kv.Value.lazyWords)
                        {
                            var readOutput = reader(word, dialect);
                            if (!string.IsNullOrWhiteSpace(readOutput))
                            {
                                wh.Add(readOutput);
                            }
                        }                    
                    
                    }

                }

            }
            //执行动态语义的解析

            if (dialect.wordTranslator != null)
            {
                dialect.wordTranslator.build(kit, dialect);
            }

            return wh;
        }


        /// <summary>
        /// 调用注册的语义解析器
        /// </summary>
        public void invokeWordPreLoader()
        {
            if (customPreWorkReads.Count > 0)
            {
                foreach (var reader in customLazyWorkReads)
                {
                    foreach (var kv in wordBag.groups) { 
                        foreach (var word in kv.Value.lazyWords)
                        {
                            var readOutput = reader(word, dialect);
                            if (!string.IsNullOrWhiteSpace(readOutput))
                            {

                            }
                        }                    
                    }

                }

            }

        }
    }
}