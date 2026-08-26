// 基础功能说明：

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    // 插入和修改的方面
    public partial class StepBuilder
    {

        /// <summary>
        /// 设置 update /delete 语句的目标 表。
        /// </summary>
        /// <param name="tbName"></param>
        /// <returns></returns>

        public override SQLBuilder setTable(string tbName)
        {
            current.setTable(tbName);
            return this;
        }

        #region 字段值赋值

        private UpdateSetNullOption setNullOption = UpdateSetNullOption.None;
        /// <summary>
        /// 永不会取 None
        /// </summary>
        public UpdateSetNullOption UpdateSetNullOpt
        {
            get { 
                if(this.setNullOption != UpdateSetNullOption.None)
                {
                    return this.setNullOption;
                }
                if(this.Client != null && this.Client.builderOption.UpdateSetNullOpt != UpdateSetNullOption.None)
                {
                    return this.Client.builderOption.UpdateSetNullOpt;
                }
                return UpdateSetNullOption.IgnoreNull;
            }
        }

        /// <summary>
        /// 设置当set的值对象是null时如何处理。
        /// </summary>
        /// <param name="option"></param>
        /// <returns></returns>
        public override SQLBuilder configSetNull(UpdateSetNullOption option) { 
            this.setNullOption = option;
            return this;
        }

        /// <summary>
        /// 设置一个插入或更新 字段的名--值映射。指定是否参数化，是否用于insert 或 update语句
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <param name="paramed"></param>
        /// <param name="updatable"></param>
        /// <param name="insertable"></param>
        /// <returns></returns>
        public override SQLBuilder set(string key, Object val, bool paramed = true, Type type = null, bool updatable = true, bool insertable = true)
        {
            return setCore(key, val, paramed, type, updatable, insertable, null);
        }

        /// <summary>
        /// 使用编排期烘焙的槽位全名写入 set（paramKey 已定，不再用 cl_ 计数起名）。
        /// </summary>
        public SQLBuilder setWithSlot(
            string key,
            object val,
            string staticSlotName,
            bool paramed = true,
            Type type = null,
            bool updatable = true,
            bool insertable = true)
        {
            return setCore(key, val, paramed, type, updatable, insertable, staticSlotName);
        }

        /// <summary>
        /// 兼容：按当前 paraSeed / groupKey 派生 set 槽位名后写入。
        /// </summary>
        public SQLBuilder setWithSlot(
            string key,
            object val,
            int staticSlotId,
            bool paramed = true,
            Type type = null,
            bool updatable = true,
            bool insertable = true)
        {
            var groupKey = current != null ? current.key : "";
            var name = StaticSlotMarks.FormatSetName(paraSeed, groupKey, staticSlotId);
            return setCore(key, val, paramed, type, updatable, insertable, name);
        }

        private StepBuilder setCore(
            string key,
            object val,
            bool paramed,
            Type type,
            bool updatable,
            bool insertable,
            string staticSlotName)
        {
            if (!opened)
            {
                opened = true;
                return this;
            }
            if (paramed && val == null) {
                if (UpdateSetNullOpt == UpdateSetNullOption.IgnoreNull)
                {
                    return this;
                }
                else if (UpdateSetNullOpt == UpdateSetNullOption.AsDBNull) { 
                    paramed = false;
                    val = "null";
                }
            }


            SetFrag field = current.getSetFrag(key);

            if (val == DBNull.Value)
            {
                field.setValue(current.RowIndex, "NULL", type, false, updatable, insertable);
            }
            else {
                field.setValue(current.RowIndex, val, type, paramed, updatable, insertable);
            }

            if (!string.IsNullOrEmpty(staticSlotName) && paramed && val != null && val != DBNull.Value)
            {
                var pair = field.values[current.RowIndex];
                if (pair != null)
                    pair.paramKey = staticSlotName;
            }
                
            if (this.Client != null)
            {
                var ok = Client.fireBuildSetFrag(field, SQLBuilder.Attach(this, materializing: true));
                if (ok == false)
                {
                    return this;
                }
            }
            this.current.set(field);
            return this;
        }
        /// <summary>
        /// 获取当前行设置的字段值。 若不存在则返回null。 若设置了多个值，则会取最后一个设置的值。
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public override object getSetedValue(string fieldName)
        {
            SetFrag field = current.getSetFrag(fieldName,false);
            if (field == null) return null;
            var val= field.getValue(current.RowIndex);
            return val;
        }

        #endregion


        #region merge into 语句的配置
        /// <summary>
        /// 创建一个merge into 语句的构建器。
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="asName"></param>
        /// <returns></returns>
        public override MergeIntoBuilder mergeInto(string tbName,string asName=null)
        {
            var kit=new MergeIntoBuilder(DBLive);
            if (this._printSQL) {
                kit.print(this.onSQLPrint);
            }
            kit.into(tbName,asName);
            return kit;
        }
        /// <summary>
        /// 将来源的from 部分 嵌套一层的 as 名称
        /// </summary>
        /// <param name="asName"></param>
        /// <returns></returns>
        public override SQLBuilder mergeAs(string asName)
        {
            current.mergeAs(asName);
            return this;
        }
        /// <summary>
        /// merge into 语句的on 部分
        /// </summary>
        /// <param name="onPart"></param>
        /// <returns></returns>
        public override SQLBuilder mergeOn(string onPart)
        {
            current.mergeOn(onPart);
            return this;
        }
        /// <summary>
        /// merge into 当不匹配时，是否删除
        /// </summary>
        /// <param name="thenDelete"></param>
        /// <returns></returns>
        public override SQLBuilder mergeDelete(bool thenDelete)
        {
            current.mergeDelete(thenDelete);
            return this;
        }
        #endregion



        #region 多行修改
        /// <summary>
        /// 用来执行的SQL语句。
        /// </summary>
        private List<string> _todoSQLs= new List<string>();
        public List<string> todoSQLs { get { return _todoSQLs; } set { _todoSQLs = value; } }
        /// <summary>
        /// 用于创建 insert into values 多行值的SQL移动到下一行。
        /// </summary>
        /// <returns></returns>
        public override SQLBuilder newRow()
        {
            current.newRow();
            return this;
        }
        /// <summary>
        /// insert into values 多行值的添加本行值。
        /// </summary>
        /// <returns></returns>
        public override SQLBuilder addRow()
        {
            current.addRow();
            return this;
        }
        /// <summary>
        /// 创建SQL语句到语句池中，同时积累参数。
        /// </summary>
        /// <returns></returns>
        public override SQLBuilder addInsert()
        {
            if (EnsureWriteTableName(nameof(addInsert)))
            {
                string sql = current.buildInsert();
                todoSQLs.Add(sql);
            }
            //清理掉创建配置池（无论是否构建成功，避免污染后续轮次）
            current.clearToNext();

            return this;
        }
        /// <summary>
        /// 创建 update SQL语句到语句池中，同时积累参数。
        /// </summary>
        /// <returns></returns>
        public override SQLBuilder addUpdate()
        {
            if (EnsureWriteTableName(nameof(addUpdate)))
            {
                string sql = current.buildUpdate();
                todoSQLs.Add(sql);
            }
            //清理掉创建配置池（无论是否构建成功，避免污染后续轮次）
            current.clearToNext();

            return this;
        }
        /// <summary>
        /// 创建 update from SQL语句到语句池中，同时积累参数。
        /// </summary>
        /// <returns></returns>
        public override SQLBuilder addUpdateFrom()
        {
            if (EnsureWriteTableName(nameof(addUpdateFrom)))
            {
                string sql = current.buildUpdateFrom();
                todoSQLs.Add(sql);
            }
            //清理掉创建配置池（无论是否构建成功，避免污染后续轮次）
            current.clearToNext();

            return this;
        }
        #endregion
    }
}
