using System;
using System.Collections.Generic;
using System.Linq;

namespace mooSQL.data
{
    /// <summary>
    /// Schema 对齐门面：包装 DDLBuilder.doInitTable / toInitTableList，默认不 DROP。
    /// </summary>
    public static class SchemaEnsure
    {
        /// <summary>全局默认：生产可设为 false。</summary>
        public static bool DefaultAllowSchemaSync { get; set; } = true;

        /// <summary>
        /// 客户端级 DROP 闸（由 <see cref="MooClient.configureSchema"/> 写入）。
        /// 与 <see cref="SchemaEnsureOptions.AllowDropColumn"/> 双闸同时为 true 才允许 DROP。
        /// </summary>
        public static bool DefaultAllowDropColumn { get; set; } = false;

        /// <summary>Ensure 指定实体类型。</summary>
        public static SchemaEnsureResult Ensure<T>(DBInstance db, SchemaEnsureOptions opt = null)
            => Ensure(db, typeof(T), opt);

        /// <summary>Ensure 指定类型。</summary>
        public static SchemaEnsureResult Ensure(DBInstance db, Type type, SchemaEnsureOptions opt = null)
        {
            if (type == null) return SchemaEnsureResult.Fail("type 为空");
            return Ensure(db, new[] { type }, opt);
        }

        /// <summary>Ensure 多个类型。</summary>
        public static SchemaEnsureResult Ensure(DBInstance db, IEnumerable<Type> types, SchemaEnsureOptions opt = null)
        {
            if (db == null) return SchemaEnsureResult.Fail("db 为空");
            opt = opt ?? new SchemaEnsureOptions();
            if (!DefaultAllowSchemaSync)
                opt.AllowSchemaSync = false;
            if (!opt.AllowSchemaSync)
                return SchemaEnsureResult.Fail("AllowSchemaSync=false，已跳过结构同步");

            var typeArr = types?.Where(t => t != null).Distinct().ToArray() ?? new Type[0];
            if (typeArr.Length == 0)
                return SchemaEnsureResult.Fail("未指定实体类型");

            try
            {
                if (opt.Mode == SyncMode.CreateIfMissing)
                    return EnsureCreateIfMissing(db, typeArr, opt);

                var wantDrop = opt.Mode == SyncMode.AddAndDropExtraColumns;
                var allowDrop = wantDrop && opt.AllowDropColumn && DefaultAllowDropColumn;
                string warn = null;
                if (wantDrop && !allowDrop)
                    warn = "AddAndDropExtraColumns 未同时满足 Options.AllowDropColumn 与 DefaultAllowDropColumn，已降级为只增不删";

                var dml = new DMLOption { IsDropColumn = allowDrop };
                var ddl = db.useDDL();
                string okMsg;
                if (allowDrop)
                    okMsg = SyncMode.AddAndDropExtraColumns.ToString();
                else if (wantDrop)
                    okMsg = SyncMode.AddMissingColumns + "; " + warn;
                else
                    okMsg = opt.Mode.ToString();

                if (opt.PreviewOnly)
                {
                    var scripts = ddl.toInitTableList(dml, typeArr)?.ToList() ?? new List<string>();
                    opt.ScriptsOut.AddRange(scripts);
                    return SchemaEnsureResult.Ok(okMsg, scripts);
                }

                ddl.doInitTable(dml, typeArr);
                return SchemaEnsureResult.Ok(okMsg);
            }
            catch (Exception ex)
            {
                return SchemaEnsureResult.Fail(ex.Message);
            }
        }

        /// <summary>仅预览 SQL。</summary>
        public static IReadOnlyList<string> Preview<T>(DBInstance db, SyncMode mode = SyncMode.AddMissingColumns)
        {
            var opt = new SchemaEnsureOptions { Mode = mode, PreviewOnly = true };
            var r = Ensure<T>(db, opt);
            return r.Scripts ?? new string[0];
        }

        static SchemaEnsureResult EnsureCreateIfMissing(DBInstance db, Type[] types, SchemaEnsureOptions opt)
        {
            var ddl = db.useDDL();
            var scripts = new List<string>();
            foreach (var t in types)
            {
                var en = db.client.EntityCash.getEntityInfo(t);
                if (en == null) continue;
                var table = string.IsNullOrEmpty(opt.PhysicalTableName) ? en.DbTableName : opt.PhysicalTableName;
                if (ddl.hasTable(table))
                    continue;

                var one = new DMLOption { IsDropColumn = false };
                if (opt.PreviewOnly)
                {
                    var list = ddl.toInitTableList(one, t)?.ToList() ?? new List<string>();
                    scripts.AddRange(list);
                    opt.ScriptsOut.AddRange(list);
                }
                else
                {
                    ddl.doInitTable(one, t);
                }
            }
            return SchemaEnsureResult.Ok(SyncMode.CreateIfMissing.ToString(), scripts);
        }
    }
}
