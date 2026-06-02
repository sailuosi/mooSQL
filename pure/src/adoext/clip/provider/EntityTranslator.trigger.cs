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
        /// 触发插入逻辑，无影响时返回false
        /// </summary>
        /// <param name="kit"></param>
        /// <param name="field"></param>
        /// <param name="val"></param>
        /// <param name="en"></param>
        /// <returns></returns>
        protected virtual bool fireInsertField(SQLBuilder kit, string field, object val, EntityInfo en) {

            var vocs = this._onInsertField.GetInvocationList();
            if (vocs != null)
            {
                foreach (var del in vocs)
                {
                    // 2. 强转回具体的 Func 类型
                    var func = (Func<SQLBuilder, string, object, EntityInfo, bool>)del;

                    // 3. 执行并获取结果
                    var result = func.Invoke(kit,field,val,en);
                    if (result == true) {
                        return true;
                    }

                }
            }
            return false;
        }
        /// <summary>
        /// 触发更新逻辑字段解析
        /// </summary>
        /// <param name="kit"></param>
        /// <param name="field"></param>
        /// <param name="val"></param>
        /// <param name="en"></param>
        /// <returns></returns>
        protected virtual bool fireUpdateField(SQLBuilder kit, string field, object val, EntityInfo en)
        {

            var vocs = this._onUpdateField.GetInvocationList();
            if (vocs != null)
            {
                foreach (var del in vocs)
                {
                    // 2. 强转回具体的 Func 类型
                    var func = (Func<SQLBuilder, string, object, EntityInfo, bool>)del;

                    // 3. 执行并获取结果
                    var result = func.Invoke(kit, field, val, en);
                    if (result == true)
                    {
                        return true;
                    }

                }
            }
            return false;
        }

        /// <summary>
        /// fireBeforeInsert 方法。
        /// </summary>
        protected virtual void fireBeforeInsert(SQLBuilder kit, object entity, Type EntityType, EntityInfo en) {

            if (this._onBeforeInsertEntity != null)
            {
                this._onBeforeInsertEntity(kit, entity, EntityType, en);
            }
        }

        /// <summary>
        /// fireBeforeUpdate 方法。
        /// </summary>
        protected virtual void fireBeforeUpdate(SQLBuilder kit, object entity, Type EntityType, EntityInfo en) {
            if (this._onBeforeUpdateEntity != null)
            {
                this._onBeforeUpdateEntity(kit, entity, EntityType, en);
            }
        }

        /// <summary>
        /// fireBeforeDelete 方法。
        /// </summary>
        protected virtual void fireBeforeDelete(SQLBuilder kit, object entity, Type EntityType, EntityInfo en) {
            if (this._onBeforeDeleteEntity != null)
            {
                this._onBeforeDeleteEntity(kit, entity, EntityType, en);
            }
        }

        /// <summary>
        /// fireReadyInsert 方法。
        /// </summary>
        protected virtual void fireReadyInsert(SQLBuilder kit, object entity, Type EntityType, EntityInfo en) {
            if (this._onReadyInsertEntity != null)
            {
                this._onReadyInsertEntity(kit, entity, EntityType, en);
            }
        }

        /// <summary>
        /// fireReadyUpdate 方法。
        /// </summary>
        protected virtual void fireReadyUpdate(SQLBuilder kit, object entity, Type EntityType, EntityInfo en) {
            if (this._onReadyUpdateEntity != null)
            {
                this._onReadyUpdateEntity(kit, entity, EntityType, en);
            }
        }

        /// <summary>
        /// fireReadyDelete 方法。
        /// </summary>
        protected virtual void fireReadyDelete(SQLBuilder kit, object entity, Type EntityType, EntityInfo en) {
            if (this._onReadyDeleteEntity != null)
            {
                this._onReadyDeleteEntity(kit, entity, EntityType, en);
            }
        }

    }
}