
using System;

using System.Text;



namespace mooSQL.data
{
    /**
     * 复杂的where条件构造器。 本类下 各where项的连接采用  and or 方法
     */
    public class WhereItem
    {
        /// <summary>
        /// 字段 ps（Paras）。
        /// </summary>
        public Paras ps;
        private SQLBuilder root;

        private StringBuilder sqlbase = new StringBuilder();
        /// <summary>
        /// 默认情况下，输出括号项时，自动在前后追加一层括号包裹。
        /// </summary>
        public bool autoKuohao = true;

        /// <summary>
        /// 初始化 WhereItem（构造）。
        /// </summary>
        public WhereItem(SQLBuilder roo)
        {
            this.ps = roo.ps;
            this.root = roo;
        }

        /// <summary>
        /// and 方法（返回 WhereItem）。
        /// </summary>
        public WhereItem and(string key)
        {
            return and(key, null, "", false);
        }
        /// <summary>
        /// and 方法（返回 WhereItem）。
        /// </summary>
        public WhereItem and(string key, Object val)
        {
            return and(key, val, "=", true);
        }
        /// <summary>
        /// and 方法（返回 WhereItem）。
        /// </summary>
        public WhereItem and(string key, Object val, string op)
        {
            return and(key, val, op, true);
        }
        /// <summary>
        /// and 方法（返回 WhereItem）。
        /// </summary>
        public WhereItem and(string key, Object val, string op, bool paramed)
        {
            return append(key, val, op, paramed, "AND");
        }

        /// <summary>
        /// or 方法（返回 WhereItem）。
        /// </summary>
        public WhereItem or(string key)
        {
            return or(key, null, "", false);
        }
        /// <summary>
        /// or 方法（返回 WhereItem）。
        /// </summary>
        public WhereItem or(string key, Object val)
        {
            return or(key, val, "=", true);
        }
        /// <summary>
        /// or 方法（返回 WhereItem）。
        /// </summary>
        public WhereItem or(string key, Object val, string op)
        {
            return or(key, val, op, true);
        }
        /// <summary>
        /// or 方法（返回 WhereItem）。
        /// </summary>
        public WhereItem or(string key, Object val, string op, bool paramed)
        {
            return append(key, val, op, paramed, "OR");
        }


        /// <summary>
        /// orlike 方法（返回 WhereItem）。
        /// </summary>
        public WhereItem orlike(string key, Object val)
        {
            if (sqlbase.Length > 0)
            {
                sqlbase.Append(" OR ");
            }
            return appendFormat(key + " like concat(concat('%', {0}), '%')", val);
        }
        /// <summary>
        /// orFormat 方法（返回 WhereItem）。
        /// </summary>
        public WhereItem orFormat(string key, params Object[] values)
        {
            if (sqlbase.Length > 0)
            {
                sqlbase.Append(" OR ");
            }
            var dbstr = root.DBLive.expression.paraPrefix;
            for (int i = 0; i < values.Length; i++)
            {
                string ke = "wf_" + ps.Count;
                key = key.Replace("[{]" + i + "[}]", dbstr + ke);
                ps.AddByPrefix(ke, values[i],dbstr);
            }
            sqlbase.Append(key);
            return this;
        }

        /// <summary>
        /// append 方法（返回 WhereItem）。
        /// </summary>
        public WhereItem append(string key, Object val, string op, bool paramed, string connector)
        {
            var dbstr = root.DBLive.expression.paraPrefix;
            string prefix = "wi_" + ps.Count;
            if (sqlbase.Length > 0)
            {
                sqlbase.Append(" " + connector + " ");
            }
            sqlbase.Append(key + " ");
            sqlbase.Append(op + " ");
            if (paramed)
            {
                sqlbase.Append(dbstr + prefix);
                ps.AddByPrefix(prefix, val, dbstr);
            }
            else if (val != null)
            {
                sqlbase.Append(val.ToString());
            }
            return this;
        }

        /// <summary>
        /// appendFormat 方法（返回 WhereItem）。
        /// </summary>
        public WhereItem appendFormat(string key, params Object[] values)
        {
            var dbstr = root.DBLive.expression.paraPrefix;
            for (int i = 0; i < values.Length; i++)
            {
                string ke = "wf_" + ps.Count;
                key = key.Replace("[{]" + i + "[}]", dbstr + ke);
                ps.AddByPrefix(ke, values[i], dbstr);
            }
            sqlbase.Append(key);
            return this;
        }

        /// <summary>
        /// end 方法（返回 SQLBuilder）。
        /// </summary>
        public SQLBuilder end()
        {
            string tar = sqlbase.ToString();
            if (this.autoKuohao)
            {
                tar = "(" + tar + ")";
            }
            root.where(tar);
            return root;
        }
    }
}