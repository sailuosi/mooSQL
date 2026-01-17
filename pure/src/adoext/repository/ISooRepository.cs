using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 平平无奇的抽象仓储接口
    /// </summary>
    public interface ISooRepository<T>
    {


        #region 删除
        bool Delete(T deleteObj);
        int Delete(IEnumerable<T> deleteObjs);
        bool DeleteById<K>(K id);
        int DeleteByIds<K>(IEnumerable<K> ids);
        #endregion

        #region 查询
        int Count(Expression<Func<T, bool>> whereExpression);
        /// <summary>
        /// 查询一条记录
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        T GetById<K>(K id);
        /// <summary>
        /// 查询一组记录
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <param name="ids"></param>
        /// <returns></returns>
        List<T> GetByIds<K>(List<K> ids);
        /// <summary>
        /// 获取所有
        /// </summary>
        /// <returns></returns>
        List<T> GetList();
        /// <summary>
        /// 按条件获取所有记录
        /// </summary>
        /// <param name="whereExpression"></param>
        /// <returns></returns>
        List<T> GetList(Expression<Func<T, bool>> whereExpression);

        T GetFirst(Expression<Func<T, bool>> whereExpression);
        #endregion


        #region 插入
        bool Insert(T insertObj);

        int InsertRange(IEnumerable<T> insertObjs);

        #endregion

        #region 更新
        bool Update(T updateObj);


        int UpdateRange(IEnumerable<T> updateObjs);

        int UpdateRange(T[] updateObjs);

        bool Update(Expression<Func<T, T>> columns, Expression<Func<T, bool>> whereExpression);
        #endregion
    }
}
