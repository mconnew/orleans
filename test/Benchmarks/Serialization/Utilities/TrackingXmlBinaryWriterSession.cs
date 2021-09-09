using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Benchmarks.Serialization.Utilities
{
    internal class TrackingXmlBinaryWriterSession : XmlBinaryWriterSession
    {
        List<XmlDictionaryString> newStrings = new List<XmlDictionaryString>();

        public bool HasNewStrings
        {
            get { return newStrings.Count > 0; }
        }

        public IList<XmlDictionaryString> NewStrings => newStrings;

        public void ClearNew()
        {
            newStrings.Clear();
        }

        public override bool TryAdd(XmlDictionaryString value, out int key)
        {
            if (base.TryAdd(value, out key))
            {
                newStrings.Add(value);
                return true;
            }

            return false;
        }
    }
}
