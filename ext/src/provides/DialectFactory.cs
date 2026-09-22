




using System.Collections.Generic;
using System.Xml;
using System;

using System.Collections.Concurrent;
using mooSQL.linq;


namespace mooSQL.data
{
    /// <summary>
    /// ���Թ��������ݷ������ͣ��������ԡ�
    /// </summary>
    public class DialectFactory : DialectFactoryBase
    {
        /// <summary>
        /// ��ʼ����Ĭ��ע���Դ���10������
        /// </summary>
        public DialectFactory() {

            this.useDialect(DataBaseType.MySQL, () => new MySQLDialect());
            this.useDialect(DataBaseType.OceanBase, () => new OBMySQLDialect() );
            this.useDialect(DataBaseType.MSSQL, () => new MSSQLDialect() );
            this.useDialect(DataBaseType.Oracle, () => new OracleDialect() );
            this.useDialect(DataBaseType.PostgreSQL, () => new NpgsqlDialect() );
            this.useDialect(DataBaseType.CrateDB, () => new CrateDBDialect());
            this.useDialect(DataBaseType.Taos, () => new TaosDialect() );
            this.useDialect(DataBaseType.GBase8a, () => new GBase8aDialect() );
            this.useDialect(DataBaseType.SQLite, () => new SQLiteDialect() );
            this.useDialect(DataBaseType.Oscar, () => new OscarDialect() );
            this.useDialect(DataBaseType.DM, () => new DMDialect());
#if !NET451
            this.useDialect(DataBaseType.KingBaseR3, () => new KingBaseDialect());
            this.useDialect(DataBaseType.KingBaseR6, () => new KingBaseDialect());
#endif
#if NET6_0_OR_GREATER
            this.useDialect(DataBaseType.DuckDB, () => new DuckDBDialect() );
            this.useDialect(DataBaseType.ClickHouse, () => new ClickHouseDialect());
#endif
        }

        /// <summary>
        /// �������ݿ����ã�������UCML��xml���÷���ʵ�֡�
        /// </summary>
        /// <param name="dBIns"></param>
        /// <returns></returns>
        public override Dictionary<int, DataBase> loadDBConfig(DBInsCash dBIns)
        {
            try
            {


                if (string.IsNullOrWhiteSpace(dBIns.configPath))
                {
                    return null;
                }
                var doc = new XmlDocument();

                doc.Load(dBIns.configPath);

                var res = new Dictionary<int, DataBase>();

                if (doc.DocumentElement == null) {
                    return res;
                }
                var xmlNodeList = doc.DocumentElement.SelectNodes("//DBConnEx");
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