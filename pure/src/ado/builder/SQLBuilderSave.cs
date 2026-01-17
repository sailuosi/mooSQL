// 基础功能说明：

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    // 插入和修改的方面
    public partial class SQLBuilder
    {

        /// <summary>
        /// 设置 update /delete 语句的目标 表。
        /// </summary>
        /// <param name="tbName"></param>
        /// <returns></returns>

        public SQLBuilder setTable(string tbName)
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
        public SQLBuilder configSetNull(UpdateSetNullOption option) { 
            this.setNullOption = option;
            return this; 
        
        }

        /// <summary>
        /// 设置一个字符串值，并指定其最大长度，多余的会被自动截断。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="maxLength"></param>
        /// <returns></returns>
        public SQLBuilder set(string key, string value, int maxLength) {
            if (value != null && value.Length > maxLength) { 
                value= value.Substring(0, maxLength);
            }
            return set(key, value);
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
        public SQLBuilder set(string key, Object val, bool paramed = true, Type type = null, bool updatable = true, bool insertable = true)
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
                
            if (this.Client != null)
            {
                var ok = Client.fireBuildSetFrag(field, this);
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
        public object getSetedValue(string fieldName)
        {
            SetFrag field = current.getSetFrag(fieldName,false);
            if (field == null) return null;
            var val= field.getValue(current.RowIndex);
            return val;
        }
        /// <summary>
        /// 将字段设置为null。
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public SQLBuilder setToNull(string fieldName)
        {
            return set(fieldName, "NULL",false);
        }

        /// <summary>
        /// 参数化的插入值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <returns></returns>

        public SQLBuilder setI(string key, Object val)
        {
            this.set(key, val, true, null, false, true);
            return this;
        }
        /// <summary>
        /// 设置一个用于 insert的 字段的名--值映射。 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <param name="paramed"></param>
        /// <returns></returns>
        public SQLBuilder setI(string key, Object val, bool paramed)
        {
            this.set(key, val, paramed, null, false, true);
            return this;
        }
        /// <summary>
        /// 设置一个用于 update 的 字段的名--值映射。 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public SQLBuilder setU(string key, Object val)
        {
            this.set(key, val, true, null, true, false);
            return this;
        }
        /// <summary>
        /// 设置一个用于 update 的 字段的名--值映射。 并指定是否参数化
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <param name="paramed"></param>
        /// <returns></returns>
        public SQLBuilder setU(string key, Object val, bool paramed)
        {
            this.set(key, val, paramed, null, true, false);
            return this;
        }
        #endregion


        #region merge into 语句的配置
        /// <summary>
        /// 创建一个merge into 语句的构建器。
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="asName"></param>
        /// <returns></returns>
        public MergeIntoBuilder mergeInto(string tbName,string asName=null)
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
        public SQLBuilder mergeAs(string asName)
        {
            current.mergeAs(asName);
            return this;
        }
        /// <summary>
        /// merge into语句的来源表。使用更符合SQL语句结构的写法，即using (select ...) as asName.
        /// </summary>
        /// <param name="asName"></param>
        /// <param name="buildSelect"></param>
        /// <returns></returns>
        public SQLBuilder mergeUsing(string asName,Action<SQLBuilder> buildSelect)
        {
            current.mergeAs(asName);
            buildSelect(this);
            return this;
        }
        /// <summary>
        /// merge into 语句的来源表。使用更符合SQL语句结构的写法，即using tabname as asName.
        /// </summary>
        /// <param name="asName"></param>
        /// <param name="tabname"></param>
        /// <returns></returns>
        public SQLBuilder mergeUsing(string asName, string tabname)
        {
            current.mergeAs(asName);
            from(tabname);
            return this;
        }

        /// <summary>
        /// merge into 语句的on 部分
        /// </summary>
        /// <param name="onPart"></param>
        /// <returns></returns>
        public SQLBuilder mergeOn(string onPart)
        {
            current.mergeOn(onPart);
            return this;
        }
        /// <summary>
        /// merge into 当不匹配时，是否删除
        /// </summary>
        /// <param name="thenDelete"></param>
        /// <returns></returns>
        public SQLBuilder mergeDelete(bool thenDelete)
        {
            current.mergeDelete(thenDelete);
            return this;
        }
        #endregion



        #region 多行修改
        /// <summary>
        /// 用来执行的SQL语句。
        /// </summary>
        public List<string> todoSQLs = new List<string>();
        /// <summary>
        /// 用于创建 insert into values 多行值的SQL移动到下一行。
        /// </summary>
        /// <returns></returns>
        public SQLBuilder newRow()
        {
            current.newRow();
            return this;
        }
        /// <summary>
        /// insert into values 多行值的添加本行值。
        /// </summary>
        /// <returns></returns>
        public SQLBuilder addRow()
        {
            current.addRow();
            return this;
        }
        /// <summary>
        /// 创建SQL语句到语句池中，同时积累参数。
        /// </summary>
        /// <returns></returns>
        public SQLBuilder addInsert()
        {
            string sql = current.buildInsert();
            todoSQLs.Add(sql);
            //清理掉创建配置池  
            current.clearToNext();

            return this;
        }
        /// <summary>
        /// 创建 update SQL语句到语句池中，同时积累参数。
        /// </summary>
        /// <returns></returns>
        public SQLBuilder addUpdate()
        {
            string sql = current.buildUpdate();
            todoSQLs.Add(sql);
            //清理掉创建配置池  
            current.clearToNext();

            return this;
        }
        /// <summary>
        /// 创建 update from SQL语句到语句池中，同时积累参数。
        /// </summary>
        /// <returns></returns>
        public SQLBuilder addUpdateFrom()
        {
            string sql = current.buildUpdateFrom();
            todoSQLs.Add(sql);
            //清理掉创建配置池  
            current.clearToNext();

            return this;
        }
        #endregion
    }
}