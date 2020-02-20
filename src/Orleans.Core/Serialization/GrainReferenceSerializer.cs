using System;
using Orleans.CodeGeneration;
using Orleans.Metadata.NewGrainRefSystem.NewNewNew;
using Orleans.Runtime;

namespace Orleans.Serialization
{
    [Serializer(typeof(GrainReference))]
    internal class GrainReferenceSerializer
    {
        private readonly GrainReferenceActivator activator;

        public GrainReferenceSerializer(GrainReferenceActivator activator)
        {
            this.activator = activator;
        }

        /// <summary> Serializer function for grain reference.</summary>
        /// <seealso cref="SerializationManager"/>
        [SerializerMethod]
        protected internal void SerializeGrainReference(object obj, ISerializationContext context, Type expected)
        {
            var writer = context.StreamWriter;
            var input = (GrainReference)obj;
            writer.Write(input.GrainId);
            context.SerializeInner(input.InterfaceType);
        }

        /// <summary> Deserializer function for grain reference.</summary>
        /// <seealso cref="SerializationManager"/>
        [DeserializerMethod]
        protected internal object DeserializeGrainReference(Type t, IDeserializationContext context)
        {
            var reader = context.StreamReader;
            GrainId id = reader.ReadGrainId();
            Type interfaceType = context.DeserializeInner(null) as Type;

            return activator.CreateGrainReference(id, interfaceType);
        }

        /// <summary> Copier function for grain reference. </summary>
        /// <seealso cref="SerializationManager"/>
        [CopierMethod]
        protected internal object CopyGrainReference(object original, ICopyContext context)
        {
            return (GrainReference)original;
        }
    }
}
