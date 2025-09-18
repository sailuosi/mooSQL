using System;
using System.Collections.Generic;
using System.Linq;

using System.Text;


namespace mooSQL.data
{
    /// <summary>
    /// Clip的泛型版本，用于构造特定类型的更新、删除语句。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public partial class SQLClip<T> : SQLClip
    {
        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="DB"></param>
        public SQLClip(DBInstance DB) : base(DB) { }
        /// <summary>
        /// 构造，用于复制一个Clip实例。
        /// </summary>
        /// <param name="clip"></param>
        public SQLClip(SQLClip clip) : base(clip)
        {


        }
        /// <summary>
        /// 设置翻页参数
        /// </summary>
        /// <param name="pageSize"></param>
        /// <param name="pageNum"></param>
        /// <returns></returns>
        public SQLClip<T> setPage(int pageSize, int pageNum)
        {

            Context.Builder.setPage(pageSize, pageNum);
            return this;
        }


        /// <summary>
        /// 查询出唯一结果，自动根据字段数量自动选择查询方法。
        /// </summary>
        /// <returns></returns>
        public T queryUnique()
        {
            if (this.Context.FieldCount == 1)
            {
                return Context.Builder.queryScalar<T>();
            }
            else
            {
                return Context.Builder.queryUnique<T>();
            }
        }
        /// <summary>
        /// 查询出列表结果，自动根据字段数量自动选择查询方法。
        /// </summary>
        /// <returns></returns>
        public IEnumerable<T> queryList()
        {
            if (this.Context.FieldCount == 1)
            {
                return Context.Builder.queryFirstField<T>();
            }
            else
            {
                return Context.Builder.query<T>();
            }
        }
        /// <summary>
        /// 查询出分页结果。
        /// </summary>
        /// <returns></returns>
        public PageOutput<T> queryPage()
        {

            return Context.Builder.queryPaged<T>();
        }
    }

}
