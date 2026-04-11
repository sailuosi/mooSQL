using System;
using System.Collections.Generic;
using System.Linq;

using System.Text;
using System.Threading.Tasks;
using mooSQL.data;

namespace mooSQL.linq
{
    /// <summary>
    /// 单层查询的上下文
    /// </summary>
    public class LayerContext
    {
        /// <summary>
        /// 子层级
        /// </summary>
        public List<LayerContext> children = new List<LayerContext>();
        /// <summary>
        /// 根SQL编辑器
        /// </summary>
        public SQLBuilder Root { get; set; }
        /// <summary>
        /// 当前层的SQL编织器
        /// </summary>
        public SQLBuilder Current {  get; set; }
        /// <summary>
        /// 当前层的表信息
        /// </summary>
        public List<OriginTable> OriginTables { get; set; } = new List<OriginTable>();

        private Dictionary<Type,EntityOrigin> entityMap=new Dictionary<Type, EntityOrigin>();

        private Dictionary<string, EntityOrigin> registed=new Dictionary<string, EntityOrigin>();

        /// <summary>
        /// 从另一层上下文复制来源表与实体映射（浅拷贝引用）。
        /// </summary>
        /// <param name="src">源层。</param>
        public void Copy(LayerContext src) { 
            this.OriginTables = src.OriginTables;
            this.entityMap = src.entityMap;
        }

        /// <summary>
        /// 将实体按昵称注册到当前 SQL，并生成 FROM 片段。
        /// </summary>
        public void register(string nick, Type entityType,LayerRunType type) {
            if (registed.ContainsKey(nick)) return;
            var org= entityMap[entityType];

            var sql = org.build(Current.DBLive,type);
            org.nickName = nick;
            org.SQL = sql;
            Current.from(sql);
            registed.Add(nick, org);
        }

        /// <summary>
        /// 将实体以 JOIN 形式注册到当前 SQL。
        /// </summary>
        public void registerJoin(string nick,string joinType, string onPart,Type entityType, LayerRunType type)
        {
            if (registed.ContainsKey(nick)) return;
            var org = entityMap[entityType];
            org.nickName = nick;
            var sql = org.build(Current.DBLive, type);


            if (!string.IsNullOrWhiteSpace(onPart))
            {
                var sqlJoin = string.Format("{0} {1} on {2}", joinType, sql, onPart);
                Current.join(sqlJoin);
                org.SQL = sqlJoin;
            }
            else {
                Current.from(sql);
                org.SQL = sql;
            }
            registed.Add(nick, org);
        }

        /// <summary>
        /// 将实体类型登记为当前层的来源表（去重）。
        /// </summary>
        /// <param name="entityType">实体 CLR 类型。</param>
        /// <param name="nick">表别名，可选。</param>
        public void suck(Type entityType,string nick=null) {
            if (entityMap.ContainsKey(entityType)) {
                return;
            }
            var tbinfo = Current.DBLive.client.EntityCash.getEntityInfo(entityType);
            var tar = new EntityOrigin();
            tar.EntityInfo = tbinfo;
            tar.EntityType= entityType;
            tar.NickName = nick;

            OriginTables.Add(tar);
            entityMap[entityType] = tar;
        }
        /// <summary>
        /// 获取昵称或者null
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public string getNick(Type entity) {
            if (entityMap.ContainsKey(entity))
            {
                return entityMap[entity].NickName;
            }
            return null;
        }

        /// <summary>
        /// 准备执行运行前的校验工作
        /// </summary>
        public void PrepareRun(LayerRunType type) { 
            this.checkRun(type);
            if (this.children.Count > 0) {
                foreach (var child in this.children) { 
                    child.PrepareRun(type);
                }
            }
        }

        private void checkRun(LayerRunType type)
        {
            
            if (type== LayerRunType.select && Current.FromCount == 0)
            {
                if (this.OriginTables.Count>0)
                {
                    var ot=OriginTables.First();
                    var tbinfo = Root.DBLive.client.EntityCash.getEntityInfo(ot.EntityType);
                    var fro= ot.build(Root.DBLive,type);
                    Current.from(fro);
                }
            }

            if (type == LayerRunType.update && string.IsNullOrWhiteSpace( Current.current.tableName))
            {
                if (this.OriginTables.Count > 0)
                {
                    var ot = OriginTables.First();
                    var tbinfo = Root.DBLive.client.EntityCash.getEntityInfo(ot.EntityType);
                    var fro = ot.build(Root.DBLive, type);
                    Current.setTable(fro);
                }
            }
            if (type == LayerRunType.delete && string.IsNullOrWhiteSpace(Current.current.tableName))
            {
                if (this.OriginTables.Count > 0)
                {
                    var ot = OriginTables.First();
                    var tbinfo = Root.DBLive.client.EntityCash.getEntityInfo(ot.EntityType);
                    var fro = ot.build(Root.DBLive, type);
                    Current.setTable(fro);
                }
            }
        }


    }

    /// <summary>
    /// 单层编译时的语句类型（SELECT/UPDATE/DELETE）。
    /// </summary>
    public enum LayerRunType { 
        /// <summary>未指定。</summary>
        none=0,
        /// <summary>查询。</summary>
        select=1,
        /// <summary>更新。</summary>
        update=2, 
        /// <summary>删除。</summary>
        delete=3,
    }
}
