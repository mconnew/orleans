using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProtoBuf.Serialization.Tests
{
    [Serializable]
    [Hagar.GenerateSerializer]
    public class OrleansType
    {
        [Hagar.Id(0)]
        public int val = 33;
        [Hagar.Id(1)]
        public string val2 = "Hello, world!";
        [Hagar.Id(2)]
        public int[] val3 = new[] { 1, 2 };

        public override bool Equals(object obj)
        {
            var o = obj as OrleansType;
            return o != null && val.Equals(o.val);
        }

        public override int GetHashCode()
        {
            return val;
        }
    }
}
