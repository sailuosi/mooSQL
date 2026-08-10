using System;
using System.Collections.Generic;
using System.Linq;
using mooSQL.data;

namespace mooSQL.data.richRepo.schema
{
    /// <summary>
    /// Schema 对齐门面：包装 DDLBuilder.doInitTable / toInitTableList，默认不 DROP。
    /// </summary>
    public static class SchemaEnsure
    {
        /// <summary>全局默认：生产可设为 false。</summary>
        public static bool DefaultAllowSchemaSync { get; set; } = true;

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

                var dml = new DMLOption
                {
                    IsDropColumn = opt.Mode == SyncMode.AddAndDropExtraColumns && opt.AllowDropColumn
                };
                var ddl = db.useDDL();
                if (opt.PreviewOnly)
                {
                    var scripts = ddl.toInitTableList(dml, typeArr)?.ToList() ?? new List<string>();
                    opt.ScriptsOut.AddRange(scripts);
                    return SchemaEnsureResult.Ok("preview", scripts);
                }

                ddl.doInitTable(dml, typeArr);
                return SchemaEnsureResult.Ok(opt.Mode.ToString());
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
