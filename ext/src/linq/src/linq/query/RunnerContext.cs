using mooSQL.data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq.Linq
{
    /// <summary>
    /// 执行 SentenceBag 时的所有参数.
    /// </summary>
    internal class RunnerContext
    {
        public DBInstance dataContext;
        public Expression expression;

        public SentenceBag sentenceBag;

        public object?[]? paras;
        public object?[]? premble;
        public CancellationToken cancellationToken;
    }
}
