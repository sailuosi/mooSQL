namespace mooSQL.linq;

public static partial class DbFunc
{
    [Expression("COALESCE({0}, {1})", PreferServerSide = true)]
    public static T? Coalesce<T>(T? a, T b) where T : class => a ?? b;

    [Expression("COALESCE({0}, {1})", PreferServerSide = true)]
    public static T Coalesce<T>(T? a, T b) where T : struct => a ?? b;
}
