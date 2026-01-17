using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.Mapping
{
    /// <summary>
    /// 实体类信息解析工厂
    /// </summary>
    public class BaseEntityAnalyseFactory : IEntityAnalyseFactory
    {
        /// <summary>
        /// 已注册的解析器，先进先出
        /// </summary>
        protected List<IEntityAnalyser> entityAnalysers = new List<IEntityAnalyser>();
        /// <summary>
        /// 失败回滚的解析器，后进先出原则。
        /// </summary>
        protected List<IEntityAnalyser> failbackAnalysers = new List<IEntityAnalyser>();
        /// <summary>
        /// 解析结果的缓存
        /// </summary>
        protected ConcurrentDictionary<Type,EntityInfo> parsedCache = new ConcurrentDictionary<Type,EntityInfo>();


        /// <summary>
        /// 注册解析器
        /// </summary>
        /// <param name="entityAnalyser"></param>
        /// <returns></returns>
        public virtual IEntityAnalyseFactory register(IEntityAnalyser entityAnalyser)
        {
            if (entityAnalyser.FailBacked)
            {
                failbackAnalysers.Add(entityAnalyser);
            }
            else {
                entityAnalysers.Add(entityAnalyser);
            }
            return this;
        }
        /// <summary>
        /// 执行解析
        /// </summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public virtual EntityInfo doAnalyse(Type entityType)
        {
            EntityInfo tar;
            var goted = parsedCache.TryGetValue(entityType,out tar);
            if (goted) { 
                return tar;
            }

            tar= this.askAnalyse(entityType);
            if (tar != null) { 
                parsedCache.TryAdd(entityType,tar);
            }
            return tar;
        }
        /// <summary>
        /// 取得信息时，是否中断。
        /// </summary>
        protected virtual bool breakOnAnalysed
        {
            get {
                return true;
            }
        }

        protected virtual EntityInfo askAnalyse(Type Entity)
        {
            EntityInfo entityInfo = null;
            for (var i = 0; i < entityAnalysers.Count; i++) { 
                var he= entityAnalysers[i];
                if (!he.CanParse(Entity)) {
                    continue;
                }

                entityInfo = he.ParseEntity(Entity, entityInfo);
                if (entityInfo != null && entityInfo.Columns.Count>0) {
                    //不为null，有解析的列，视为成功，不再访问下一个
                    if (breakOnAnalysed) { 
                        return entityInfo;
                    }
                }
            }

            if (entityInfo != null) {
                return entityInfo;
            }
            //未找到，尝试使用回滚访问器

            for (var i = failbackAnalysers.Count; i >0; i--)
            {
                var he = failbackAnalysers[i-1];
                if (!he.CanParse(Entity))
                {
                    continue;
                }

                entityInfo = he.ParseEntity(Entity, entityInfo);
                if (entityInfo != null && entityInfo.Columns.Count > 0)
                {
                    //不为null，有解析的列，视为成功，不再访问下一个
                    if (breakOnAnalysed)
                    {
                        return entityInfo;
                    }
                }
            }
            if (entityInfo != null)
            {
                return entityInfo;
            }
            return new EntityInfo();
            throw new Exception("实体类信息解析失败，可能尚未支持或缺少解析器，类型："+Entity.FullName);
        }


    }
}
