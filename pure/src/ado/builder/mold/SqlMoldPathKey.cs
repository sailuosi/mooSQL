using System;

namespace mooSQL.data
{
    /// <summary>
    /// SQLMold 缓存路径键（结构相等；禁止仅用 int 哈希当唯一键）。
    /// </summary>
    public sealed class SqlMoldPathKey : IEquatable<SqlMoldPathKey>
    {
        /// <summary>掩码指纹。</summary>
        public string MaskFingerprint { get; }

        /// <summary>select/from/join/order/page 等结构指纹。</summary>
        public string StructureFingerprint { get; }

        /// <summary>数据库类型。</summary>
        public DataBaseType DbType { get; }

        /// <summary>方言 whereIn 上限（无限制为 -1）。</summary>
        public int InLimit { get; }

        /// <summary>命令种类标记。</summary>
        public string CmdKind { get; }

        readonly int _hash;

        /// <summary>
        /// 创建路径键。
        /// </summary>
        public SqlMoldPathKey(string maskFingerprint, string structureFingerprint, DataBaseType dbType, int inLimit, string cmdKind)
        {
            MaskFingerprint = maskFingerprint ?? "";
            StructureFingerprint = structureFingerprint ?? "";
            DbType = dbType;
            InLimit = inLimit;
            CmdKind = cmdKind ?? "Select";
            unchecked
            {
                var h = 17;
                h = h * 31 + MaskFingerprint.GetHashCode();
                h = h * 31 + StructureFingerprint.GetHashCode();
                h = h * 31 + (int)DbType;
                h = h * 31 + InLimit;
                h = h * 31 + CmdKind.GetHashCode();
                _hash = h;
            }
        }

        /// <inheritdoc />
        public bool Equals(SqlMoldPathKey other)
        {
            if (other == null) return false;
            return DbType == other.DbType
                && InLimit == other.InLimit
                && CmdKind == other.CmdKind
                && MaskFingerprint == other.MaskFingerprint
                && StructureFingerprint == other.StructureFingerprint;
        }

        /// <inheritdoc />
        public override bool Equals(object obj) => Equals(obj as SqlMoldPathKey);

        /// <inheritdoc />
        public override int GetHashCode() => _hash;
    }
}
