using Orleans;
using TestExtensions;

namespace UnitTests.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using GrainInterfaces;
    using Orleans.CodeGeneration;
    using Orleans.Runtime;
    using Orleans.Serialization;
    using Xunit;

    /// <summary>
    /// Tests for the serialization system.
    /// </summary>
    [Collection(TestEnvironmentFixture.DefaultCollection)]
    public class InternalSerializationTests
    {
        private readonly TestEnvironmentFixture fixture;

        /// <summary>
        /// Initializes the system for testing.
        /// </summary>
        public InternalSerializationTests(TestEnvironmentFixture fixture)
        {
            this.fixture = fixture;
        }

        /// <summary>
        /// Tests that grain references are serialized correctly.
        /// </summary>
        [Fact]
        [TestCategory("BVT")]
        [TestCategory("Functional")]
        [TestCategory("Serialization")]
        public void SerializationTests_GrainReference()
        {
            this.RunGrainReferenceSerializationTest<ISimpleGrain>();
        }

        /// <summary>
        /// Tests that generic grain references are serialized correctly.
        /// </summary>
        [Fact]
        [TestCategory("BVT")]
        [TestCategory("Functional")]
        [TestCategory("Serialization")]
        public void SerializationTests_GenericGrainReference()
        {
            this.RunGrainReferenceSerializationTest<ISimpleGenericGrain1<int>>();
        }

        private void RunGrainReferenceSerializationTest<TGrainInterface>()
        {
            var counters = new List<CounterStatistic>
            {
                CounterStatistic.FindOrCreate(StatisticNames.SERIALIZATION_BODY_FALLBACK_SERIALIZATION),
                CounterStatistic.FindOrCreate(StatisticNames.SERIALIZATION_BODY_FALLBACK_DESERIALIZATION),
                CounterStatistic.FindOrCreate(StatisticNames.SERIALIZATION_BODY_FALLBACK_DEEPCOPIES)
            };

            // Get the (generated) grain reference implementation for a particular grain interface type.
            var grainReference = this.GetGrainReference<TGrainInterface>();

            // Get the current value of each of the fallback serialization counters.
            var initial = counters.Select(_ => _.GetCurrentValue()).ToList();

            // Serialize and deserialize the grain reference.
            var writer = new BinaryTokenStreamWriter();
            this.fixture.SerializationManager.Serialize(grainReference, writer);
            var deserialized = this.fixture.SerializationManager.Deserialize(new BinaryTokenStreamReader(writer.ToByteArray()));
            var copy = (GrainReference)this.fixture.SerializationManager.DeepCopy(deserialized);

            // Get the final value of the fallback serialization counters.
            var final = counters.Select(_ => _.GetCurrentValue()).ToList();

            // Ensure that serialization was correctly performed.
            Assert.IsAssignableFrom(grainReference.GetType(), deserialized); //Deserialized GrainReference type should match original type

            var deserializedGrainReference = (GrainReference)deserialized;
            Assert.True(
                deserializedGrainReference.GrainId.Equals(grainReference.GrainId),
                "Deserialized GrainReference should have same GrainId as original value.");
            Assert.IsAssignableFrom(grainReference.GetType(), copy);
            Assert.True(
                copy.GrainId.Equals(grainReference.GrainId),
                "DeepCopy GrainReference should have same GrainId as original value.");

            // Check that the counters have not changed.
            var initialString = string.Join(",", initial);
            var finalString = string.Join(",", final);
            Assert.Equal(initialString, finalString); // GrainReference serialization should not use fallback serializer.
        }

        /// <summary>
        /// Returns a grain reference.
        /// </summary>
        /// <typeparam name="TGrainInterface">
        /// The grain interface type.
        /// </typeparam>
        /// <returns>
        /// The <see cref="GrainReference"/>.
        /// </returns>
        public GrainReference GetGrainReference<TGrainInterface>()
        {
            var originalGrainType = typeof(TGrainInterface);

            if (originalGrainType.IsGenericTypeDefinition)
            {
                throw new ArgumentException("Cannot create grain reference for non-concrete grain type");
            }

            // The type used in attributes will not be a constructed generic type, so if this is a constructed generic type then we need to
            // get its corresponding unconstructed type definition.
            var searchGrainType = originalGrainType.IsConstructedGenericType ? originalGrainType.GetGenericTypeDefinition() : originalGrainType;

            var type = originalGrainType.Assembly.DefinedTypes.First(
                _ =>
                {
                    var attr = _.GetCustomAttribute<GrainReferenceAttribute>();
                    return attr != null && attr.TargetType == searchGrainType;
                }).AsType();

            if (originalGrainType.IsConstructedGenericType)
            {
                type = type.MakeGenericType(originalGrainType.GetGenericArguments());
            }

            var regularGrainId = LegacyGrainId.GetGrainIdForTesting(Guid.NewGuid());
            var grainRef = this.fixture.InternalGrainFactory.GetGrain(regularGrainId);
            return
                (GrainReference)
                type.GetConstructor(
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.CreateInstance,
                    null,
                    new[] { typeof(GrainReference) },
                    null).Invoke(new object[] { grainRef });
        }
    }
}
