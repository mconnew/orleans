using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Orleans;

namespace UnitTests.GrainInterfaces
{
    [Serializable]
    [Orleans.GenerateSerializer]
    public class EnumClass
    {
        [Orleans.Id(0)]
        public IEnumerable<DateTimeKind> EnumsList { get; set; }
    }

    public interface IExternalTypeGrain : IGrainWithIntegerKey
    {
        Task GetAbstractModel(IEnumerable<NameObjectCollectionBase> list);

        Task<EnumClass> GetEnumModel();
    }
}
