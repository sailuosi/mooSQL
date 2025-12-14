using mooSQL.data;
using mooSQL.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.config
{
    public static class FastConfigExtensions
    {
        /// <summary>
        /// 用户侧的数据库配置转换为内部使用的配置。
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        public static DataBase asDataBase(this DBPosition db) { 
            
            var res=new DataBase();
            res.dbType= db.DbType.asDBType();
            res.DBConnectStr= db.ConnectString;
            res.index =db.Position;
            res.name =db.Name;
            res.version =db.Version;
            if (db.VersionNumber != null) {
                res.versionNumber = db.VersionNumber.Value;
            }
            
            if (!string.IsNullOrWhiteSpace(db.Version) && db.VersionNumber == null) { 
                res.versionNumber = TypeAs.asDouble(db.Version, 0.0);
            }
            if (!string.IsNullOrWhiteSpace(db.Edition)) {
                res.edition = db.Edition;
            }
            if (db.EditionNumber > 0) {
                res.editionNumber = db.EditionNumber;
            }
            if (db.WatchSQL == true) {
                res.watchSQL = true;
            }
            if (db.MinTimeSpan > 0) {
                res.minTimeSpan = db.MinTimeSpan;
            }
            return res;
        }
        /// <summary>
        /// 读取数据库配置
        /// </summary>
        /// <param name="dbTypeName"></param>
        /// <returns></returns>
        public static DataBaseType asDBType(this string dbTypeName) { 
            
            var name= dbTypeName.Trim();
            DataBaseType res =DataBaseType.None;
            if (Enum.TryParse(dbTypeName, true, out res)) { 
                return res;
            }
            return res;
        }
        /// <summary>
        /// 添加连接
        /// </summary>
        /// <param name="cash"></param>
        /// <param name="positions"></param>
        public static void addConfig(this DBInsCash cash, List<DBPosition> positions) {
            if (positions == null) { return; }
            for (var i=0;i<positions.Count;i++) {
                var pos = positions[i];

                var tar= pos.asDataBase();

                cash.addDataBase(tar.index, tar);
            }
        }

    }
}
