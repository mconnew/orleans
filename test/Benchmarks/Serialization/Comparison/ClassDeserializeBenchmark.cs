using BenchmarkDotNet.Attributes;
using Benchmarks.Models;
using Benchmarks.Utilities;
using Orleans.Serialization;
using Orleans.Serialization.Buffers;
using Orleans.Serialization.Session;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.IO;
using Xunit;
using SerializerSession = Orleans.Serialization.Session.SerializerSession;
using Utf8JsonNS = Utf8Json;
using Hyperion;
using System.Xml;
using System.Runtime.Serialization;
using Benchmarks.Serialization.Utilities;
using System.Text;

namespace Benchmarks.Comparison
{
    [Trait("Category", "Benchmark")]
    [Config(typeof(BenchmarkConfig))]
    //[DisassemblyDiagnoser(recursiveDepth: 2, printSource: true)]
    //[EtwProfiler]
    public class ClassDeserializeBenchmark
    {
        private static readonly MemoryStream ProtoInput;

        private static readonly byte[] MsgPackInput = MessagePack.MessagePackSerializer.Serialize(IntClass.Create());

        private static readonly string NewtonsoftJsonInput = JsonConvert.SerializeObject(IntClass.Create());

        private static readonly byte[] SpanJsonInput = SpanJson.JsonSerializer.Generic.Utf8.Serialize(IntClass.Create());

        private static readonly Hyperion.Serializer HyperionSerializer = new(new SerializerOptions(knownTypes: new[] { typeof(IntClass) }));
        private static readonly MemoryStream HyperionInput;

        private static readonly Serializer<IntClass> Serializer;
        private static readonly byte[] Input;
        private static readonly SerializerSession Session;

        private static readonly DeserializerSession HyperionSession;

        private static readonly Utf8JsonNS.IJsonFormatterResolver Utf8JsonResolver = Utf8JsonNS.Resolvers.StandardResolver.Default;
        private static readonly byte[] Utf8JsonInput;

        private static readonly byte[] SystemTextJsonInput;

        private static MemoryStream _dcsXmlBuffer = new(4096);
        private static MemoryStream _dcsBuffer;
        private static XmlDictionaryReader _dcsReader;
        private static XmlBinaryReaderSession _xmlBinaryReaderSession;
        private static DataContractSerializer _dcs = new(typeof(IntClass));

        static ClassDeserializeBenchmark()
        {
            ProtoInput = new MemoryStream();
            ProtoBuf.Serializer.Serialize(ProtoInput, IntClass.Create());

            HyperionInput = new MemoryStream();
            HyperionSession = HyperionSerializer.GetDeserializerSession();
            HyperionSerializer.Serialize(IntClass.Create(), HyperionInput);

            // 
            var services = new ServiceCollection()
                .AddSerializer()
                .BuildServiceProvider();
            Serializer = services.GetRequiredService<Serializer<IntClass>>();
            var bytes = new byte[1000];
            Session = services.GetRequiredService<SerializerSessionPool>().GetSession();
            var writer = new SingleSegmentBuffer(bytes).CreateWriter(Session);
            Serializer.Serialize(IntClass.Create(), ref writer);
            Input = bytes;

            Utf8JsonInput = Utf8JsonNS.JsonSerializer.Serialize(IntClass.Create(), Utf8JsonResolver);

            var stream = new MemoryStream();
            using (var jsonWriter = new System.Text.Json.Utf8JsonWriter(stream))
            {
                System.Text.Json.JsonSerializer.Serialize(jsonWriter, IntClass.Create());
            }

            SystemTextJsonInput = stream.ToArray();

            _dcsBuffer = new MemoryStream();
            TrackingXmlBinaryWriterSession xmlBinaryWriterSession = new TrackingXmlBinaryWriterSession();
            var dcsWriter = XmlDictionaryWriter.CreateBinaryWriter(_dcsBuffer, new XmlDictionary(), xmlBinaryWriterSession, false);
            _dcs.WriteObject(dcsWriter, IntClass.Create());
            dcsWriter.Flush();
            dcsWriter.Close();
            _xmlBinaryReaderSession = new XmlBinaryReaderSession();
            if (xmlBinaryWriterSession.HasNewStrings)
            {
                int dictionaryId = 0;
                foreach (var newString in xmlBinaryWriterSession.NewStrings)
                {
                    _xmlBinaryReaderSession.Add(dictionaryId++, newString.Value);
                }
            }

            _dcsBuffer.Position = 0;
            _dcsReader = XmlDictionaryReader.CreateBinaryReader(_dcsBuffer, new XmlDictionary(), XmlDictionaryReaderQuotas.Max, _xmlBinaryReaderSession, null);
            _dcs.WriteObject(_dcsXmlBuffer, IntClass.Create());
        }

        private static int SumResult(IntClass result) => result.MyProperty1 +
                   result.MyProperty2 +
                   result.MyProperty3 +
                   result.MyProperty4 +
                   result.MyProperty5 +
                   result.MyProperty6 +
                   result.MyProperty7 +
                   result.MyProperty8 +
                   result.MyProperty9;

        [Fact]
        [Benchmark(Baseline = true)]
        public int Orleans()
        {
            Session.FullReset();
            var instance = Serializer.Deserialize(Input, Session);
            return SumResult(instance);
        }

        [Benchmark]
        public int DataContractSerializer()
        {
            _dcsBuffer.Position = 0;
            ((IXmlBinaryReaderInitializer)_dcsReader).SetInput(_dcsBuffer, new XmlDictionary(), XmlDictionaryReaderQuotas.Max, _xmlBinaryReaderSession, null);
            var instance = (IntClass)_dcs.ReadObject(_dcsReader);
            return SumResult(instance);
        }

        [Benchmark]
        public int DataContractSerializerXml()
        {
            _dcsXmlBuffer.Position = 0;
            var instance = (IntClass)_dcs.ReadObject(_dcsXmlBuffer);
            return SumResult(instance);
        }

        [Benchmark]
        public int Utf8Json() => SumResult(Utf8JsonNS.JsonSerializer.Deserialize<IntClass>(Utf8JsonInput, Utf8JsonResolver));

        [Benchmark]
        public int SystemTextJson() => SumResult(System.Text.Json.JsonSerializer.Deserialize<IntClass>(SystemTextJsonInput));

        [Benchmark]
        public int MessagePackCSharp() => SumResult(MessagePack.MessagePackSerializer.Deserialize<IntClass>(MsgPackInput));

        [Benchmark]
        public int ProtobufNet()
        {
            ProtoInput.Position = 0;
            return SumResult(ProtoBuf.Serializer.Deserialize<IntClass>(ProtoInput));
        }

        [Benchmark]
        public int Hyperion()
        {
            HyperionInput.Position = 0;

            return SumResult(HyperionSerializer.Deserialize<IntClass>(HyperionInput, HyperionSession));
        }

        [Benchmark]
        public int NewtonsoftJson() => SumResult(JsonConvert.DeserializeObject<IntClass>(NewtonsoftJsonInput));

        [Benchmark(Description = "SpanJson")]
        public int SpanJsonUtf8() => SumResult(SpanJson.JsonSerializer.Generic.Utf8.Deserialize<IntClass>(SpanJsonInput));
    }
}