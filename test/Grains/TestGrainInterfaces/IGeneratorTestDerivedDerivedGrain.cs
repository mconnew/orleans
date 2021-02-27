using System;
using System.Threading.Tasks;

namespace UnitTests.GrainInterfaces
{
    [Serializable]
    [Hagar.GenerateSerializer]
    public class ReplaceArguments
    {
        [Hagar.Id(0)]
        public string OldString { get; private set; }
        [Hagar.Id(1)]
        public string NewString { get; private set; }

        public ReplaceArguments(string oldStr, string newStr)
        {
            OldString = oldStr;
            NewString = newStr;
        }
    }

    public interface IGeneratorTestDerivedDerivedGrain : IGeneratorTestDerivedGrain2
    {
        Task<string> StringNConcat(string[] strArray);
        Task<string> StringReplace(ReplaceArguments strs);
    }
}