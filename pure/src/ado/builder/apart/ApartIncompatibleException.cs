using System;

namespace mooSQL.data
{
    /// <summary>
    /// 碎片与目标 SQLBuilder 数据库类型不兼容时抛出（一阶段仅允许同类数据库复用）。
    /// </summary>
    public class ApartIncompatibleException : Exception
    {
        public DataBaseType SourceDbType { get; }
        public DataBaseType TargetDbType { get; }

        public ApartIncompatibleException(DataBaseType source, DataBaseType target)
            : base($"SQLApart was captured for {source} but target SQLBuilder uses {target}. Cross-database apart reuse is not supported in this version.")
        {
            SourceDbType = source;
            TargetDbType = target;
        }
    }
}
