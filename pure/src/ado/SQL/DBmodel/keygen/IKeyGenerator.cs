namespace mooSQL.data
{
    /// <summary>应用侧主键生成器（强类型 / <c>KeyGeneratorType</c>）。</summary>
    public interface IKeyGenerator
    {
        /// <summary>生成下一个主键值。</summary>
        object Next();
    }

    /// <summary>泛型主键生成器。</summary>
    public interface IKeyGenerator<T> : IKeyGenerator
    {
        /// <summary>生成下一个主键值。</summary>
        new T Next();
    }
}
