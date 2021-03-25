using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans.CodeGeneration;
using Orleans.Concurrency;
using Orleans.Runtime;

[assembly: GenerateSerializer(typeof(Orleans.Transactions.Abstractions.PendingTransactionState<>))]
[assembly: GenerateSerializer(typeof(Orleans.Transactions.Abstractions.TransactionalStorageLoadResponse<>))]

namespace Orleans.Transactions.Abstractions
{
    /// <summary>
    /// Storage interface for transactional state
    /// </summary>
    /// <typeparam name="TState">the type of the state</typeparam>
    public interface ITransactionalStateStorage<TState>
        where TState : class, new()
    {
        Task<TransactionalStorageLoadResponse<TState>> Load();

        Task<string> Store(

            string expectedETag,
            TransactionalStateMetaData metadata,

            // a list of transactions to prepare.
            List<PendingTransactionState<TState>> statesToPrepare,

            // if non-null, commit all pending transaction up to and including this sequence number.
            long? commitUpTo,

            // if non-null, abort all pending transactions with sequence numbers strictly larger than this one.
            long? abortAfter
        );
    }

    [Hagar.GenerateSerializer]
    [Serializable]
    [Immutable]
    public class PendingTransactionState<TState>
        where TState : class, new()
    {
        /// <summary>
        /// Transactions are given dense local sequence numbers 1,2,3,4...
        /// If a new transaction is prepared with the same sequence number as a 
        /// previously prepared transaction, it replaces it.
        /// </summary>
        [Hagar.Id(0)]
        public long SequenceId { get; set; }

        /// <summary>
        /// A globally unique identifier of the transaction. 
        /// </summary>
        [Hagar.Id(1)]
        public string TransactionId { get; set; }

        /// <summary>
        /// The logical timestamp of the transaction.
        /// Timestamps are guaranteed to be monotonically increasing.
        /// </summary>
        [Hagar.Id(2)]
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// The transaction manager that knows about the status of this prepared transaction,
        /// or null if this is the transaction manager.
        /// Used during recovery to inquire about the fate of the transaction.
        /// </summary>
        [Hagar.Id(3)]
        public ParticipantId TransactionManager { get; set; }

        /// <summary>
        /// A snapshot of the state after this transaction executed
        /// </summary>
        [Hagar.Id(4)]
        public TState State { get; set; }
    }

    [Hagar.GenerateSerializer]
    [Serializable]
    [Immutable]
    public class TransactionalStorageLoadResponse<TState>
        where TState : class, new()
    {
        public TransactionalStorageLoadResponse() : this(null, new TState(), 0, new TransactionalStateMetaData(), Array.Empty<PendingTransactionState<TState>>()) { }

        public TransactionalStorageLoadResponse(string etag, TState committedState, long committedSequenceId, TransactionalStateMetaData metadata, IReadOnlyList<PendingTransactionState<TState>> pendingStates)
        {
            this.ETag = etag;
            this.CommittedState = committedState;
            this.CommittedSequenceId = committedSequenceId;
            this.Metadata = metadata;
            this.PendingStates = pendingStates;
        }

        [Hagar.Id(0)]
        public string ETag { get; set; }

        [Hagar.Id(1)]
        public TState CommittedState { get; set; }

        /// <summary>
        /// The local sequence id of the last committed transaction, or zero if none
        /// </summary>
        [Hagar.Id(2)]
        public long CommittedSequenceId { get; set; }

        /// <summary>
        /// Additional state maintained by the transaction algorithm, such as commit records
        /// </summary>
        [Hagar.Id(3)]
        public TransactionalStateMetaData Metadata { get; set; }

        /// <summary>
        /// List of pending states, ordered by sequence id
        /// </summary>
        [Hagar.Id(4)]
        public IReadOnlyList<PendingTransactionState<TState>> PendingStates { get; set; }
    }

    /// <summary>
    /// Metadata is stored in storage, as a JSON object
    /// </summary>
    [Hagar.GenerateSerializer]
    [Serializable]
    public class TransactionalStateMetaData
    {
        [Hagar.Id(0)]
        public DateTime TimeStamp { get; set; } = default;

        [Hagar.Id(1)]
        public Dictionary<Guid, CommitRecord> CommitRecords { get; set; } = new Dictionary<Guid, CommitRecord>();
    }

    [Hagar.GenerateSerializer]
    [Serializable]
    [Immutable]
    public class CommitRecord
    {
        [Hagar.Id(0)]
        public DateTime Timestamp { get; set; }

        [Hagar.Id(1)]
        public List<ParticipantId> WriteParticipants { get; set; }
    }
}
