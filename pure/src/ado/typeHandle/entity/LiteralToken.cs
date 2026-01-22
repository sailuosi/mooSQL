using System;
using System.Collections.Generic;

namespace mooSQL.data
{
 
    /// <summary>
    /// 表示一个占位符，该值应在生成的 SQL 中替换为字面量值
    /// </summary>
    internal readonly struct LiteralToken
    {
        /// <summary>
        /// 原始命令中应被替换的文本
        /// </summary>
        public string Token { get; }

        /// <summary>
        /// 标记引用的成员名称
        /// </summary>
        public string Member { get; }

        internal LiteralToken(string token, string member)
        {
            Token = token;
            Member = member;
        }

        internal static IList<LiteralToken> None => new List<LiteralToken>();
    }
    
}
