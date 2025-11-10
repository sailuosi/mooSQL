using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    public partial class EntityTranslator
    {
        /// <summary>
        /// 清空配置
        /// </summary>
        /// <returns></returns>
        public EntityTranslator clear()
        {
            this._onParseTableName = null;
            this.ignoreInsertFields.Clear();
            this.ignoreUpdateFields.Clear();
            this.includeUpdateFields.Clear();
            this.ignoreInsertFields.Clear();
            return this;
        }
        /// <summary>
        /// 设置更新的表名
        /// </summary>
        /// <param name="onParseTableName"></param>
        /// <returns></returns>
        public EntityTranslator setTable(Func<EntityInfo, string> onParseTableName)
        {
            _onParseTableName = onParseTableName;
            return this;
        }
        /// <summary>
        /// 设置更新时要包含的字段
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        public EntityTranslator includeUpdate(IEnumerable<string> fields) {
            foreach (var field in fields) {
                if (string.IsNullOrEmpty(field)) continue;
                var f=field.Trim();
                if (includeUpdateFields.Contains(f)) {
                    continue;
                }
                includeUpdateFields.Add(f);
            }
            return this;
        }
        public EntityTranslator includeUpdate(params string[] fields)
        {
            return includeUpdate(fields);
        }
        /// <summary>
        /// 设置更新时要忽略的字段
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        public EntityTranslator ignoreUpdate(IEnumerable<string> fields)
        {
            foreach (var field in fields)
            {
                if (string.IsNullOrEmpty(field)) continue;
                var f = field.Trim();
                if (ignoreUpdateFields.Contains(f))
                {
                    continue;
                }
                ignoreUpdateFields.Add(f);
            }
            return this;
        }

        public EntityTranslator ignoreUpdate(params string[] fields)
        {
            return includeUpdate(fields);
        }

        /// <summary>
        /// 设置插入时要包含的字段
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        public EntityTranslator includeInsert(IEnumerable<string> fields)
        {
            foreach (var field in fields)
            {
                if (string.IsNullOrEmpty(field)) continue;
                var f = field.Trim();
                if (includeInsertField.Contains(f))
                {
                    continue;
                }
                includeInsertField.Add(f);
            }
            return this;
        }

        public EntityTranslator includeInsert(params string[] fields)
        {
            return includeUpdate(fields);
        }

        /// <summary>
        /// 设置插入时要忽略的字段
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        public EntityTranslator ignoreInsert(IEnumerable<string> fields)
        {
            foreach (var field in fields)
            {
                if (string.IsNullOrEmpty(field)) continue;
                var f = field.Trim();
                if (ignoreInsertFields.Contains(f))
                {
                    continue;
                }
                ignoreInsertFields.Add(f);
            }
            return this;
        }

        public EntityTranslator ignoreInsert(params string[] fields)
        {
            return includeUpdate(fields);
        }
    }
}
