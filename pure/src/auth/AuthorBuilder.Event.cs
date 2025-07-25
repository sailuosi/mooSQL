// 基础功能说明：

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.auth
{
    /// <summary>
    /// 编织器的事件类别注册，注册到这里
    /// </summary>

    public abstract partial class AuthorBuilder<RealDialect> where RealDialect : AuthDialect
    { 
        
        private HashSet<Action<RealDialect>> _onAfterLoadWordActions ;
        /// <summary>
        /// 所有数据权限的解析动作。
        /// </summary>
        /// <param name="onIsAll"></param>
        /// <returns></returns>
        public AuthorBuilder<RealDialect> onIsAll(Func<WordBagDialect, string> onIsAll)
        {

            if (onIsAll != null)
            {
                dialect.wordBag.onIsAll(onIsAll);
            }

            return this;

        }
        /// <summary>
        /// 词条加载完毕时刻，此时刚读取完毕角色下的词条，但尚未进行解析。
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public AuthorBuilder<RealDialect> onLoadedWords(Action<RealDialect> action) {

            if (action != null) {
                if (_onAfterLoadWordActions == null) {
                    _onAfterLoadWordActions = new HashSet<Action<RealDialect>>();
                }
                _onAfterLoadWordActions.Add(action);
            } 
            
            return this;

        }

        private void invokeOnLoadedWords() {
            if (_onAfterLoadWordActions != null) {
                foreach (var action in _onAfterLoadWordActions)
                {
                    action(dialect);
                }
            }
        }

    }
}
