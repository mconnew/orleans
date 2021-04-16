using System;

namespace Orleans.Transactions.TestKit.Consistency
{
    [Serializable]
    [Orleans.GenerateSerializer]
    public struct Observation
    {
        [Orleans.Id(0)]
        public int Grain { get; set; }
        [Orleans.Id(1)]
        public int SeqNo { get; set; }
        [Orleans.Id(2)]
        public string WriterTx { get; set; }
        [Orleans.Id(3)]
        public string ExecutingTx { get; set; }
    }
}
