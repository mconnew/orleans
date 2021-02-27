using System;
using System.Threading.Tasks;
using Orleans;

namespace UnitTests.GrainInterfaces
{
    public enum ReturnCode
    {
        OK = 0,
        Fail = 1
    }

    [Serializable]
    [Hagar.GenerateSerializer]
    public struct MemberVariables
    {
        [Hagar.Id(0)]
        public byte[] byteArray;
        [Hagar.Id(1)]
        public string stringVar;
        [Hagar.Id(2)]
        public ReturnCode code;

        public MemberVariables(byte[] bytes, string str, ReturnCode codeInput)
        {
            byteArray = bytes;
            stringVar = str;
            code = codeInput;
        }
    }

    public interface IGeneratorTestGrain : IGrainWithIntegerKey
    {
        Task<byte[]> ByteSet(byte[] data);
        Task StringSet(string str);
        Task<bool> StringIsNullOrEmpty();
        Task<MemberVariables> GetMemberVariables();
        Task SetMemberVariables(MemberVariables x);

    }
}
