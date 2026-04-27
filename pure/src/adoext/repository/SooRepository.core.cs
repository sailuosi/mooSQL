using mooSQL.data.model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace mooSQL.data
{

    public partial class SooRepository<T> : ISooRepository<T>
    {

        private EntityInfo? _EnInfo;

        private string tbname;
        /// <summary>
        /// 实体信息对象
        /// </summary>
        public EntityInfo En
        {
            get {
                if (_EnInfo != null) {
                    return _EnInfo;
                }
                _EnInfo = this.DBLive.Client.EntityCash.getEntityInfo(typeof(T));
                return _EnInfo;
            }
        }
        //var en = kit.DBLive.client.EntityCash.getEntityInfo(typeof(T));

        private int insertInner(T entity,SQLBuilder kit)
        {
            int cc = 0;
            OnBeforeSave(entity);
            var res = Translator.prepareInsert(kit, entity, typeof(T), null, tryTableNameLoader(tbname));
            if (res.Status)
            {
                cc= kit.doInsert();
                OnAfterSave(entity, cc);
            }
            kit.Client.Loggor.LogError(res.Message);
            cc = -1;
            return cc;
        }
        private int updateInner(T entity, SQLBuilder kit)
        {
            int cc = 0;
            OnBeforeSave(entity);
            var res = Translator.prepareUpdate(kit, entity, typeof(T), null, tryTableNameLoader(tbname));
            if (res.Status)
            {
                cc = kit.doUpdate();
                OnAfterSave(entity, cc);
            }
            kit.Client.Loggor.LogError(res.Message);
            cc = -1;
            return cc;
        }
        private int SaveInner( T entity, SQLBuilder builder)
        {
            if (entity == null)
            {
                return 0;
            }


            if (En.Insertable == false && En.Updatable == false)
            {
                return 0;
            }
            //主键短路检测，未设置主键的，直接插入
            var pks = En.GetPK();
            if (pks.Count == 0)
            {
                throw new Exception("无主键定义时，无法自动保存！");
            }
            foreach (var pk in pks)
            {
                if (pk.PropertyInfo.GetValue(entity) == null)
                {
                    //主键不全时，走插入逻辑
                    return insertInner(entity,builder);
                }
            }
            //检查是否存在
            var ck = builder.DBLive.useSQL();
            ck.from(tbname.HasText() ? tbname : En.DbTableName);
            Translator.setPKWhere(ck, entity, En);
            var cc = ck.count();
            if (cc > 0 && En.Updatable != false)
            {
                return updateInner( entity, builder);
            }
            else if (En.Insertable != false)
            {
                return insertInner( entity, builder);
            }
            return 0;
        }
        private static Func<string> tryTableNameLoader(string tbname)
        {
            if (!tbname.HasText())
                return null;
            return () => tbname;
        }
    }
}
