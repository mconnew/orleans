using System;

namespace UnitTests.Grains
{
    public enum IntEnum
    {
        Value1,
        Value2,
        Value3
    }

    public enum UShortEnum : ushort
    {
        Value1,
        Value2,
        Value3
    }

    public enum CampaignEnemyType : sbyte
    {
        None = -1,
        Brute = 0,
        Enemy1,
        Enemy2,
        Enemy3,
        Enemy4,
    }

    public class UnserializableException : Exception
    {
        public UnserializableException(string message) : base(message)
        { }
    }

    [Serializable]
    [Orleans.GenerateSerializer]
    public class Unrecognized
    {
        [Orleans.Id(0)]
        public int A { get; set; }
        [Orleans.Id(1)]
        public int B { get; set; }
    }

    [Serializable]
    [Orleans.GenerateSerializer]
    public class ClassWithCustomSerializer
    {
        [Orleans.Id(0)]
        public int IntProperty { get; set; }
        [Orleans.Id(1)]
        public string StringProperty { get; set; }

        public static int SerializeCounter { get; set; }
        public static int DeserializeCounter { get; set; }

        static ClassWithCustomSerializer()
        {
            SerializeCounter = 0;
            DeserializeCounter = 0;
        }
    }
}
