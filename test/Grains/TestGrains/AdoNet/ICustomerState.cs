using System;
using System.Collections.Generic;
using Orleans.SqlUtils.StorageProvider.GrainInterfaces;

namespace Orleans.SqlUtils.StorageProvider.GrainClasses
{
    [Serializable]
    [Orleans.GenerateSerializer]
    public class CustomerState
    {
        [Orleans.Id(0)]
        public int CustomerId { get; set; }
        [Orleans.Id(1)]
        public string FirstName { get; set; }
        [Orleans.Id(2)]
        public string LastName { get; set; }
        [Orleans.Id(3)]
        public string NickName { get; set; }
        [Orleans.Id(4)]
        public DateTime BirthDate { get; set; }
        [Orleans.Id(5)]
        public int Gender { get; set; }
        [Orleans.Id(6)]
        public string Country { get; set; }
        [Orleans.Id(7)]
        public string AvatarUrl { get; set; }
        [Orleans.Id(8)]
        public int KudoPoints { get; set; }
        [Orleans.Id(9)]
        public int Status { get; set; }
        [Orleans.Id(10)]
        public DateTime LastLogin { get; set; }
        [Orleans.Id(11)]
        public List<IDeviceGrain> Devices { get; set; }
    }
}