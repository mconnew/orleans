using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans.Runtime;

namespace TesterInternal
{
    [Serializable]
    [Hagar.GenerateSerializer]
    public class GrainStateContainingGrainReferences
    {
        [Hagar.Id(0)]
        public IAddressable Grain { get; set; }
        [Hagar.Id(1)]
        public List<IAddressable> GrainList { get; set; }
        [Hagar.Id(2)]
        public Dictionary<string, IAddressable> GrainDict { get; set; }

        public GrainStateContainingGrainReferences()
        {
            GrainList = new List<IAddressable>();
            GrainDict = new Dictionary<string, IAddressable>();
        }
    }
}
