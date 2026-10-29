using System;

namespace mooSQL.data
{
    /// <summary>
    /// EDB Provider 显式注册。
    /// </summary>
    public static class EDBProviderExtensions
    {
        /// <summary>注册 EnterpriseDB 方言。</summary>
        public static BaseClientBuilder UseEDB(this BaseClientBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            return builder
                .useDialect(DataBaseType.EDB, () => new EDBDialect())
                .useDialect(DialectKeys.EDB, () => new EDBDialect())
                .useDialect("EnterpriseDB", () => new EDBDialect());
        }

        /// <summary>向工厂注册 EDB。</summary>
        public static IDialectFactory UseEDB(this IDialectFactory factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            factory.useDialect(DataBaseType.EDB, () => new EDBDialect());
            factory.useDialect(DialectKeys.EDB, () => new EDBDialect());
            factory.useDialect("EnterpriseDB", () => new EDBDialect());
            return factory;
        }
    }
}
