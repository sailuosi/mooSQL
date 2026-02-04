using mooSQL.utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    public partial class DDLBuilder
    {

        /// <summary>
        /// 获取自增字段列表
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public virtual List<string> loadTableIdentities(string tableName)
        {
            return (from it in SQLVerse.GetDbColumnsByTableName(tableName)
                    where it.IsIdentity
                    select it into p
                    select p.Name).ToList();
        }

        /// <summary>
        /// 获取主键列表
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public virtual List<string> loadTablePKs(string tableName)
        {
            return (from it in SQLVerse.GetDbColumnsByTableName(tableName)
                    where it.IsPrimary
                    select it into p
                    select p.Name).ToList();
        }
        /// <summary>
        /// 是否存在该视图
        /// </summary>
        /// <param name="viewName"></param>
        /// <returns></returns>
        public virtual bool hasView(string viewName)
        {
            //Check.NotEmpty(viewName, "viewName");
            return SQLVerse.GetDbViewList()?.Any((DbTableInfo it) => it.Name.Equals(viewName, StringComparison.CurrentCultureIgnoreCase)) ?? false;
        }
        /// <summary>
        /// 检查是否有某个表
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public virtual bool hasTable(string tableName)
        {
            var cmd= SQLVerse.buildHasTable(tableName);
            if (cmd != null) { 
                var cc= DBLive.ExeQueryScalar<int>(cmd);
                return cc > 0;
            }
            //Check.NotEmpty(tableName, "tableName");
            return SQLVerse.GetDbTableList()?.Any((DbTableInfo it) => it.Name.Equals(tableName, StringComparison.CurrentCultureIgnoreCase)) ?? false;
        }
        /// <summary>
        /// 加载数据库的所有表信息
        /// </summary>
        /// <returns></returns>
        public virtual List<DbTableInfo> loadTableList()
        {
            return SQLVerse.GetDbTableList();
        }

        public virtual List<DbTableInfo> loadTableList(string name)
        {
            return loadTableList((kit) => {
                kit.whereLike("t.Name",name);
            });
        }

        public virtual List<DbTableInfo> loadTableList(Action<SQLBuilder> doFilter)
        {

            var kit = DBLive.useSQL();

            kit.from(string.Format("({0}) as t", this.SQLVerse.GetTableInfoListSql));
            doFilter(kit);

            return kit.query<DbTableInfo>().ToList();
        }

        /// <summary>
        /// 检查是否存在某个字段
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public virtual bool hasColumn(string tableName, string columnName)
        {
            bool flag = hasTable(tableName);
            //Check.CheckCondition(!flag, "Table {0} does not exist", tableName);
            List<DbColumnInfo> columnInfosByTableName = SQLVerse.GetDbColumnsByTableName(tableName);
            if (columnInfosByTableName.Count == 0)
            {
                return false;
            }
            return columnInfosByTableName.Any((DbColumnInfo it) => it.Name.Equals(columnName, StringComparison.CurrentCultureIgnoreCase));
        }

        /// <summary>
        /// 加载字段信息列表
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public List<DbColumnInfo> loadColumns(string tableName)
        {
            return SQLVerse.GetDbColumnsByTableName(tableName);
        }

        /// <summary>
        /// 是否存在主键
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public virtual bool isPrimaryKey(string tableName, string columnName)
        {
            bool flag = hasTable(tableName);
            //Check.CheckCondition(!flag, "Table {0} does not exist", tableName);
            List<DbColumnInfo> columnInfosByTableName = SQLVerse.GetDbColumnsByTableName(tableName);
            if (columnInfosByTableName.Count == 0)
            {
                return false;
            }
            return columnInfosByTableName.Any((DbColumnInfo it) => it.IsPrimary && it.Name.Equals(columnName, StringComparison.CurrentCultureIgnoreCase));
        }
        /// <summary>
        /// 是否是自增列
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public virtual bool isIdentity(string tableName, string columnName)
        {
            bool flag = hasTable(tableName);
            //Check.CheckCondition(!flag, "Table {0} does not exist", tableName);
            List<DbColumnInfo> columnInfosByTableName = SQLVerse.GetDbColumnsByTableName(tableName);
            if (columnInfosByTableName.Count == 0)
            {
                return false;
            }
            return columnInfosByTableName.Any((DbColumnInfo it) => it.IsIdentity = it.Name.Equals(columnName, StringComparison.CurrentCultureIgnoreCase));
        }
        /// <summary>
        /// 是否存在约束
        /// </summary>
        /// <param name="constraintName"></param>
        /// <returns></returns>
        public virtual bool hasConstraint(string constraintName)
        {
            var res = DBLive.ExeQueryScalar<int>("select  object_id('" + constraintName + "')", new Paras()) > 0;
            return res;
        }
        /// <summary>
        /// 是否拥有表权限
        /// </summary>
        /// <returns></returns>
        public virtual bool hasSystemTablePermissions()
        {
            string checkSystemTablePermissionsSql = SQLPit.CheckSystemTablePermissionsBy();
            try
            {

                DBLive.ExeNonQuery(checkSystemTablePermissionsSql, null);

                return true;
            }
            catch
            {
                return false;
            }
        }


        /// <summary>
        /// 该表是否存在中文名
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public virtual bool hasTableCaption(string tableName)
        {
            string sQL = SQLPit.IsAnyTableCaptionBy(tableName);
            DataTable dataTable = DBLive.ExeQuery(sQL);
            return dataTable.Rows != null && dataTable.Rows.Count > 0;
        }
        /// <summary>
        /// 是否存在该索引
        /// </summary>
        /// <param name="indexName"></param>
        /// <returns></returns>
        public virtual bool hasIndex(string indexName)
        {
            string sQL = SQLPit.IsAnyIndexBy(indexName);
            return DBLive.ExeQueryScalar<int>(sQL, new Paras()) > 0;
        }
        /// <summary>
        /// 创建表
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        public virtual string toCreateTable(string tableName, List<DbColumnInfo> columns)
        {
            List<string> list = new List<string>();
            //Check.CheckCondition(columns.IsNullOrEmpty(), "No columns found ");
            foreach (DbColumnInfo column in columns)
            {
                string translationTableName = SQLPit.wrapField(column.Name);
                string fullDataType = column.DbTypeTextFull;
                string text = (column.IsNullable ? SQLPit.CreateTableNullBy() : SQLPit.CreateTableNotNullBy());
                string text2 = null;
                string text3 = (column.IsIdentity ? SQLPit.getTableAutoIdSQL() : null);
                string item = SQLPit.CreateTableColumnBy(translationTableName, fullDataType, null, text, text2, text3);
                list.Add(item);
            }
            return SQLPit.CreateTableBy(SQLPit.wrapTable(tableName), string.Join(",\r\n", list));
        }
        /// <summary>
        /// 执行表创建
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="columns"></param>
        /// <param name="isCreatePrimaryKey"></param>
        /// <returns></returns>
        public virtual bool doCreateTable(string tableName, List<DbColumnInfo> columns, bool isCreatePrimaryKey = true)
        {
            string createTableSql = toCreateTable(tableName, columns);
            DBLive.ExeNonQuery(createTableSql, null);
            if (isCreatePrimaryKey)
            {
                List<DbColumnInfo> list = columns.Where((DbColumnInfo it) => it.IsPrimary).ToList();
                if (list.Count > 1)
                {
                    doAddPrimaryKeys(tableName, list.Select((DbColumnInfo it) => it.Name).ToArray());
                }
                else
                {
                    foreach (DbColumnInfo item in list)
                    {
                        doAddPrimaryKey(tableName, item.Name);
                    }
                }
            }
            return true;
        }
        /// <summary>
        /// 添加列
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="columnInfo"></param>
        /// <returns></returns>
        public virtual string toAddColumn(string tableName, DbColumnInfo columnInfo)
        {
            string translationColumnName = SQLPit.wrapField(columnInfo.Name);
            tableName = SQLPit.wrapTable(tableName);
            string fullDataType = columnInfo.DbTypeTextFull;
            string text = (columnInfo.IsNullable ? SQLPit.CreateTableNullBy() : SQLPit.CreateTableNotNullBy());
            string text2 = null;
            string text3 = null;
            return SQLPit.AddColumnToTableBy(tableName, translationColumnName, fullDataType, null, text, text2, text3);
        }
        /// <summary>
        /// 执行列添加
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="columnInfo"></param>
        /// <returns></returns>
        public virtual bool doAddColumn(string tableName, DbColumnInfo columnInfo)
        {
            string addColumnSql = toAddColumn(tableName, columnInfo);
            DBLive.ExeNonQuery(addColumnSql, null);
            return true;
        }


        /// <summary>
        /// 更新列
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="columnInfo"></param>
        /// <returns></returns>
        public virtual string toUpdateColumn(string tableName, DbColumnInfo columnInfo)
        {
            //name = builder.DataContext.DB.dialect.clauseTranslator.TranslateValue(name, ConvertType.NameToQueryField);
            string translationColumnName = SQLPit.wrapField(columnInfo.Name);
            tableName = SQLPit.wrapTable(tableName);
            string fullDataType = columnInfo.DbTypeTextFull;
            string text = (columnInfo.IsNullable ? SQLPit.CreateTableNullBy() : SQLPit.CreateTableNotNullBy());
            string text2 = null;
            string text3 = null;
            return SQLPit.AlterColumnToTableby(tableName, translationColumnName, fullDataType, null, text, text2, text3);
        }
        /// <summary>
        /// 执行更新列
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        public virtual bool doUpdateColumn(string tableName, DbColumnInfo column)
        {
            string updateColumnSql = toUpdateColumn(tableName, column);
            DBLive.ExeNonQuery(updateColumnSql, null);
            return true;
        }
        /// <summary>
        /// 添加主键
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public virtual string toAddPrimaryKey(string tableName, string columnName)
        {
            string arg = "PK_" + tableName + "_" + columnName;
            tableName = SQLPit.wrapTable(tableName);
            columnName = SQLPit.wrapTable(columnName);
            return SQLPit.AddPrimaryKeyBy(tableName, arg, columnName);
        }
        /// <summary>
        /// 执行添加主键
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public virtual bool doAddPrimaryKey(string tableName, string columnName)
        {
            string addPrimaryKeySql = toAddPrimaryKey(tableName, columnName);
            DBLive.ExeNonQuery(addPrimaryKeySql, null);
            return true;
        }
        /// <summary>
        /// 添加联合主键
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="columnNames"></param>
        /// <returns></returns>
        public virtual string toAddPrimaryKeys(string tableName, string[] columnNames)
        {
            string arg = "PK_" + tableName + "_" + string.Join("_", columnNames);
            string arg2 = string.Join(",", columnNames);
            tableName = SQLPit.wrapTable(tableName);
            return SQLPit.AddPrimaryKeyBy(tableName, arg, arg2);
        }
        /// <summary>
        /// 执行添加联合主键
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="columnNames"></param>
        /// <returns></returns>
        public virtual bool doAddPrimaryKeys(string tableName, string[] columnNames)
        {
            string addPrimaryKeysSql = toAddPrimaryKeys(tableName, columnNames);
            DBLive.ExeNonQuery(addPrimaryKeysSql, null);
            return true;
        }


        /// <summary>
        /// 获取字段的注释SQL
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="tableName"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public virtual string toAddColumnCaption(string columnName, string tableName, string description)
        {
            return SQLPit.AddColumnCaptionBy(columnName, tableName, description);
        }
        /// <summary>
        /// 添加字段注释
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="tableName"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public virtual bool doAddColumnCaption(string columnName, string tableName, string description)
        {
            string addColumnRemarkSql = toAddColumnCaption(columnName, tableName, description);
            DBLive.ExeNonQuery(addColumnRemarkSql, null);
            return true;
        }
        /// <summary>
        /// 删除字段注释
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public virtual string toDeleteColumnCaption(string columnName, string tableName)
        {
            return SQLPit.DeleteColumnCaptionBy(columnName, tableName);
        }
        /// <summary>
        /// 删除字段注释
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public virtual bool doDeleteColumnCaption(string columnName, string tableName)
        {
            string deleteColumnRemarkSql = toDeleteColumnCaption(columnName, tableName);
            DBLive.ExeNonQuery(deleteColumnRemarkSql, null);
            return true;
        }
        /// <summary>
        /// 添加表注释
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public virtual string toAddTableCaption(string tableName, string description)
        {
            return SQLPit.AddTableCaptionBy(tableName, description);
        }
        /// <summary>
        /// 添加表注释
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public virtual bool doAddTableCaption(string tableName, string description)
        {
            string addTableRemarkSql = toAddTableCaption(tableName, description);
            DBLive.ExeNonQuery(addTableRemarkSql, null);
            return true;
        }

        /// <summary>
        /// 创建索引
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="columnNames"></param>
        /// <param name="isUnique"></param>
        /// <returns></returns>
        public virtual string toCreateIndex(string tableName, string[] columnNames, bool isUnique)
        {
            if (isUnique)
            {
                return SQLPit.CreateIndexBy(tableName, string.Join(",", columnNames), string.Join("_", columnNames) + "_Unique", "UNIQUE");
            }
            return SQLPit.CreateIndexBy(tableName, string.Join(",", columnNames), string.Join("_", columnNames), "");
        }
        /// <summary>
        /// 创建索引
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="columnNames"></param>
        /// <param name="isUnique"></param>
        /// <returns></returns>
        public virtual bool doCreateIndex(string tableName, string[] columnNames, bool isUnique)
        {
            string createIndexSql = toCreateIndex(tableName, columnNames, isUnique);
            DBLive.ExeNonQuery(createIndexSql, null);
            return true;
        }

        /// <summary>
        /// 添加索引
        /// </summary>
        /// <param name="entityInfo"></param>
        /// <returns></returns>
        public virtual IEnumerable<string> toAddIndex(EntityInfo entityInfo)
        {
            List<string> list = new List<string>();
            List<EntityColumn> source = entityInfo.Columns.Where((EntityColumn it) => !it.IsIgnore).ToList();
            List<EntityColumn> list2 = source.Where((EntityColumn it) => it.IndexGroupNameList.HasValue()).ToList();
            if (list2.Count > 0)
            {
                List<string> list3 = (from it in list2.SelectMany((EntityColumn it) => it.IndexGroupNameList)
                                      group it by it into it
                                      select it.Key).ToList();
                foreach (string item2 in list3)
                {
                    string[] array = (from it in list2
                                      where it.IndexGroupNameList.Any((string i) => i.Equals(item2, StringComparison.CurrentCultureIgnoreCase))
                                      select it.DbColumnName).ToArray();
                    string indexName = string.Format("Index_{0}_{1}", entityInfo.DbTableName, string.Join("_", array));
                    if (!hasIndex(indexName))
                    {
                        list.Add(toCreateIndex(entityInfo.DbTableName, array, isUnique: false));
                    }
                }
            }
            List<EntityColumn> list4 = source.Where((EntityColumn it) => it.UIndexGroupNameList.HasValue()).ToList();
            if (list4.HasValue())
            {
                List<string> list5 = (from it in list4.SelectMany((EntityColumn it) => it.UIndexGroupNameList)
                                      group it by it into it
                                      select it.Key).ToList();
                foreach (string item in list5)
                {
                    string[] array2 = (from it in list4
                                       where it.UIndexGroupNameList.Any((string i) => i.Equals(item, StringComparison.CurrentCultureIgnoreCase))
                                       select it.DbColumnName).ToArray();
                    string indexName2 = string.Format("Index_{0}_{1}_Unique", entityInfo.DbTableName, string.Join("_", array2));
                    if (!hasIndex(indexName2))
                    {
                        list.Add(toCreateIndex(entityInfo.DbTableName, array2, isUnique: true));
                    }
                }
            }
            return list;
        }
        /// <summary>
        /// 添加索引
        /// </summary>
        /// <param name="entityInfo"></param>
        public virtual void doAddIndex(EntityInfo entityInfo)
        {
            List<EntityColumn> source = entityInfo.Columns.Where((EntityColumn it) => !it.IsIgnore).ToList();
            List<EntityColumn> list = source.Where((EntityColumn it) => it.IndexGroupNameList.HasValue()).ToList();
            if (list.HasValue())
            {
                List<string> list2 = (from it in list.SelectMany((EntityColumn it) => it.IndexGroupNameList)
                                      group it by it into it
                                      select it.Key).ToList();
                foreach (string item2 in list2)
                {
                    string[] array = (from it in list
                                      where it.IndexGroupNameList.Any((string i) => i.Equals(item2, StringComparison.CurrentCultureIgnoreCase))
                                      select it.DbColumnName).ToArray();
                    string indexName = string.Format("Index_{0}_{1}", entityInfo.DbTableName, string.Join("_", array));
                    if (!hasIndex(indexName))
                    {
                        doCreateIndex(entityInfo.DbTableName, array, isUnique: false);
                    }
                }
            }
            List<EntityColumn> list3 = source.Where((EntityColumn it) => it.UIndexGroupNameList.HasValue()).ToList();
            if (!list3.HasValue())
            {
                return;
            }
            List<string> list4 = (from it in list3.SelectMany((EntityColumn it) => it.UIndexGroupNameList)
                                  group it by it into it
                                  select it.Key).ToList();
            foreach (string item in list4)
            {
                string[] array2 = (from it in list3
                                   where it.UIndexGroupNameList.Any((string i) => i.Equals(item, StringComparison.CurrentCultureIgnoreCase))
                                   select it.DbColumnName).ToArray();
                string indexName2 = string.Format("Index_{0}_{1}_Unique", entityInfo.DbTableName, string.Join("_", array2));
                if (!hasIndex(indexName2))
                {
                    doCreateIndex(entityInfo.DbTableName, array2, isUnique: true);
                }
            }
        }
        /// <summary>
        /// 创建视图
        /// </summary>
        /// <param name="entityInfo"></param>
        /// <returns></returns>
        public virtual bool doCreateView(EntityInfo entityInfo)
        {
            string createViewSql = toGetCreateView(entityInfo);
            if (string.IsNullOrEmpty(createViewSql))
            {
                return false;
            }
            DBLive.ExeNonQuery(createViewSql, null);
            return true;
        }
        /// <summary>
        /// 创建视图
        /// </summary>
        /// <param name="entityInfo"></param>
        /// <returns></returns>
        public virtual string toGetCreateView(EntityInfo entityInfo)
        {
            return entityInfo.ViewSQL;
        }
        /// <summary>
        /// 创建数据库
        /// </summary>
        /// <param name="DatabaseName"></param>
        /// <param name="databaseDirectory"></param>
        /// <returns></returns>
        public virtual bool doCreateDatabase(string DatabaseName, string databaseDirectory = null)
        {
            bool result = false;
            if (databaseDirectory != null && Directory.Exists(databaseDirectory))
            {
                Directory.CreateDirectory(databaseDirectory);
            }

            if (!SQLVerse.GetDataBaseList().Any((string it) => it.Equals(DatabaseName, StringComparison.CurrentCultureIgnoreCase)))
            {
                var sql = toCreateDataBase(DatabaseName, databaseDirectory);
                DBLive.ExeNonQuery(sql);
                result = true;
            }
            return result;
        }

        protected virtual SQLCmd toCreateDataBase(string DatabaseName, string databaseDirectory = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 初始化表
        /// </summary>
        /// <param name="types"></param>
        public virtual void doInitTable(DMLOption option, params Type[] types)
        {
            if (types.HasValue())
            {
                types = types.Distinct().ToArray();
                EntityInfo[] entityInfos = (from p in types
                                            select loadEntityInfo(p) into it
                                            orderby it.IsView ? 1 : 0
                                            select it).ToArray();
                doInitTable(entityInfos, option);
            }
        }
        /// <summary>
        /// 初始化表
        /// </summary>
        /// <param name="allAssemblies"></param>
        /// <param name="option"></param>
        public virtual void doInitTable(IEnumerable<Assembly> allAssemblies, DMLOption option)
        {
            var ef = loadAllEntityInfos(allAssemblies, option);
            doInitTable(ef, option);
        }
        /// <summary>
        /// 初始化表
        /// </summary>
        /// <param name="entityInfos"></param>
        /// <param name="option"></param>
        public virtual void doInitTable(IEnumerable<EntityInfo> entityInfos, DMLOption option)
        {
            if (entityInfos == null || entityInfos.Count() == 0)
            {
                return;
            }
            entityInfos = entityInfos.OrderBy((EntityInfo it) => it.IsView ? 1 : 0).ToArray();
            List<string> list = new List<string>();
            //EntityInfo[] array = entityInfos;
            foreach (EntityInfo entityinfo in entityInfos)
            {
                if (entityinfo.DBPosition != DBLive.config.index)
                {
                    //ULog.LogWarning(GetType(), "InitTable:" + entityinfo.EntityName + "类中定义的数据库和当前ado所指向数据库不是同一个数据库，无法完成表创建或更新处理!");
                    continue;
                }
                try
                {
                    if (entityinfo.IsView)
                    {
                        if (hasView(entityinfo.DbTableName))
                        {
                            //ULog.LogInformation(GetType(), "InitTable:视图" + entityinfo.DbTableName + "已在数据库中存在!");
                            continue;
                        }
                        doCreateView(entityinfo);
                        //ULog.LogDebug(GetType(), "InitTable:创建视图" + entityinfo.DbTableName);
                    }
                    else if (hasTable(entityinfo.DbTableName) || list.Any((string p) => p.Equals(entityinfo.DbTableName, StringComparison.OrdinalIgnoreCase)))
                    {
                        doAlterTableLogic(entityinfo, option.IsDropColumn);
                        //ULog.LogDebug(GetType(), "InitTable:更新表" + entityinfo.DbTableName);
                    }
                    else
                    {
                        CreateTableLogic(entityinfo);
                        //ULog.LogDebug(GetType(), "InitTable:创建表" + entityinfo.DbTableName);
                        list.Add(entityinfo.DbTableName);
                        doAddIndex(entityinfo);
                    }
                }
                catch (Exception ex)
                {
                    //ULog.LogWarning(GetType(), "InitTable:" + entityinfo.EntityName + "创建或更新表失败!失败原因是:" + ex.Message);
                }
            }
        }
        /// <summary>
        /// 初始化表
        /// </summary>
        /// <param name="option"></param>
        /// <param name="types"></param>
        /// <returns></returns>
        public virtual IEnumerable<string> toInitTableList(DMLOption option, params Type[] types)
        {
            if (!types.HasValue())
            {
                return new List<string>();
            }
            types = types.Distinct().ToArray();
            EntityInfo[] entityInfos = (from p in types
                                        select loadEntityInfo(p) into it
                                        orderby it.IsView ? 1 : 0
                                        select it).ToArray();
            return toInitTableList(entityInfos, option);
        }
        /// <summary>
        /// 初始化一组表
        /// </summary>
        /// <param name="allAssemblies"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public virtual IEnumerable<string> toInitTableList(IEnumerable<Assembly> allAssemblies, DMLOption option)
        {
            var ef = loadAllEntityInfos(allAssemblies, option);
            return toInitTableList(ef, option);
        }
        /// <summary>
        /// 获取实体列表
        /// </summary>
        /// <param name="allAssemblies"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public virtual IEnumerable<EntityInfo> loadAllEntityInfos(IEnumerable<Assembly> allAssemblies, DMLOption option)
        {


            List<EntityInfo> entityInfos = new List<EntityInfo>();
            foreach (Assembly assembly in allAssemblies)
            {
                var types = assembly.GetTypes();
                foreach (var t in types)
                {
                    var ef = DBLive.client.EntityCash.getEntityInfo(t);
                    if (ef != null)
                    {
                        entityInfos.Add(ef);
                    }
                }
            }
            return entityInfos;
        }
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="entityInfos"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public virtual IEnumerable<string> toInitTableList(IEnumerable<EntityInfo> entityInfos, DMLOption option)
        {
            List<string> list = new List<string>();
            entityInfos = entityInfos.OrderBy((EntityInfo it) => it.IsView ? 1 : 0).ToArray();
            List<string> list2 = new List<string>();

            foreach (EntityInfo entityinfo in entityInfos)
            {
                //if (entityinfo.DBConnIndex != base.UCMLdao.DBConnIndex)
                //{
                //    //ULog.LogWarning(GetType(), "InitTable:" + entityinfo.EntityName + "类中定义的数据库和当前ado所指向数据库不是同一个数据库，无法完成表创建或更新处理的SQL语句!");
                //    continue;
                //}
                try
                {
                    if (entityinfo.IsView)
                    {
                        if (hasView(entityinfo.DbTableName))
                        {
                            //ULog.LogInformation(GetType(), "InitTable:视图" + entityinfo.DbTableName + "已在数据库中存在!");
                            continue;
                        }
                        string createViewSql = toGetCreateView(entityinfo);
                        if (!string.IsNullOrEmpty(createViewSql))
                        {
                            list.Add(createViewSql);
                        }
                    }
                    else if (hasTable(entityinfo.DbTableName) || list2.Any((string p) => p.Equals(entityinfo.DbTableName, StringComparison.OrdinalIgnoreCase)))
                    {
                        list.AddRange(toAlterTableLogicSqls(entityinfo, option.IsDropColumn));
                    }
                    else
                    {
                        list.AddRange(toCreateTableLogicSqls(entityinfo));
                        list2.Add(entityinfo.DbTableName);
                        list.AddRange(toAddIndex(entityinfo));
                    }
                }
                catch (Exception ex)
                {
                    //ULog.LogWarning(GetType(), "InitTable:" + entityinfo.EntityName + "创建或更新表的SQL语句失败!失败原因是:" + ex.Message);
                }
            }
            return list;
        }





        public EntityInfo loadEntityInfo(Type infoType)
        {
            var res = DBLive.client.EntityCash.getEntityInfo(infoType);
            return res;
        }


        protected virtual DbColumnInfo EntityColumnToDbColumn(EntityColumn item)
        {
            DbColumnInfo result = new DbColumnInfo
            {
                Name = (string.IsNullOrWhiteSpace(item.DbColumnName) ? item.DbColumnName : item.PropertyName),
                IsPrimary = item.IsPrimarykey,
                IsIdentity = item.IsIdentity,
                IsNullable = item.IsNullable,
                DefaultValue = item.DefaultValue,
                Comment = item.ColumnDescription,
                MaxLength = item.Length,
                Precision = item.Precision,
                Scale = item.Scale
            };
            SetDbType(item, result);
            return result;
        }

        protected virtual void SetDbType(EntityColumn item, DbColumnInfo result)
        {
            if (!Enum.IsDefined(typeof(DataFam), item.DataType))
            {
                item.DataType = DataFam.VarChar;
            }
            result.DbType = item.DbType;
            result.DbTypeTextFull = result.DbType.ToDBString();
        }

        protected virtual bool isSamgeType(EntityColumn ec, DbColumnInfo dc)
        {
            return ec.DbType.IsSame(dc.DbType);
        }

        protected virtual void CreateTableLogic(EntityInfo entityInfo)
        {
            List<DbColumnInfo> list = new List<DbColumnInfo>();
            if (entityInfo.Columns.HasValue())
            {
                foreach (EntityColumn item2 in from it in entityInfo.Columns
                                               orderby (!it.IsPrimarykey) ? 1 : 0
                                               where !it.IsIgnore
                                               select it)
                {
                    DbColumnInfo item = EntityColumnToDbColumn(item2);
                    list.Add(item);
                }
            }
            doCreateTable(entityInfo.DbTableName, list);
            IEnumerable<EntityColumn> enumerable = entityInfo.Columns.Where((EntityColumn it) => !it.IsIgnore);
            foreach (EntityColumn item3 in enumerable)
            {
                if (item3.ColumnDescription != null)
                {
                    doAddColumnCaption(item3.DbColumnName, entityInfo.DbTableName, item3.ColumnDescription);
                }
            }
            if (entityInfo.TableDescription != null)
            {
                doAddTableCaption(entityInfo.DbTableName, entityInfo.TableDescription);
            }
        }

        protected virtual IEnumerable<string> toCreateTableLogicSqls(EntityInfo entityInfo)
        {
            List<string> list = new List<string>();
            List<DbColumnInfo> list2 = new List<DbColumnInfo>();
            if (entityInfo.Columns.HasValue())
            {
                foreach (EntityColumn item2 in from it in entityInfo.Columns
                                               orderby (!it.IsPrimarykey) ? 1 : 0
                                               where !it.IsIgnore
                                               select it)
                {
                    DbColumnInfo item = EntityColumnToDbColumn(item2);
                    list2.Add(item);
                }
            }
            list.Add(toCreateTable(entityInfo.DbTableName, list2));
            List<DbColumnInfo> list3 = list2.Where((DbColumnInfo it) => it.IsPrimary).ToList();
            if (list3.Count > 1)
            {
                list.Add(toAddPrimaryKeys(entityInfo.DbTableName, list3.Select((DbColumnInfo it) => it.Name).ToArray()));
            }
            else
            {
                foreach (DbColumnInfo item3 in list3)
                {
                    list.Add(toAddPrimaryKey(entityInfo.DbTableName, item3.Name));
                }
            }
            IEnumerable<EntityColumn> enumerable = entityInfo.Columns.Where((EntityColumn it) => !it.IsIgnore);
            foreach (EntityColumn item4 in enumerable)
            {
                if (item4.ColumnDescription != null)
                {
                    list.Add(toAddColumnCaption(item4.DbColumnName, entityInfo.DbTableName, item4.ColumnDescription));
                }
            }
            if (entityInfo.TableDescription != null)
            {
                list.Add(toAddTableCaption(entityInfo.DbTableName, entityInfo.TableDescription));
            }
            return list;
        }

        protected virtual void doAlterTableLogic(EntityInfo entityInfo, bool IsDropColumn)
        {
            if (entityInfo.Columns.HasValue())
            {
                List<DbColumnInfo> dbColumns = SQLVerse.GetDbColumnsByTableName(entityInfo.DbTableName);
                List<EntityColumn> entityColumns = entityInfo.Columns.Where((EntityColumn it) => !it.IsIgnore).ToList();
                List<DbColumnInfo> list = dbColumns.Where((DbColumnInfo dc) => !entityColumns.Any((EntityColumn ec) => dc.Name.Equals(ec.DbColumnName, StringComparison.CurrentCultureIgnoreCase))).ToList();
                List<EntityColumn> list2 = entityColumns.Where((EntityColumn ec) => !dbColumns.Any((DbColumnInfo dc) => ec.DbColumnName.Equals(dc.Name, StringComparison.CurrentCultureIgnoreCase))).ToList();
                List<EntityColumn> list3 = entityColumns.Where((EntityColumn ec) => dbColumns.Any((DbColumnInfo dc) => dc.Name.Equals(ec.DbColumnName, StringComparison.CurrentCultureIgnoreCase) && !isSamgeType(ec, dc))).ToList();
                List<EntityColumn> list4 = entityColumns.Where((EntityColumn ec) => dbColumns.Any((DbColumnInfo dc) => dc.Name.Equals(ec.DbColumnName, StringComparison.CurrentCultureIgnoreCase) && !ec.ColumnDescription.Equals(ec.ColumnDescription, StringComparison.CurrentCultureIgnoreCase))).ToList();
                foreach (EntityColumn item in list2)
                {
                    doAddColumn(entityInfo.DbTableName, EntityColumnToDbColumn(item));
                    doAddColumnCaption(item.DbColumnName, entityInfo.DbTableName, item.ColumnDescription);
                }
                if (IsDropColumn)
                {
                    foreach (DbColumnInfo item2 in list)
                    {
                        doDropColumn(entityInfo.DbTableName, item2.Name);
                    }
                }
                foreach (EntityColumn item3 in list3)
                {
                    doUpdateColumn(entityInfo.DbTableName, EntityColumnToDbColumn(item3));
                }
                foreach (EntityColumn item4 in list4)
                {
                    doDeleteColumnCaption(item4.DbColumnName, entityInfo.DbTableName);
                    doAddColumnCaption(item4.DbColumnName, entityInfo.DbTableName, item4.ColumnDescription);
                }
                doAlterPrimarykeyAndIdentity(entityColumns, dbColumns, entityInfo.DbTableName);
            }
            string text = (from it in SQLVerse.GetDbTableList()
                           where it.Name.Equals(entityInfo.DbTableName, StringComparison.CurrentCultureIgnoreCase)
                           select it).FirstOrDefault()?.Comment;
            bool flag = false;
        }


        protected virtual void doAlterPrimarykeyAndIdentity(List<EntityColumn> entityColumns, List<DbColumnInfo> dbColumns, string tableName)
        {
            if (dbColumns.Where((DbColumnInfo it) => it.IsPrimary).Count() > 1 || entityColumns.Where((EntityColumn it) => it.IsPrimarykey).Count() > 1)
            {
                return;
            }
            EntityColumn EntityColumn = entityColumns.FirstOrDefault((EntityColumn p) => p.IsPrimarykey);
            DbColumnInfo DbColumnInfo = dbColumns.FirstOrDefault((DbColumnInfo p) => p.IsPrimary);
            if (EntityColumn != null || DbColumnInfo != null)
            {
                if (EntityColumn != null && DbColumnInfo == null)
                {
                    doAddPrimaryKey(tableName, EntityColumn.DbColumnName);
                }
                else if (EntityColumn == null && DbColumnInfo != null)
                {
                    doDropConstraint(tableName, $"PK_{tableName}_{DbColumnInfo.Name}");
                }
                else if (EntityColumn != null && DbColumnInfo != null && !EntityColumn.DbColumnName.Equals(DbColumnInfo.Name, StringComparison.OrdinalIgnoreCase))
                {
                    doDropConstraint(tableName, $"PK_{tableName}_{DbColumnInfo.Name}");
                    doAddPrimaryKey(tableName, EntityColumn.DbColumnName);
                }
            }
        }

        protected virtual IEnumerable<string> toAlterPrimarykeyAndIdentitySqls(List<EntityColumn> entityColumns, List<DbColumnInfo> dbColumns, string tableName)
        {
            List<string> list = new List<string>();
            if (dbColumns.Where((DbColumnInfo it) => it.IsPrimary).Count() <= 1 && entityColumns.Where((EntityColumn it) => it.IsPrimarykey).Count() <= 1)
            {
                EntityColumn EntityColumn = entityColumns.FirstOrDefault((EntityColumn p) => p.IsPrimarykey);
                DbColumnInfo DbColumnInfo = dbColumns.FirstOrDefault((DbColumnInfo p) => p.IsPrimary);
                if (EntityColumn == null && DbColumnInfo == null)
                {
                    return list;
                }
                if (EntityColumn != null && DbColumnInfo == null)
                {
                    list.Add(toAddPrimaryKey(tableName, EntityColumn.DbColumnName));
                    return list;
                }
                if (EntityColumn == null && DbColumnInfo != null)
                {
                    list.Add(toDropConstraint(tableName, $"PK_{tableName}_{DbColumnInfo.Name}").sql);
                    return list;
                }
                if (EntityColumn != null && DbColumnInfo != null && !EntityColumn.DbColumnName.Equals(DbColumnInfo.Name, StringComparison.OrdinalIgnoreCase))
                {
                    list.Add(toDropConstraint(tableName, $"PK_{tableName}_{DbColumnInfo.Name}").sql);
                    list.Add(toAddPrimaryKey(tableName, EntityColumn.DbColumnName));
                }
            }
            return list;
        }

        protected virtual IEnumerable<string> toAlterTableLogicSqls(EntityInfo entityInfo, bool IsDropColumn)
        {

            List<string> list = new List<string>();
            if (entityInfo.Columns.HasValue())
            {
                List<DbColumnInfo> dbColumns = SQLVerse.GetDbColumnsByTableName(entityInfo.DbTableName);

                List<EntityColumn> entityColumns = entityInfo.Columns.Where((EntityColumn it) => !it.IsIgnore).ToList();
                List<DbColumnInfo> list2 = dbColumns.Where((DbColumnInfo dc) => !entityColumns.Any((EntityColumn ec) => dc.Name.Equals(ec.DbColumnName, StringComparison.CurrentCultureIgnoreCase))).ToList();
                List<EntityColumn> list3 = entityColumns.Where((EntityColumn ec) => !dbColumns.Any((DbColumnInfo dc) => ec.DbColumnName.Equals(dc.Name, StringComparison.CurrentCultureIgnoreCase))).ToList();
                List<EntityColumn> list4 = entityColumns.Where((EntityColumn ec) => dbColumns.Any((DbColumnInfo dc) => dc.Name.Equals(ec.DbColumnName, StringComparison.CurrentCultureIgnoreCase) && !isSamgeType(ec, dc))).ToList();
                List<EntityColumn> list5 = entityColumns.Where((EntityColumn ec) => dbColumns.Any((DbColumnInfo dc) => dc.Name.Equals(ec.DbColumnName, StringComparison.CurrentCultureIgnoreCase) && !ec.ColumnDescription.Trim().Equals(ec.ColumnDescription.Trim(), StringComparison.CurrentCultureIgnoreCase))).ToList();
                foreach (EntityColumn item in list3)
                {
                    list.Add(toAddColumn(entityInfo.DbTableName, EntityColumnToDbColumn(item)));
                    list.Add(toAddColumnCaption(item.DbColumnName, entityInfo.DbTableName, item.ColumnDescription));
                }
                if (IsDropColumn)
                {
                    foreach (DbColumnInfo item2 in list2)
                    {
                        list.Add(this.toDropColumn(entityInfo.DbTableName, item2.Name).sql);
                    }
                }
                foreach (EntityColumn item3 in list4)
                {
                    list.Add(toUpdateColumn(entityInfo.DbTableName, EntityColumnToDbColumn(item3)));
                }
                foreach (EntityColumn item4 in list5)
                {
                    list.Add(toDeleteColumnCaption(item4.DbColumnName, entityInfo.DbTableName));
                    list.Add(toAddColumnCaption(item4.DbColumnName, entityInfo.DbTableName, item4.ColumnDescription));
                }
                list.AddRange(toAlterPrimarykeyAndIdentitySqls(entityColumns, dbColumns, entityInfo.DbTableName));
            }
            if (entityInfo.TableDescription != null)
            {
                if (hasTableCaption(entityInfo.DbTableName))
                {
                    list.Add(this.toDeleteTableCaption(entityInfo.DbTableName).sql);
                    list.Add(toAddTableCaption(entityInfo.DbTableName, entityInfo.TableDescription));
                }
                else
                {
                    list.Add(toAddTableCaption(entityInfo.DbTableName, entityInfo.TableDescription));
                }
            }
            return list;
        }

        protected virtual void doChangeKey(string tableName, EntityColumn item)
        {
            string constraintName = $"PK_{tableName}_{item.DbColumnName}";
            if (hasConstraint(constraintName))
            {
                doDropConstraint(tableName, constraintName);
            }
            doDropColumn(tableName, item.DbColumnName);
            doAddColumn(tableName, EntityColumnToDbColumn(item));
            if (item.IsPrimarykey)
            {
                doAddPrimaryKey(tableName, item.DbColumnName);
            }
        }
    }
}
