using System;
using Orleans;

namespace UnitTests.DtosRefOrleans
{
    [Serializable]
    [Hagar.GenerateSerializer]
    public class ClassReferencingOrleansTypeDto
    {
        static ClassReferencingOrleansTypeDto()
        {
            typeof(IGrain).ToString();
        }

        [Hagar.Id(0)]
        public string MyProperty { get; set; }
    }
}