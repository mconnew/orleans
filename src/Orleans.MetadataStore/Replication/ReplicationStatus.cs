namespace Orleans.MetadataStore
{
    [GenerateSerializer]
    public enum ReplicationStatus
    {
        Failed,
        Uncertain,
        Success
    }
}