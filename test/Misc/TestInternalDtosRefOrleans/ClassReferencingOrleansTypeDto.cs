using System;
using Orleans;

namespace UnitTests.DtosRefOrleans
{
    [Serializable]
    [Orleans.GenerateSerializer]
    public class ClassReferencingOrleansTypeDto
    {
        static ClassReferencingOrleansTypeDto()
        {
            typeof(IGrain).ToString();
        }

        [Orleans.Id(0)]
        public string MyProperty { get; set; }
    }
}