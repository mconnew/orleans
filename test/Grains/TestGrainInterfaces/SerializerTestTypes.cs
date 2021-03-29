using System;
using System.Runtime.Serialization;
using Orleans.CodeGeneration;
using Orleans.Serialization;

namespace UnitTests.GrainInterfaces
{
    /// <summary>
    /// A type with an <see cref="IOnDeserialized"/> hook, to test that it is correctly called by the internal serializers.
    /// </summary>
    [Serializable]
    [Hagar.GenerateSerializer]
    [Hagar.SerializationCallbacks(typeof(Orleans.Runtime.OnDeserializedCallbacks))]
    public class TypeWithOnDeserializedHook : IOnDeserialized
    {
        [NonSerialized]
        public ISerializerContext Context;

        [Hagar.Id(0)]
        public int Int { get; set; }

        void IOnDeserialized.OnDeserialized(ISerializerContext context)
        {
            this.Context = context;
        }
    }

    [Serializable]
    [Hagar.GenerateSerializer]
    public class BaseClassWithAutoProp
    {
        [Hagar.Id(0)]
        public int AutoProp { get; set; }
    }

    /// <summary>
    /// Code generation test to ensure that an overridden autoprop with a type which differs from
    /// the base autoprop is not used during serializer generation
    /// </summary>
    [Serializable]
    [Hagar.GenerateSerializer]
    public class SubClassOverridingAutoProp : BaseClassWithAutoProp
    {
        public new string AutoProp { get => base.AutoProp.ToString(); set => base.AutoProp = int.Parse(value); }
    }

    [KnownBaseType]
    public abstract class WellKnownBaseClass { }

    public class DescendantOfWellKnownBaseClass : WellKnownBaseClass
    {
        public int FavouriteNumber { get; set; }
    }

    [KnownBaseType]
    public interface IWellKnownBase { }

    public class ImplementsWellKnownInterface : IWellKnownBase
    {
        public int FavouriteNumber { get; set; }
    }

    public class NotDescendantOfWellKnownBaseType
    {
        public int FavouriteNumber { get; set; }
    }
}
