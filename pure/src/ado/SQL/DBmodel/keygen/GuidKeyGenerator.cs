using System;

namespace mooSQL.data
{
    /// <summary>
    /// 内置 Guid 主键生成器，可直接用于 <c>KeyGeneratorType = typeof(GuidKeyGenerator)</c>。
    /// </summary>
    public sealed class GuidKeyGenerator : IKeyGenerator<Guid>
    {
        /// <inheritdoc />
        public Guid Next() => Guid.NewGuid();

        object IKeyGenerator.Next() => Next();
    }
}
