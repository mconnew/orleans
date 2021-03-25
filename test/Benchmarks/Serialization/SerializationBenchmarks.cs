using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Configuration;
using Orleans.Runtime;
using Orleans.Runtime.Configuration;
using Orleans.Runtime.Messaging;
using Orleans.Serialization;
using Orleans.Serialization.ProtobufNet;
using UnitTests.GrainInterfaces;

namespace Benchmarks.Serialization
{
    public enum SerializerToUse
    {
        Default,
        IlBasedFallbackSerializer,
        ProtoBufNet
    }

    [MemoryDiagnoser]
    public class SerializationBenchmarks
    {
        private void InitializeSerializer(SerializerToUse serializerToUse)
        {
            Type fallback = null;
            switch (serializerToUse)
            {
                case SerializerToUse.Default:
                    break;
                case SerializerToUse.IlBasedFallbackSerializer:
                    fallback = typeof(ILBasedSerializer);
                    break;
                case SerializerToUse.ProtoBufNet:
                    fallback = typeof(ProtobufNetSerializer);
                    break;
                default:
                    throw new InvalidOperationException("Invalid Serializer was selected");
            }

            var client = new ClientBuilder()
                .UseLocalhostClustering()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = nameof(SerializationBenchmarks);
                    options.ServiceId = Guid.NewGuid().ToString();
                })
                .Configure<SerializationProviderOptions>(
                    options => options.FallbackSerializationProvider = fallback)
                .Build();
            this.serializationManager = client.ServiceProvider.GetRequiredService<SerializationManager>();
        }
        
        [Params(SerializerToUse.IlBasedFallbackSerializer, SerializerToUse.Default, SerializerToUse.ProtoBufNet)]
        public SerializerToUse Serializer { get; set; }

        private OuterClass.SomeConcreteClass complexClass;
        private Message.HeadersContainer messageHeaders;
        private byte[] serializedBytes;
        private ReadOnlySequence<byte> headerBytes;
        private LargeTestData largeTestData;
        private SerializationManager serializationManager;
        private TestSingleSegmentBufferWriter bufferWriter;
        private MessageSerializer.HeadersSerializer headerSerializer;

        [GlobalSetup]
        public void BenchmarkSetup()
        {
            this.InitializeSerializer(this.Serializer);

            this.complexClass = OuterClass.GetPrivateClassInstance();
            this.complexClass.Int = 89;
            this.complexClass.String = Guid.NewGuid().ToString();
            this.complexClass.NonSerializedInt = 39;
            var classes = new List<SomeAbstractClass>
            {
                this.complexClass,
                new AnotherConcreteClass
                {
                    AnotherString = "hi",
                    Interfaces = new List<ISomeInterface>
                    {
                        this.complexClass
                    }
                },
                new AnotherConcreteClass(),
                OuterClass.GetPrivateClassInstance()
            };
            
            this.complexClass.Classes = classes.ToArray();
            this.complexClass.Enum = SomeAbstractClass.SomeEnum.Something;
            this.complexClass.SetObsoleteInt(38);

            this.complexClass.Struct = new SomeStruct(10)
            {
                Id = Guid.NewGuid(),
                PublicValue = 6,
                ValueWithPrivateGetter = 7
            };
            this.complexClass.Struct.SetValueWithPrivateSetter(8);
            this.complexClass.Struct.SetPrivateValue(9);


            this.largeTestData = new LargeTestData
            {
                Description = "This is a test. This is only a test. In the event of a real execution, this would contain actual data.",
                EnumValue = TestEnum.First
            };
            this.largeTestData.SetBit(13);
            this.largeTestData.SetEnemy(17, CampaignEnemyTestType.Enemy1);

            var body = new Response("yess!");
            messageHeaders = (new Message
            {
                TargetActivation = ActivationId.NewId(),
                TargetSilo = SiloAddress.New(IPEndPoint.Parse("210.50.4.44:40902"), 5423123),
                TargetGrain = GrainId.Create("sys.mygrain", "borken_thee_doggo"),
                BodyObject = body,
                InterfaceType = GrainInterfaceType.Create("imygrain"),
                SendingActivation = ActivationId.NewId(),
                SendingSilo = SiloAddress.New(IPEndPoint.Parse("10.50.4.44:40902"), 5423123),
                SendingGrain = GrainId.Create("sys.mygrain", "fluffy_g"),
                TraceContext = new TraceContext { ActivityId = Guid.NewGuid() },
                Id = CorrelationId.GetNext()
            }).Headers;

            this.serializedBytes = this.serializationManager.SerializeToByteArray(this.largeTestData);

            this.bufferWriter = new TestSingleSegmentBufferWriter(new byte[10000]); 
            this.headerSerializer = new MessageSerializer.HeadersSerializer(this.serializationManager);
            this.headerSerializer.Serialize(this.bufferWriter, this.messageHeaders);
            this.headerBytes = this.bufferWriter.GetReadOnlySequence();
            this.bufferWriter.Reset();
        }

        public class TestSingleSegmentBufferWriter : IBufferWriter<byte>
        {
            private readonly byte[] _buffer;
            private int _written;

            public TestSingleSegmentBufferWriter(byte[] buffer)
            {
                _buffer = buffer;
                _written = 0;
            }

            public void Advance(int bytes) => _written += bytes;

            public Memory<byte> GetMemory(int sizeHint = 0) => _buffer.AsMemory().Slice(_written);

            public Span<byte> GetSpan(int sizeHint) => _buffer.AsSpan().Slice(_written);

            public void Reset() => _written = 0;

            public ReadOnlySequence<byte> GetReadOnlySequence()
            {
                var bytes = new byte[_written];
                _buffer.AsSpan(0, _written).CopyTo(bytes);
                return new ReadOnlySequence<byte>(bytes);
            }
        }

        [Benchmark]
        public void SerializeHeaders()
        {
            this.bufferWriter.Reset();
            this.headerSerializer.Serialize(this.bufferWriter, this.messageHeaders);
        }

        [Benchmark]
        public object DeserializerHeaders()
        {
            this.headerSerializer.Deserialize(this.headerBytes, out var result);
            return result;
        }

        //[Benchmark]
        public byte[] SerializerBenchmark()
        {
            return this.serializationManager.SerializeToByteArray(this.largeTestData);
        }

        //[Benchmark]
        public object DeserializerBenchmark()
        {
            return this.serializationManager.DeserializeFromByteArray<LargeTestData>(this.serializedBytes);
        }

        /// <summary>
        /// Performs a full serialization loop using a type which has not had code generation performed.
        /// </summary>
        /// <returns></returns>
        //[Benchmark]
        public object FallbackFullLoop()
        {
            return OrleansSerializationLoop(this.complexClass);
        }

        internal object OrleansSerializationLoop(object input, bool includeWire = true)
        {
            var copy = this.serializationManager.DeepCopy(input);
            if (includeWire)
            {
                copy = this.serializationManager.RoundTripSerializationForTesting(copy);
            }
            return copy;
        }
    }

    [Serializable]
    [Hagar.GenerateSerializer]
    internal struct SomeStruct
    {
        [Hagar.Id(0)]
        public Guid Id { get; set; }
        [Hagar.Id(1)]
        public int PublicValue { get; set; }
        [Hagar.Id(2)]
        public int ValueWithPrivateSetter { get; private set; }
        [Hagar.Id(3)]
        public int ValueWithPrivateGetter { private get; set; }
        [Hagar.Id(4)]
        private int PrivateValue { get; set; }

        [Hagar.Id(5)]
        public readonly int ReadonlyField;

        public SomeStruct(int readonlyField)
            : this()
        {
            this.ReadonlyField = readonlyField;
        }

        public int GetValueWithPrivateGetter()
        {
            return this.ValueWithPrivateGetter;
        }

        public int GetPrivateValue()
        {
            return this.PrivateValue;
        }

        public void SetPrivateValue(int value)
        {
            this.PrivateValue = value;
        }

        public void SetValueWithPrivateSetter(int value)
        {
            this.ValueWithPrivateSetter = value;
        }
    }

    internal interface ISomeInterface { int Int { get; set; } }

    [Serializable]
    [Hagar.GenerateSerializer]
    internal abstract class SomeAbstractClass : ISomeInterface
    {
        [NonSerialized]
        private int nonSerializedIntField;

        [Hagar.Id(0)]
        public abstract int Int { get; set; }

        [Hagar.Id(1)]
        public List<ISomeInterface> Interfaces { get; set; }

        [Hagar.Id(2)]
        public SomeAbstractClass[] Classes { get; set; }

        [Obsolete("This field should not be serialized", true)]
        [Hagar.Id(3)]
        public int ObsoleteIntWithError { get; set; }

        [Obsolete("This field should be serialized")]
        [Hagar.Id(4)]
        public int ObsoleteInt { get; set; }


#pragma warning disable 618
        public int GetObsoleteInt() => this.ObsoleteInt;
        public void SetObsoleteInt(int value)
        {
            this.ObsoleteInt = value;
        }
#pragma warning restore 618

        [Hagar.Id(5)]
        public SomeEnum Enum { get; set; }

        public int NonSerializedInt
        {
            get
            {
                return this.nonSerializedIntField;
            }

            set
            {
                this.nonSerializedIntField = value;
            }
        }

        [Serializable]
        public enum SomeEnum
        {
            None,

            Something,

            SomethingElse
        }
    }

    internal class OuterClass
    {
        public static SomeConcreteClass GetPrivateClassInstance() => new PrivateConcreteClass(Guid.NewGuid());

        public static Type GetPrivateClassType() => typeof(PrivateConcreteClass);

        [Serializable]
        [Hagar.GenerateSerializer]
        public class SomeConcreteClass : SomeAbstractClass
        {
            [Hagar.Id(0)]
            public override int Int { get; set; }

            [Hagar.Id(1)]
            public string String { get; set; }

            [Hagar.Id(2)]
            public SomeStruct Struct { get; set; }

            [Hagar.Id(3)]
            private PrivateConcreteClass secretPrivateClass;

            public void ConfigureSecretPrivateClass()
            {
                this.secretPrivateClass = new PrivateConcreteClass(Guid.NewGuid());
            }

            public bool AreSecretBitsIdentitcal(SomeConcreteClass other)
            {
                return other.secretPrivateClass?.Identity == this.secretPrivateClass?.Identity;
            }
        }

        [Serializable]
        [Hagar.GenerateSerializer]
        private class PrivateConcreteClass : SomeConcreteClass
        {
            public PrivateConcreteClass(Guid identity)
            {
                this.Identity = identity;
            }

            [Hagar.Id(0)]
            public readonly Guid Identity;
        }
    }

    [Serializable]
    [Hagar.GenerateSerializer]
    internal class AnotherConcreteClass : SomeAbstractClass
    {
        [Hagar.Id(0)]
        public override int Int { get; set; }

        [Hagar.Id(1)]
        public string AnotherString { get; set; }
    }

    [Serializable]
    [Hagar.GenerateSerializer]
    internal class InnerType
    {
        public InnerType()
        {
            this.Id = Guid.NewGuid();
            this.Something = this.Id.ToString();
        }

        [Hagar.Id(0)]
        public Guid Id { get; set; }
        [Hagar.Id(1)]
        public string Something { get; set; }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((InnerType)obj);
        }

        protected bool Equals(InnerType other)
        {
            return this.Id.Equals(other.Id) && string.Equals(this.Something, other.Something);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (this.Id.GetHashCode() * 397) ^ (this.Something?.GetHashCode() ?? 0);
            }
        }
    }
}
