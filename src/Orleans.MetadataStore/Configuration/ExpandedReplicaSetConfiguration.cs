namespace Orleans.MetadataStore
{
    /// <summary>
    /// Combines <see cref="ReplicaSetConfiguration"/> with a corresponding set of references to store instances on each node.
    /// </summary>
    public class ExpandedReplicaSetConfiguration
    {
        public ExpandedReplicaSetConfiguration(
            ReplicaSetConfiguration configuration,
            IRemoteMetadataStore[][] storeReferences)
        {
            Configuration = configuration;
            StoreReferences = storeReferences;
        }

        public IRemoteMetadataStore[][] StoreReferences { get; }
        public ReplicaSetConfiguration Configuration { get; }

        public static ExpandedReplicaSetConfiguration Create(
            ReplicaSetConfiguration config,
            MetadataStoreOptions options,
            IStoreReferenceFactory factory)
        {
            IRemoteMetadataStore[][] refs;
            if (config?.Members != null)
            {
                refs = new IRemoteMetadataStore[config.Members.Length][];
                for (var i = 0; i < config.Members.Length; i++)
                {
                    var instances = refs[i] = new IRemoteMetadataStore[options.InstancesPerSilo];

                    for (short j = 0; j < options.InstancesPerSilo; j++)
                    {
                        instances[j] = factory.GetReference(config.Members[i], j);
                    }
                }
            }
            else
            {
                refs = null;
            }

            return new ExpandedReplicaSetConfiguration(config, refs);
        }
    }
}