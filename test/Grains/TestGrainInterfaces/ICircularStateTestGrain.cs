using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans;

namespace TestGrainInterfaces
{
    public interface ICircularStateTestGrain : IGrainWithGuidCompoundKey
    {
        Task<CircularTest1> GetState();
    }

    [Serializable]
    [Hagar.GenerateSerializer]
    public class CircularStateTestState
    {
        [Hagar.Id(0)]
        public CircularTest1 CircularTest1 { get; set; }
    }

    [Serializable]
    [Hagar.GenerateSerializer]
    public class CircularTest1
    {
        [Hagar.Id(0)]
        public CircularTest2 CircularTest2 { get; set; }
    }

    [Serializable]
    [Hagar.GenerateSerializer]
    public class CircularTest2
    {
        public CircularTest2()
        {
            CircularTest1List = new List<CircularTest1>();
        }

        [Hagar.Id(0)]
        public List<CircularTest1> CircularTest1List { get; set; }
    }
}
