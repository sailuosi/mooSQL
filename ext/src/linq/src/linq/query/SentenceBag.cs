
using mooSQL.data;
using mooSQL.linq.Linq.Builder;
using mooSQL.linq.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq.Linq
{
    /// <summary>
    /// SQL模型包，代表一个LINQ查询的构建结果。
    /// </summary>
    internal class SentenceBag
    {

        public DBInstance DBLive;
        /// <summary>
        /// 待执行语句集
        /// </summary>
        public List<SentenceItem> Sentences;

        public ExecuteType ExecuteType;
        /// <summary>
        /// 原始LINQ表达式
        /// </summary>
        public Expression srcExp;

        public Type EntityType;


        public Expression ErrorExpression;

        public bool IsFinalized=false;
        /// <summary>
        /// 最终处理后的表达式
        /// </summary>
        public Expression finalExp;
        /// <summary>
        /// 中间环节的编辑器
        /// </summary>
        public IBuildContext buildContext;

        /** 结果执行器 **/


        private ISentenceRunner runner;

        public virtual ISentenceRunner Runner { 
            get {
                if (runner == null) {
                    runner = new BasicSentenceRunner();
                }
                return runner; 
            }
        }


        public void add(SentenceItem sentence) {
            if (Sentences is null) {
                Sentences= new List<SentenceItem>();
            }
            this.Sentences.Add(sentence);
        }



        List<Expression>? _parameterized;



        internal void SetParameterized(List<Expression>? parameterized)
        {
            _parameterized = parameterized;
        }


        internal void ClearDynamicQueryableInfo()
        {
            
        }

        Preamble[]? _preambles;

        internal void SetPreambles(List<Preamble>? preambles)
        {
            _preambles = preambles?.ToArray();
        }
        internal bool IsAnyPreambles()
        {
            return _preambles?.Length > 0;
        }
        internal object?[]? InitPreambles(DBInstance dc, Expression rootExpression, object?[]? ps)
        {
            if (_preambles == null)
                return null;

            var preambles = new object[_preambles.Length];
            for (var i = 0; i < preambles.Length; i++)
            {
                //dc, rootExpression, ps, preambles
                preambles[i] = _preambles[i].Execute(new RunnerContext
                {
                    dataContext = dc,
                    expression = rootExpression,
                    paras = ps,
                    premble = preambles
                });
            }

            return preambles;
        }

        internal async Task<object?[]?> InitPreamblesAsync(DBInstance dc, Expression rootExpression, object?[]? ps, CancellationToken cancellationToken)
        {
            if (_preambles == null)
                return null;

            var preambles = new object[_preambles.Length];
            for (var i = 0; i < preambles.Length; i++)
            {
                //dc, rootExpression, ps, preambles, cancellationToken
                preambles[i] = await _preambles[i].ExecuteAsync(new RunnerContext
                {
                    dataContext = dc,
                    expression = rootExpression,
                    paras = ps,
                    premble = preambles,
                    cancellationToken = cancellationToken
                }).ConfigureAwait(mooSQL.linq.Common.Configuration.ContinueOnCapturedContext);
            }

            return preambles;
        }
    }

    /// <summary>
    /// 含有类型参数的结果集
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class SentenceBag<T>:SentenceBag
    {

        private ISentenceRunner<T> runner;

        public ISentenceRunner<T> Runner
        {
            get {
                if (runner == null)
                {
                    runner = new BasicSentenceRunner<T>();
                }
                return runner; 
            }
        }

    }
}
