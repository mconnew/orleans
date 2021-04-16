using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Transactions.TestKit.Consistency
{
    [Serializable]
    [Orleans.GenerateSerializer]
    public class ConsistencyTestOptions
    {
        [Orleans.Id(0)]
        public int RandomSeed { get; set; } = 0;
        [Orleans.Id(1)]
        public int NumGrains { get; set; } = 50;
        [Orleans.Id(2)]
        public int MaxDepth { get; set; } = 5;
        [Orleans.Id(3)]
        public bool AvoidDeadlocks { get; set; } = true;
        [Orleans.Id(4)]
        public bool AvoidTimeouts { get; set; } = true;
        [Orleans.Id(5)]
        public ReadWriteDetermination ReadWrite { get; set; } = ReadWriteDetermination.PerGrain;
        [Orleans.Id(6)]
        public long GrainOffset { get; set; }

        public const int MaxGrains = 100000;
    }

    public enum ReadWriteDetermination
    {
        PerTransaction, PerGrain, PerAccess
    }
}
