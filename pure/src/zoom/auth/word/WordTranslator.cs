// 基础功能说明：


using mooSQL.data;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading.Tasks;

namespace mooSQL.auth
{
    /// <summary>
    /// 语义翻译器。将一个定义好的字符串语义，转义为语义集合，并最终执行。
    /// </summary>
    public class WordTranslator
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="authRange"></param>
        public WordTranslator() { 

        }

        internal AuthDialect dialect;
        /// <summary>
        /// 语义池。
        /// </summary>
        public List<AuthWord> words= new List<AuthWord>();
        /// <summary>
        /// 已解析的语义
        /// </summary>
        public Dictionary<string, ConditionGroup> parseedWords = new Dictionary<string, ConditionGroup>();

        /// <summary>
        /// 可供解析的动态参数
        /// </summary>
        public Dictionary<string,Object> para = new Dictionary<string,Object>();

        public bool Empty
        {
            get { 
                if(words.Count > 0) return false;
                return true;
            }
        }

        private Action<ConditionGroup, SQLBuilder> _nextWordBuildInvoker;
        /// <summary>
        /// 注册编织器加载事件
        /// </summary>
        /// <param name="registerBuilder"></param>
        public void onLoadBuild(Action<ConditionGroup, SQLBuilder> registerBuilder) { 
            this._nextWordBuildInvoker = registerBuilder;
        }
        /// <summary>
        /// 注册背景参数
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void addPara(string key, Object value)
        {
            para[key] = value;
        }
        /// <summary>
        /// 添加词条
        /// </summary>
        /// <param name="word"></param>
        public void addWord(AuthWord word)
        {
            if (word != null && !words.Contains(word)) { 
                words.Add(word);
            }
        }

        /// <summary>
        /// 执行角色的语义解析
        /// </summary>
        public void parse(AuthDialect dialect) { 
            foreach (AuthWord scope in words)
            {
                var val = scope.parser;
                if (!string.IsNullOrWhiteSpace(val))
                {
                    var tar = dialect.pipeline.parseAuthWord(scope);
                    if (tar != null)
                    {
                        parseedWords[scope.id] = tar;
                    }
                }
            }
        
        }
        /// <summary>
        /// 执行语义的应用
        /// </summary>
        public void build(SQLBuilder kit,AuthDialect dialect) {

            foreach (var kv in parseedWords) {
                if (kit != null) {
                    kv.Value.translator = this;
                    foreach (Condition condi in kv.Value.Filters)
                    {
                        dialect.pipeline.readAuthWordPara(condi, para);
                    }
                    //如果自定义读取逻辑时，使用自定义逻辑。
                    if (_nextWordBuildInvoker != null)
                    {
                        kv.Value.nextWordBuildInvoker = _nextWordBuildInvoker;
                        _nextWordBuildInvoker(kv.Value,kit);
                        continue;
                    }
                    //动态语义必须业务层提供上下文，否则不予执行。
                    //kv.Value.ApplyToSQL(range.kit);
                    
                }
            }
        }



    }
}