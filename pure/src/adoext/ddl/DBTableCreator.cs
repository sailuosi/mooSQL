using mooSQL.data.model;
using mooSQL.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 创建表的帮助类，直接根据注解，创建表
    /// </summary>
    public class DBTableCreator
    {

        public DBInstance DBLive {  get; set; }

        private int defaultFieldLength = 50;
        /// <summary>
        /// 创建表
        /// </summary>
        /// <param name="en"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public string CreateTable(EntityInfo en,string mode)
        {
            //先默认为表不存在。

            var ddl = DBLive.useDDL();
    

            var tbname = en.DbTableName;
            if (string.IsNullOrWhiteSpace(tbname))
            {
                throw new Exception("未知表名");
            }


            var isExist = ddl.hasTable(tbname);
            if (isExist) {
                return "";
            }
            


            //获取字段的配置



            if (isExist == false)
            {
                //执行表的创建
                ddl.setTable(tbname, en.TableDescription);
                //if (tb.fCustomKey == null || tb.fCustomKey == false)
                //{
                    ddl.set(tbname + "OID", new DbDataType(DataFam.Guid), "主键", false, null, true);
                //}

                foreach (var field in en.Columns)
                {
                    setDDLField(field, ddl);
                }


                //ddl.doCreateTable();

                if (mode == "preview")
                {
                    var sql = ddl.toCreateTable();
                    var txt = sql.toRawSQL(DBLive.dialect.expression.paraPrefix);
                    return txt;
                }
                else if (mode == "create" || mode == "createAuto")
                {
                    var cc = ddl.doCreateTable();
                    return "处理完毕";
                }
            }
            else
            {
                //此时，表已存在，进行新增字段的添加
                //获取已有字段

                var exFields = ddl.loadColumns(tbname);

                var exFNames = exFields.map(x => x.Name);
                ddl.setTable(tbname, en.TableDescription);

                foreach (var field in en.Columns)
                {
                    if (exFNames.Contains(field.DbColumnName))
                    {
                        continue;
                    }
                    setDDLField(field, ddl);
                }
                //ddl.doCreateTable();

                if (mode == "preview")
                {
                    var sql = ddl.toAddColumn();
                    var txt = sql.toRawSQL(DBLive.dialect.expression.paraPrefix);
                    return txt;
                }
                else if (mode == "create" || mode == "createAuto")
                {
                    var cc = ddl.doAddColumn();
                    return "处理完毕";
                }

            }

            return "未执行处理";
        }
        private void setDDLField(EntityColumn field, DDLBuilder ddl) {
            var dt = transDtype(field);
            ddl.set(field.DbColumnName, dt, field.ColumnDescription, field.IsNullable, field.DefaultValue, field.IsPrimarykey);

        }

        private DbDataType transDtype(EntityColumn field)
        {
            if (field.DbType != DbDataType.Undefined) { 
                return field.DbType;
            }
            if (field.DataType == DataFam.Undefined) {
                var valType = field.PropertyInfo.PropertyType.UnwrapNullable();
                if (valType == typeof(string)) {
                    return new DbDataType(DataFam.VarChar, field.Length);
                }
                else if (valType == typeof(Guid))
                {
                    return new DbDataType(DataFam.Guid);
                }
                else if (valType == typeof(DateTime))
                {
                    return new DbDataType(DataFam.DateTime);
                }
                else if (valType == typeof(bool))
                {
                    return new DbDataType(DataFam.Boolean);
                }
                else if (valType == typeof(int))
                {
                    return new DbDataType(DataFam.Int32);
                }
                else if (valType == typeof(long))
                {
                    return new DbDataType(DataFam.Long);
                }
                else if (valType == typeof(double)|| valType == typeof(decimal) || valType == typeof(float))
                {
                    return new DbDataType(DataFam.Decimal, field.Length, field.Precision, field.Scale);
                }
                if (field.Length == 0)
                {
                    field.Length = this.defaultFieldLength;
                }
                return getDefaultField(field);
            }
            switch (field.DataType)
            {
                case DataFam.Char:
                    return new DbDataType(DataFam.Char, field.Length);
                case DataFam.VarChar:
                    return new DbDataType(DataFam.VarChar, field.Length);
                case DataFam.NVarChar:
                    return new DbDataType(DataFam.NVarChar, field.Length);
                case DataFam.Decimal:
                    return new DbDataType(DataFam.Decimal, field.Length, field.Precision, field.Scale);
                case DataFam.Text:
                case DataFam.Xml://xml
                case DataFam.Json://json
                case DataFam.NText://html
                case DataFam.DateTime:
                case DataFam.Long:
                case DataFam.Int32:
                case DataFam.Int64:
                case DataFam.Boolean:
                case DataFam.Guid:
                    return new DbDataType(field.DataType);


            }

            return getDefaultField(field);
        }
        private DbDataType getDefaultField(EntityColumn field) {
            if (field.Length == 0)
            {
                field.Length = this.defaultFieldLength;
            }
            return new DbDataType(DataFam.VarChar, field.Length);

        }
    }
}
