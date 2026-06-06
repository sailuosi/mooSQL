using System;

namespace mooSQL.data.translation
{
    [Flags]
    public enum TranslationFlags
    {
        None = 0,
        Expression = 1,
        Sql = 1 << 1,
    }
}
