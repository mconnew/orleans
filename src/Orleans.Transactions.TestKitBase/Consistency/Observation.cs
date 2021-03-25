using System;

namespace Orleans.Transactions.TestKit.Consistency
{
    [Serializable]
    [Hagar.GenerateSerializer]
    public struct Observation
    {
        [Hagar.Id(0)]
        public int Grain { get; set; }
        [Hagar.Id(1)]
        public int SeqNo { get; set; }
        [Hagar.Id(2)]
        public string WriterTx { get; set; }
        [Hagar.Id(3)]
        public string ExecutingTx { get; set; }
    }
}
