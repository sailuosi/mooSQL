namespace mooSQL.data.cluster
{
    public enum FailoverMode
    {
        Disabled,
        MarkOnly,
        OnNextConnect,
        ImmediateOnFailure
    }
}
