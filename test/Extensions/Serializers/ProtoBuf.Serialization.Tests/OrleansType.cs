using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProtoBuf.Serialization.Tests
{
    [Serializable]
    [Orleans.GenerateSerializer]
    public class OrleansType
    {
        [Orleans.Id(0)]
        public int val = 33;
        [Orleans.Id(1)]
        public string val2 = "Hello, world!";
        [Orleans.Id(2)]
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
