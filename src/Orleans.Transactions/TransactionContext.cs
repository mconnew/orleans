
using System.Collections.Generic;
using Orleans.Runtime;

namespace Orleans.Transactions
{
    public static class TransactionContext
    {
        internal const string TransactionInfoHeader = "#TC_TI";
        internal const string Orleans_TransactionContext_Key = "#ORL_TC";

        public static TransactionInfo GetTransactionInfo()
        {
            var values = GetContextData();
            if (values != null && values.TryGetValue(TransactionInfoHeader, out var result))
            {
                return result as TransactionInfo;
            }

            return null;
        }

        public static string CurrentTransactionId => GetRequiredTransactionInfo().Id;

        public static TransactionInfo GetRequiredTransactionInfo()
        {
            return GetTransactionInfo() ?? throw new OrleansTransactionException($"A transaction context is required for access. Did you forget a [Transaction] attribute?");
        }

        internal static void SetTransactionInfo(TransactionInfo info)
        {
            Dictionary<string, object> values = GetContextData();

            values = values == null ? new Dictionary<string, object>() : new Dictionary<string, object>(values);
            values[TransactionInfoHeader] = info;
            SetContextData(values);
        }

        internal static void Clear()
        {
            // Remove the key to prevent passing of its value from this point on
            RequestContext.Remove(Orleans_TransactionContext_Key);
        }

        private static void SetContextData(Dictionary<string, object> values)
        {
            RequestContext.Set(Orleans_TransactionContext_Key, values);
        }

        private static Dictionary<string, object> GetContextData()
        {
            return (Dictionary<string, object>)RequestContext.Get(Orleans_TransactionContext_Key);
        }
    }
}
