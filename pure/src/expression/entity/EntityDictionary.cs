using mooSQL.data.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    public class EntityDictionary
    {
        public Type entityType;

        public EntityDictionary(Type entityType) { 
            this.entityType = entityType;


        }

        public Dictionary<string, EntityColumn> Fields { get; private set; }
        public EntityInfo EntityInfo { get; private set; }

        public void init(IEntityAnalyseFactory parser) { 
        
            var entity= parser.doAnalyse(entityType);
            if (entity == null) {
                throw new Exception("实体类的数据库映射信息解析失败！");
            }
            this.EntityInfo = entity;
            Fields = new Dictionary<string, EntityColumn>();
            foreach (var col in entity.Columns) {
                Fields[col.PropertyInfo.Name] = col;
            }
        }
    }
}
