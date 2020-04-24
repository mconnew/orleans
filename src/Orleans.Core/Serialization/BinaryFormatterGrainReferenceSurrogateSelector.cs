using System;
using System.Runtime.Serialization;
using Orleans.GrainReferences;
using Orleans.Runtime;

namespace Orleans.Serialization
{
    public class BinaryFormatterGrainReferenceSurrogateSelector : ISurrogateSelector
    {
        private readonly BinaryFormatterGrainReferenceSurrogate _surrogate;
        private ISurrogateSelector _chainedSelector;

        public BinaryFormatterGrainReferenceSurrogateSelector(GrainReferenceActivator activator)
        {
            _surrogate = new BinaryFormatterGrainReferenceSurrogate(activator);
        }

        public void ChainSelector(ISurrogateSelector selector)
        {
            if (_chainedSelector is null)
            {
                _chainedSelector = selector;
            }
            else
            {
                _chainedSelector.ChainSelector(selector);
            }
        }

        public ISurrogateSelector GetNextSelector() => _chainedSelector;

        public ISerializationSurrogate GetSurrogate(Type type, StreamingContext context, out ISurrogateSelector selector)
        {
            if (typeof(GrainReference).IsAssignableFrom(type))
            {
                selector = this;
                return _surrogate;
            }

            if (_chainedSelector is object)
            {
                return _chainedSelector.GetSurrogate(type, context, out selector);
            }

            selector = null;
            return null;
        }
    }

    public class BinaryFormatterGrainReferenceSurrogate : ISerializationSurrogate
    {
        private readonly GrainReferenceActivator _activator;
        public BinaryFormatterGrainReferenceSurrogate(GrainReferenceActivator activator)
        {
            _activator = activator;
        }

        public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
        {
            var typed = (GrainReference)obj;
            info.AddValue("type", typed.GrainId.Type.ToStringUtf8(), typeof(string));
            info.AddValue("key", typed.GrainId.Key.ToStringUtf8(), typeof(string));
            info.AddValue("interface", typed.InterfaceId.ToStringUtf8(), typeof(string));
        }

        public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
        {
            var id = GrainId.Create(info.GetString("type"), info.GetString("key"));
            var iface = GrainInterfaceId.Create(info.GetString("interface"));
            return _activator.CreateReference(id, iface);
        }
    }
}
