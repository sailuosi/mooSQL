namespace mooSQL.data.cluster
{
    public enum ReadRoutePolicy
    {
        MasterOnly,
        RoundRobin,
        WeightedRandom,
        FirstAvailable,
        Custom
    }
}
