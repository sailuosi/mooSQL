using System;

namespace mooSQL.data
{
    /// <summary>
    /// Turso Provider 显式注册。
    /// </summary>
    public static class TursoProviderExtensions
    {
        /// <summary>注册 Turso / libSQL 方言。</summary>
        public static BaseClientBuilder UseTurso(this BaseClientBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            return builder
                .useDialect(DataBaseType.Turso, () => new TursoDialect())
                .useDialect(DialectKeys.Turso, () => new TursoDialect())
                .useDialect("libsql", () => new TursoDialect())
                .useDialect("LibSQL", () => new TursoDialect());
        }

        /// <summary>向工厂注册 Turso。</summary>
        public static IDialectFactory UseTurso(this IDialectFactory factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            factory.useDialect(DataBaseType.Turso, () => new TursoDialect());
            factory.useDialect(DialectKeys.Turso, () => new TursoDialect());
            factory.useDialect("libsql", () => new TursoDialect());
            factory.useDialect("LibSQL", () => new TursoDialect());
            return factory;
        }
    }
}
