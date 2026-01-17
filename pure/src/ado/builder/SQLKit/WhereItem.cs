
using System;

using System.Text;



namespace mooSQL.data
{
    /**
     * 复杂的where条件构造器。 本类下 各where项的连接采用  and or 方法
     */
    public class WhereItem
    {
        public Paras ps;
        private SQLBuilder root;

        private StringBuilder sqlbase = new StringBuilder();
        /// <summary>
        /// 默认情况下，输出括号项时，自动在前后追加一层括号包裹。
        /// </summary>
        public bool autoKuohao = true;

        public WhereItem(SQLBuilder roo)
        {
            this.ps = roo.ps;
            this.root = roo;
        }

        public WhereItem and(string key)
        {
            return and(key, null, "", false);
        }
        public WhereItem and(string key, Object val)
        {
            return and(key, val, "=", true);
        }
        public WhereItem and(string key, Object val, string op)
        {
            return and(key, val, op, true);
        }
        public WhereItem and(string key, Object val, string op, bool paramed)
        {
            return append(key, val, op, paramed, "AND");
        }

        public WhereItem or(string key)
        {
            return or(key, null, "", false);
        }
        public WhereItem or(string key, Object val)
        {
            return or(key, val, "=", true);
        }
        public WhereItem or(string key, Object val, string op)
        {
            return or(key, val, op, true);
        }
        public WhereItem or(string key, Object val, string op, bool paramed)
        {
            return append(key, val, op, paramed, "OR");
        }


        public WhereItem orlike(string key, Object val)
        {
            if (sqlbase.Length > 0)
            {
                sqlbase.Append(" OR ");
            }
            return appendFormat(key + " like concat(concat('%', {0}), '%')", val);
        }
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
