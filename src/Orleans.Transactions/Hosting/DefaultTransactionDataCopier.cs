using Orleans.Transactions.Abstractions;

namespace Orleans.Transactions
{
    public class DefaultTransactionDataCopier<TData> : ITransactionDataCopier<TData>
    {
        private readonly Orleans.DeepCopier<TData> deepCopier;

        public DefaultTransactionDataCopier(Orleans.DeepCopier<TData> deepCopier)
        {
            this.deepCopier = deepCopier;
        }

        public TData DeepCopy(TData original)
        {
            return (TData)this.deepCopier.Copy(original);
        }
    }
}
