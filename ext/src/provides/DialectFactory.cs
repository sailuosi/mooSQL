




using System.Collections.Generic;
using System.Xml;
using System;
using mooSQL.linq;


namespace mooSQL.data
{
    /// <summary>
    /// 方言工厂，依据方言类型，产生方言。
    /// </summary>
    public class DialectFactory : IDialectFactory
    {
        /// <summary>
        /// 获取方言
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        public Dialect getDialect(DataBase db)
        {

            var dbType = db.dbType;
            Dialect res = null;
            if (dbType == DataBaseType.MySQL)
            {
                res = new MySQLDialect();
            }
            else if (dbType == DataBaseType.OceanBase)
            {
                res = new OBMySQLDialect();
            }
            else if (dbType == DataBaseType.MSSQL)
            {
                res = new MSSQLDialect();
            }
            else if (dbType == DataBaseType.Oracle)
            {
                res = new OracleDialect();
            }
            else if (dbType == DataBaseType.PostgreSQL)
            {
                res = new NpgsqlDialect();
            }
            else if (dbType == DataBaseType.Taos)
            {
                res = new TaosDialect();
            }
            else if (dbType == DataBaseType.GBase8a)
            {
                res = new GBase8aDialect();
            }
            else if (dbType == DataBaseType.SQLite)
            {
                res = new SQLiteDialect();
            }
            else if (dbType == DataBaseType.Oscar)
            {
                res = new OscarDialect();
            }

            res.db = db;
            //检查SQL模型转换器，没有时，置入默认转译器
            if (res.clauseTranslator == null) {
                res.clauseTranslator = new ClauseTranslateVisitor(res);
            }

            return res;
        }



        /// <summary>
        /// 加载数据库配置，这里是UCML的xml配置方言实现。
        /// </summary>
        /// <param name="dBIns"></param>
        /// <returns></returns>
        public Dictionary<int, DataBase> loadDBConfig(DBInsCash dBIns)
        {
            try
            {


                if (string.IsNullOrWhiteSpace(dBIns.configPath))
                {
                    return null;
                }
                XmlDataDocument xmlDataDocument = new XmlDataDocument();

                xmlDataDocument.Load(dBIns.configPath);

                var res = new Dictionary<int, DataBase>();


                var xmlNodeList = xmlDataDocument.DocumentElement.SelectNodes("//DBConnEx");
                if (xmlNodeList != null)
                {
                    for (int num = 0; num < xmlNodeList.Count; num++)
                    {
                        XmlNode xmlNode = xmlNodeList[num];
                        if (xmlNode != null)
                        {
                            DataBase item = new DataBase();
                            var attrType = xmlNode.Attributes["DBType"];
                            if (attrType != null)
                            {
                                var dbtype = attrType.Value;
                                item.setDBtype(dbtype);
                            }
                            item.DBConnectStr = xmlNode.InnerText;
                            item.index = num;
                            res.Add(num, item);
                        }

                    }
                }
                return res;
            }
            catch (Exception ex)
            {
                return null;
            }
        }




    }
}