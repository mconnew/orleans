using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans;
using Orleans.Concurrency;
using ProtoBuf;

namespace UnitTests.GrainInterfaces
{
    [Serializable]
    [Orleans.GenerateSerializer]
    public struct ValueTypeTestData
    {
        [Orleans.Id(0)]
        private readonly int intValue;

        public ValueTypeTestData(int i)
        {
            intValue = i;
        }

        public int GetValue()
        {
            return intValue;
        }
    }

    [Serializable]
    [Orleans.GenerateSerializer]
    public enum TestEnum : byte 
    {
        First,
        Second,
        Third
    }

    [Serializable]
    [Orleans.GenerateSerializer]
    public enum CampaignEnemyTestType : sbyte
    {
        None = -1,
        Brute = 0,
        Enemy1,
        Enemy2,
        Enemy3
    }

    [ProtoContract]
    [Serializable]
    [Orleans.GenerateSerializer]
    public class ClassWithEnumTestData
    {
        [ProtoMember(1)]
        [Orleans.Id(0)]
        public TestEnum EnumValue { get; set; }

        [ProtoMember(2)]
        [Orleans.Id(1)]
        public CampaignEnemyTestType Enemy { get; set; }
    }

    [ProtoContract]
    [Serializable]
    [Orleans.GenerateSerializer]
    public class LargeTestData
    {
        [ProtoMember(1)]
        [Orleans.Id(0)]
        public string TestString { get; set; }
        [ProtoMember(2)]
        [Orleans.Id(1)]
        private readonly bool[] boolArray;
        [ProtoMember(3)]
        [Orleans.Id(2)]
        protected Dictionary<string, int> stringIntDict;
        [ProtoMember(4)]
        [Orleans.Id(3)]
        public TestEnum EnumValue { get; set; }
        [ProtoMember(5)]
        [Orleans.Id(4)]
        private readonly ClassWithEnumTestData[] classArray;
        [ProtoMember(6)]
        [Orleans.Id(5)]
        public string Description { get; set; }

        public LargeTestData()
        {
            boolArray = new bool[20];
            stringIntDict = new Dictionary<string, int>();
            classArray = new ClassWithEnumTestData[50];
            for (var i = 0; i < 50; i++)
            {
                classArray[i] = new ClassWithEnumTestData();
            }
        }

        public void SetBit(int n, bool value = true)
        {
            boolArray[n] = value;
        }
        public bool GetBit(int n)
        {
            return boolArray[n];
        }
        public void SetEnemy(int n, CampaignEnemyTestType enemy)
        {
            classArray[n].Enemy = enemy;
        }
        public CampaignEnemyTestType GetEnemy(int n)
        {
            return classArray[n].Enemy;
        }
        public void SetNumber(string name, int value)
        {
            stringIntDict[name] = value;
        }
        public int GetNumber(string name)
        {
            return stringIntDict[name];
        }


        // This class is not actually used anywhere. It is here to test that the serializer generator properly handles
        // nested generic classes. If it doesn't, then the generated serializer for this class will fail to compile.
        [Serializable]
        [Orleans.GenerateSerializer]
        public class NestedGeneric<T>
        {
            [Orleans.Id(0)]
            private T myT;
            [Orleans.Id(1)]
            private string s;

            public NestedGeneric(T t)
            {
                myT = t;
                s = myT.ToString();
            }

            public override string ToString()
            {
                return s;
            }

            public void SetT(T t)
            {
                myT = t;
                s = myT.ToString();
            }
        }
    }

    public interface IValueTypeTestGrain : IGrainWithGuidKey
    {
        Task<ValueTypeTestData> GetStateData();

        Task SetStateData(ValueTypeTestData d);
    }

    public interface IRoundtripSerializationGrain : IGrainWithIntegerKey
    {
        Task<CampaignEnemyTestType> GetEnemyType();

        Task<object> GetClosedGenericValue();
    }

    [Serializable]
    [Immutable]
    [Orleans.GenerateSerializer]
    [Orleans.Immutable]
    public class ImmutableType
    {
        [Orleans.Id(0)]
        private readonly int a;
        [Orleans.Id(1)]
        private readonly int b;

        public int A { get { return a; } }
        public int B { get { return b; } }

        public ImmutableType(int aval, int bval)
        {
            a = aval;
            b = bval;
        }
    }

    [Serializable]
    [Orleans.GenerateSerializer]
    public class EmbeddedImmutable
    {
        [Orleans.Id(0)]
        public string A { get; set; }

        [Orleans.Id(1)]
        private readonly Immutable<List<int>> list;
        public Immutable<List<int>> B { get { return list; } }

        public EmbeddedImmutable(string a, params int[] listOfInts)
        {
            A = a;
            var l = new List<int>();
            l.AddRange(listOfInts);
            list = new Immutable<List<int>>(l);
        }
    }
}
