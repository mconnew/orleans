using System;
using System.Collections.Generic;

namespace UnitTests.GrainInterfaces
{
    [Serializable]
    [Hagar.GenerateSerializer]
    public class TestTypeA
    {
        [Hagar.Id(0)]
        public ICollection<TestTypeA> Collection { get; set; }
    }
}
