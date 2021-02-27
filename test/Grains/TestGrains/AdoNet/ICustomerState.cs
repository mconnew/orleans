using System;
using System.Collections.Generic;
using Orleans.SqlUtils.StorageProvider.GrainInterfaces;

namespace Orleans.SqlUtils.StorageProvider.GrainClasses
{
    [Serializable]
    [Hagar.GenerateSerializer]
    public class CustomerState
    {
        [Hagar.Id(0)]
        public int CustomerId { get; set; }
        [Hagar.Id(1)]
        public string FirstName { get; set; }
        [Hagar.Id(2)]
        public string LastName { get; set; }
        [Hagar.Id(3)]
        public string NickName { get; set; }
        [Hagar.Id(4)]
        public DateTime BirthDate { get; set; }
        [Hagar.Id(5)]
        public int Gender { get; set; }
        [Hagar.Id(6)]
        public string Country { get; set; }
        [Hagar.Id(7)]
        public string AvatarUrl { get; set; }
        [Hagar.Id(8)]
        public int KudoPoints { get; set; }
        [Hagar.Id(9)]
        public int Status { get; set; }
        [Hagar.Id(10)]
        public DateTime LastLogin { get; set; }
        [Hagar.Id(11)]
        public List<IDeviceGrain> Devices { get; set; }
    }
}