using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Transactions.TestKit.Consistency
{
    [Serializable]
    [Hagar.GenerateSerializer]
    public class ConsistencyTestOptions
    {
        [Hagar.Id(0)]
        public int RandomSeed { get; set; } = 0;
        [Hagar.Id(1)]
        public int NumGrains { get; set; } = 50;
        [Hagar.Id(2)]
        public int MaxDepth { get; set; } = 5;
        [Hagar.Id(3)]
        public bool AvoidDeadlocks { get; set; } = true;
        [Hagar.Id(4)]
        public bool AvoidTimeouts { get; set; } = true;
        [Hagar.Id(5)]
        public ReadWriteDetermination ReadWrite { get; set; } = ReadWriteDetermination.PerGrain;
        [Hagar.Id(6)]
        public long GrainOffset { get; set; }

        public const int MaxGrains = 100000;
    }

    public enum ReadWriteDetermination
    {
        PerTransaction, PerGrain, PerAccess
    }
}
