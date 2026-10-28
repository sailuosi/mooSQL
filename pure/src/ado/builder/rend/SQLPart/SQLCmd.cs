
using mooSQL.data.model;
using System;
using System.Collections.Generic;
using System.Data;

namespace mooSQL.data
{
    /// <summary>
    /// 一个准备执行的SQL命令：逻辑上一次执行单元。
    /// SQL 与参数始终由 <see cref="SQLSeg"/> 承载（单段或多段）；
    /// <see cref="sql"/> / <see cref="para"/> 为对首段的兼容门面。
    /// </summary>
    public partial class SQLCmd
    {
        /// <summary>结果缓存自动键前缀（与 ScriptTemplate <c>moo.st:</c> 隔离）。</summary>
        public const string ResultCacheKeyPrefix = "RC:";

        /// <summary>
        /// 预执行的命令（单个空段）。
        /// </summary>
        public SQLCmd() {
            UseSingleSegment(null, null);
            this.type = QueryType.Unknown;
        }
        /// <summary>
        /// 创建预执行的命令（单段）。
        /// </summary>
        /// <param name="sql"></param>
        public SQLCmd(string sql) {
            UseSingleSegment(sql, null);
            this.type = QueryType.Unknown;
        }
        /// <summary>
        /// 创建预执行的命令（单段）。
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="paras"></param>
        public SQLCmd(string sql,Paras paras) {
            UseSingleSegment(sql, paras);
            this.type = QueryType.Unknown;
        }
        /// <summary>
        /// 首段 SQL。set 时重置为单段（丢弃其余分段）。
        /// </summary>
        public string sql
        {
            get => PrimarySegment.sql;
            set
            {
                EnsurePrimarySegment();
                var keepPara = _segments[0].para;
                UseSingleSegment(value, keepPara);
            }
        }
        /// <summary>
        /// 首段参数袋（可直接增删）。set 整袋时重置为单段。
        /// </summary>
        public Paras para
        {
            get
            {
                var seg = PrimarySegment;
                if (seg.para == null)
                    seg.para = new Paras();
                return seg.para;
            }
            set
            {
                EnsurePrimarySegment();
                var keepSql = _segments[0].sql;
                UseSingleSegment(keepSql, value ?? new Paras());
            }
        }
        /// <summary>
        /// 命令类型,默认text
        /// </summary>
        public CommandType? cmdType {  get; set; }
        /// <summary>
        /// SQL语句类型
        /// </summary>
        public QueryType type { get; set; }
        /// <summary>
        /// 主写入/删除/合并目标表名（由 StepBuilder 生成时填充；手写 SQL 需自行设置以参与按表过滤的监听）。
        /// </summary>
        public string TargetTable { get; set; }
        /// <summary>
        /// SQL语句的超时设置
        /// </summary>
        public int timeout { get; set; }
        /// <summary>
        /// 信号
        /// </summary>
        public string signal { get; set; }

        /// <summary>
        /// 复制存在的参数到首段参数袋。
        /// </summary>
        /// <param name="pa"></param>
        public void copy(Paras pa) { 
            para.Copy(pa);
        }
        /// <summary>
        /// 是否无有效 SQL（所有段皆空）。
        /// </summary>
        public bool Empty
        {
            get {
                EnsurePrimarySegment();
                for (var i = 0; i < _segments.Count; i++)
                {
                    if (_segments[i] != null && !_segments[i].Empty)
                        return false;
                }
                return true;
            }
        }

        /// <summary>
        /// 解析 Live/Delay 参数，逐段写回各段 <see cref="SQLSeg.sql"/>。
        /// </summary>
        public SQLCmd EnsureLiveParasResolved()
        {
            EnsurePrimarySegment();
            for (var i = 0; i < _segments.Count; i++)
                _segments[i]?.EnsureLiveParasResolved();
            return this;
        }

        /// <summary>
        /// 指纹：按段序列累加 sql 与参数。
        /// </summary>
        public override int GetHashCode()
        {
            EnsureLiveParasResolved();
            var h = new ScriptHash();
            EnsurePrimarySegment();
            for (var i = 0; i < _segments.Count; i++)
            {
                var seg = _segments[i];
                if (seg == null) continue;
                h.Add(seg.sql);
                AddParaHash(ref h, seg.para);
            }
            return h.ToHashCode();
        }

        static void AddParaHash(ref ScriptHash h, Paras para)
        {
            if (para == null || para.value == null || para.value.Count == 0)
                return;
            foreach (var key in SortedParaKeys(para.value))
            {
                h.Add(key);
                var p = para.value[key];
                h.Add(p != null ? p.val : null);
            }
        }

        /// <summary>
        /// 结果缓存字符串键：<c>RC:</c> + <see cref="GetHashCode"/> 的 8 位大写十六进制。
        /// </summary>
        public string GetCacheKey()
        {
            return GetCacheKey(ResultCacheKeyPrefix);
        }

        /// <summary>
        /// 指定前缀的缓存键。
        /// </summary>
        public string GetCacheKey(string prefix)
        {
            EnsureLiveParasResolved();
            var code = unchecked((uint)GetHashCode());
            var p = string.IsNullOrEmpty(prefix) ? ResultCacheKeyPrefix : prefix;
            return p + code.ToString("X8");
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            if (!(obj is SQLCmd other)) return false;
            if (GetHashCode() != other.GetHashCode()) return false;
            if (SegmentCount != other.SegmentCount) return false;
            EnsurePrimarySegment();
            other.EnsurePrimarySegment();
            for (var i = 0; i < _segments.Count; i++)
            {
                var a = _segments[i]?.sql;
                var b = other._segments[i]?.sql;
                if (!string.Equals(a, b, StringComparison.Ordinal))
                    return false;
            }
            return true;
        }

        static IEnumerable<string> SortedParaKeys(IDictionary<string, Parameter> map)
        {
            if (map.Count <= 1)
                return map.Keys;
            var keys = new string[map.Count];
            map.Keys.CopyTo(keys, 0);
            Array.Sort(keys, StringComparer.Ordinal);
            return keys;
        }

        /// <summary>
        /// 传递
        /// </summary>
        public T giveTo<T>(Func<SQLCmd,T> func) {
            return func(this);
        }
        /// <summary>
        /// 传递
        /// </summary>
        public ISQLCmdTaker giveTo(ISQLCmdTaker tar)
        {
            tar.TakeOver(this);
            return tar;
        }
        /// <summary>
        /// 转换为原始SQL；多段时换行拼接各段。
        /// </summary>
        public string toRawSQL(string paraPrefix="") {
            this.EnsureLiveParasResolved();
            EnsurePrimarySegment();
            if (_segments.Count == 1)
                return FormatRawSql(_segments[0].sql, _segments[0].para, paraPrefix);

            var parts = new List<string>(_segments.Count);
            for (var i = 0; i < _segments.Count; i++)
            {
                var seg = _segments[i];
                if (seg == null || seg.Empty) continue;
                parts.Add(FormatRawSql(seg.sql, seg.para, paraPrefix));
            }
            return string.Join("\n", parts);
        }

        static string FormatRawSql(string sqlText, Paras para, string paraPrefix)
        {
            var sql = sqlText;
            if (para == null) return sql;
            foreach (var item in para.value)
            {
                if (sql != null && sql.Contains(item.Value.holder)) {
                    sql = sql.Replace(item.Value.holder, "'" + item.Value.val.ToString() + "'");
                }
                else
                {
                    sql = sql.Replace(paraPrefix + item.Key, "'" + item.Value.val.ToString() + "'");
                }
            }
            return sql;
        }

    
    }
}
