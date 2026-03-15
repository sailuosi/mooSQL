


using mooSQL.data;
using System;
using System.Collections.Generic;
using System.Data;

namespace HHNY.NET.Core
{
    public class BulkTable : BulkBase {
        public BulkTable(string tableName, DBInstance db) : base(tableName, db)
        {

        }
        public BulkTable():base()
        {

        }
        public int position;
        /// <summary>
        /// 带连接位的重载
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="position"></param>
        public BulkTable(string tableName, int position)
        {
            this.tableName = tableName;
            this.caption = tableName;
            this.position = position;
            this.DB = DBCash.GetDBInstance(position);
            this.colnames = new List<string>();
            this.bulkTarget = this.getBulkTable();
        }
        public Object checkValue(string colname, Object value)
        {
            //
            if (colCheck == checkColMode.none) return value;
            var col = bulkTarget.Columns[colname];
            var valueType = col.DataType.Name;
            if (valueType == "String")
            {
                var va = value.ToString();
                if (col.MaxLength > 1 && va.Length > col.MaxLength)
                {
                    if (colCheck == checkColMode.autocut)
                    {
                        va = va.Substring(0, col.MaxLength);
                        return va;
                    }
                    else
                    {
                        throw new Exception("列" + col.ColumnName + "的值" + va + "的长度超出了限制！");
                    }

                }
            }
            return value;
        }
        /// <summary>
        /// 添加系统字段的写入列，共计9个
        /// </summary>
        public void addSysTargetCols()
        {
            var sysCols = new String[] { "SYS_Deleted", "SYS_Created", "SYS_LAST_UPD", "SYS_CreatedBy", "SYS_LAST_UPD_BY", "SYS_REPLACEMENT", "SYS_DIVISION", "SYS_ORG", "SYS_POSTN" };
            foreach (var col in sysCols)
            {
                if (colnames.Contains(col) == false)
                    colnames.Add(col);
            }
        }
        ///// <summary>
        ///// 为addingRow添加系统字段
        ///// </summary>
        ///// <param name="user"></param>
        //public void addSysPart(ClientUserInfo user)
        //{
        //    addSysPart(addingRow, user);
        //}
        ///// <summary>
        ///// 添加9个系统字段，包含sysdeleted。
        ///// </summary>
        ///// <param name="row"></param>
        ///// <param name="user"></param>
        //public void addSysPart(DataRow row, ClientUserInfo user)
        //{
        //    this.add(row, "SYS_Deleted", false);
        //    this.add(row, "SYS_Created", DateTime.Now);
        //    this.add(row, "SYS_LAST_UPD", DateTime.Now);
        //    this.add(row, "SYS_CreatedBy", user.UserOID);
        //    this.add(row, "SYS_LAST_UPD_BY", user.UserOID);
        //    this.add(row, "SYS_REPLACEMENT", user.UserOID);
        //    this.add(row, "SYS_DIVISION", user.DivisionOID);
        //    this.add(row, "SYS_ORG", user.OrgOID);
        //    this.add(row, "SYS_POSTN", user.PostPrimaryOID);
        //}

    }
}