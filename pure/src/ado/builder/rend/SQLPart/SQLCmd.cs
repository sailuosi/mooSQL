
using mooSQL.data.model;
using System;
using System.Collections.Generic;
using System.Data;

namespace mooSQL.data
{
    /// <summary>
    /// 一个准备执行的SQL命令，包含SQL文本，参数等。
    /// </summary>
    public class SQLCmd
    {
        /// <summary>结果缓存自动键前缀（与 ScriptTemplate <c>moo.st:</c> 隔离）。</summary>
        public const string ResultCacheKeyPrefix = "RC:";

        /// <summary>
        /// 预执行的命令
        /// </summary>
        public SQLCmd() { 
            this.para= new Paras();
            this.type = QueryType.Unknown;
        }
        /// <summary>
        /// 创建预执行的命令
        /// </summary>
        /// <param name="sql"></param>
        public SQLCmd(string sql) {
            this.sql = sql;
            this.para = new Paras();
            this.type = QueryType.Unknown;
        }
        /// <summary>
        /// 创建预执行的命令
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="paras"></param>
        public SQLCmd(string sql,Paras paras) {
            this.sql=sql;
            this.para = new Paras();
            this.type = QueryType.Unknown;
            if (paras != null) {
                this.para.Copy(paras);
            }
        }
        /// <summary>
        /// 创建预执行的SQL
        /// </summary>
        public string sql { get; set; }
        /// <summary>
        /// 参数体集合
        /// </summary>
        public Paras para { get; set; }
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
        /// 复制存在的参数到本实例中
        /// </summary>
        /// <param name="pa"></param>
        public void copy(Paras pa) { 
            para.Copy(pa);
        }
        /// <summary>
        /// SQL是否为空
        /// </summary>
        public bool Empty
        {
            get {
                return string.IsNullOrWhiteSpace(sql); 
            }
        }

        /// <summary>
        /// 解析 Live/Delay 参数（<see cref="Paras.ResolveDelayParas"/>），写回 <see cref="sql"/>。
        /// 执行前与计算缓存指纹前均应调用；已解析则幂等。
        /// </summary>
        public SQLCmd EnsureLiveParasResolved()
        {
            if (para != null)
                sql = para.ResolveDelayParas(sql ?? "");
            return this;
        }

        /// <summary>
        /// 与 SQLBuilder 模版编排指纹一致：使用 <see cref="ScriptHash"/>（内部 <c>HashCode.Combine</c>），
        /// 对解析后的 sql 文本及每个参数名、参数值累加哈希。
        /// 调用时会先 <see cref="EnsureLiveParasResolved"/>。
        /// </summary>
        public override int GetHashCode()
        {
            EnsureLiveParasResolved();
            var h = new ScriptHash();
            h.Add(sql);
            if (para != null && para.value != null && para.value.Count > 0)
            {
                foreach (var key in SortedParaKeys(para.value))
                {
                    h.Add(key);
                    var p = para.value[key];
                    h.Add(p != null ? p.val : null);
                }
            }
            return h.ToHashCode();
        }

        /// <summary>
        /// 结果缓存字符串键：<c>RC:</c> + <see cref="GetHashCode"/> 的 8 位大写十六进制
        /// （与 <see cref="ScriptCacheKey"/> 编排哈希 <c>X8</c> 展示风格一致）。
        /// 调用时会先加载 LivePara。
        /// </summary>
        public string GetCacheKey()
        {
            return GetCacheKey(ResultCacheKeyPrefix);
        }

        /// <summary>
        /// 指定前缀的缓存键：<paramref name="prefix"/> + HashCode 十六进制。
        /// 前缀通常由 <c>SQLBuilder.useCachePrefix</c> 经 <c>StepBuilder.ComposeAutoCacheKeyPrefix</c> 规范化
        /// （如 <c>RC:Shop:</c>），再拼 <c>X8</c>。
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
            // 先比哈希；再比解析后 sql，降低碰撞误判
            if (GetHashCode() != other.GetHashCode()) return false;
            return string.Equals(sql, other.sql, StringComparison.Ordinal);
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
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        /// <returns></returns>
        public T giveTo<T>(Func<SQLCmd,T> func) {
            return func(this);
            //return tar;
        }
        /// <summary>
        /// 传递
        /// </summary>
        /// <param name="tar"></param>
        /// <returns></returns>
        public ISQLCmdTaker giveTo(ISQLCmdTaker tar)
        {
            tar.TakeOver(this);
            return tar;
        }
        /// <summary>
        /// 转换为原始SQL语句，不带参数占位符。
        /// </summary>
        /// <param name="paraPrefix"></param>
        /// <returns></returns>
        public string toRawSQL(string paraPrefix="") {

            var sql = this.sql;
            if (para == null) return sql;
            foreach (var item in para.value)
            {
                if (sql.Contains(item.Value.holder)) {
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
