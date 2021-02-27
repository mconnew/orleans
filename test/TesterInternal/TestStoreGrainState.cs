using System;
using System.Globalization;
using Orleans;
using Orleans.Internal;

namespace UnitTests.Persistence
{
    [Serializable]
    [Hagar.GenerateSerializer]
    public class TestStoreGrainState
    {
        [Hagar.Id(0)]
        public string A { get; set; }
        [Hagar.Id(1)]
        public int B { get; set; }
        [Hagar.Id(2)]
        public long C { get; set; }

        internal static GrainState<TestStoreGrainState> NewRandomState(int? aPropertyLength = null)
        {
            return new GrainState<TestStoreGrainState>
            {
                State = new TestStoreGrainState
                {
                    A = aPropertyLength == null
                        ? ThreadSafeRandom.Next().ToString(CultureInfo.InvariantCulture)
                        : GenerateRandomDigitString(aPropertyLength.Value),
                    B = ThreadSafeRandom.Next(),
                    C = ThreadSafeRandom.Next()
                }
            };
        }

        private static string GenerateRandomDigitString(int stringLength)
        {
            var characters = new char[stringLength];
            for (var i = 0; i < stringLength; ++i)
            {
                characters[i] = (char)ThreadSafeRandom.Next('0', '9' + 1);
            }
            return new string(characters);
        }
    }
}


