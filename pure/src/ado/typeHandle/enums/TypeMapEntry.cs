using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace mooSQL.data
{
    /// <summary>
    /// 类型映射条目，包含数据库类型和标志
    /// </summary>
    internal readonly struct TypeMapEntry : IEquatable<TypeMapEntry>
    {
        public DbType DbType { get; }
        public readonly TypeMapEntryFlags Flags;
        public TypeMapEntry(DbType dbType, TypeMapEntryFlags flags)
        {
            DbType = dbType;
            Flags = flags;
        }
        public override int GetHashCode() => (int)DbType ^ (int)Flags;
        public override string ToString() => $"{DbType}, {Flags}";
        public override bool Equals(object obj) => obj is TypeMapEntry other && Equals(other);
        public bool Equals(TypeMapEntry other) => other.DbType == DbType && other.Flags == Flags;
        public static readonly TypeMapEntry
            DoNotSet = new TypeMapEntry((DbType)(-2), TypeMapEntryFlags.None),
            DecimalFieldValue = new TypeMapEntry(DbType.Decimal, TypeMapEntryFlags.SetType | TypeMapEntryFlags.UseGetFieldValue);

        public static implicit operator TypeMapEntry(DbType dbType)
            => new TypeMapEntry(dbType, TypeMapEntryFlags.SetType);
    }
}
