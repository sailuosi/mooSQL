using System;
using System.Collections.Generic;

namespace mooSQL.data.richRepo.schema
{
    /// <summary>
    /// 表结构同步模式。默认只增不删。
    /// </summary>
    public enum SyncMode
    {
        /// <summary>仅当表不存在时 CREATE；已存在则跳过。</summary>
        CreateIfMissing = 0,

        /// <summary>推荐默认：无表则建；有表则只增加缺失列。不 DROP。</summary>
        AddMissingColumns = 1,

        /// <summary>在 AddMissingColumns 基础上侧重注释对齐（当前与 Add 同路径，依赖 DDL 内 caption 逻辑）。</summary>
        SyncCaptions = 2,

        /// <summary>危险：删除实体中不存在的 DB 列。须 AllowDropColumn=true。</summary>
        AddAndDropExtraColumns = 9
    }

    /// <summary>
    /// SchemaEnsure 选项。
    /// </summary>
    public sealed class SchemaEnsureOptions
    {
        /// <summary>同步模式。</summary>
        public SyncMode Mode { get; set; } = SyncMode.AddMissingColumns;

        /// <summary>为 false 时任何 Ensure 直接失败。</summary>
        public bool AllowSchemaSync { get; set; } = true;

        /// <summary>仅 Mode=AddAndDropExtraColumns 时生效；默认 false。</summary>
        public bool AllowDropColumn { get; set; } = false;

        /// <summary>true → 只生成 SQL，不执行。</summary>
        public bool PreviewOnly { get; set; } = false;

        /// <summary>分片物理表名；空则用实体默认表名。</summary>
        public string PhysicalTableName { get; set; }

        /// <summary>执行或预览产生的脚本。</summary>
        public List<string> ScriptsOut { get; } = new List<string>();
    }

    /// <summary>
    /// EnsureSchema 执行结果。
    /// </summary>
    public sealed class SchemaEnsureResult
    {
        /// <summary>是否成功。</summary>
        public bool Success { get; set; }

        /// <summary>说明信息。</summary>
        public string Message { get; set; }

        /// <summary>相关 SQL（预览或回填）。</summary>
        public IReadOnlyList<string> Scripts { get; set; } = new string[0];

        /// <summary>成功结果。</summary>
        public static SchemaEnsureResult Ok(string message = null, IReadOnlyList<string> scripts = null)
            => new SchemaEnsureResult
            {
                Success = true,
                Message = message ?? "ok",
                Scripts = scripts ?? new string[0]
            };

        /// <summary>失败结果。</summary>
        public static SchemaEnsureResult Fail(string message, IReadOnlyList<string> scripts = null)
            => new SchemaEnsureResult
            {
                Success = false,
                Message = message ?? "failed",
                Scripts = scripts ?? new string[0]
            };
    }
}
