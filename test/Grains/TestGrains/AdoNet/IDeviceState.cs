using System;
using Orleans.SqlUtils.StorageProvider.GrainInterfaces;

namespace Orleans.SqlUtils.StorageProvider.GrainClasses
{
    [Serializable]
    [Hagar.GenerateSerializer]
    public class DeviceState
    {
        [Hagar.Id(0)]
        public ICustomerGrain Owner { get; set; }
        [Hagar.Id(1)]
        public string SerialNumber { get; set; }
        [Hagar.Id(2)]
        public long EventId { get; set; }
        [Hagar.Id(3)]
        public int VehicleId { get; set; }
        [Hagar.Id(4)]
        public short CustomerId { get; set; }
        [Hagar.Id(5)]
        public short CompanyId { get; set; }
        [Hagar.Id(6)]
        public short SoftwareId { get; set; }
        [Hagar.Id(7)]
        public short StatusId { get; set; }
        [Hagar.Id(8)]
        public short LifeCycleId { get; set; }
        [Hagar.Id(9)]
        public int DateKey { get; set; }
        [Hagar.Id(10)]
        public int TimeKey { get; set; }
        [Hagar.Id(11)]
        public short MillisecondKey { get; set; }
        [Hagar.Id(12)]
        public int FaultId { get; set; }
        [Hagar.Id(13)]
        public short SystemId { get; set; }
        [Hagar.Id(14)]
        public short EventTypeId { get; set; }
        [Hagar.Id(15)]
        public int LocationId { get; set; }
        [Hagar.Id(16)]
        public double Latitude { get; set; }
        [Hagar.Id(17)]
        public double Longitude { get; set; }
        [Hagar.Id(18)]
        public DateTime TriggerTime { get; set; }
        [Hagar.Id(19)]
        public long Altitude { get; set; }
        [Hagar.Id(20)]
        public long Heading { get; set; }
        [Hagar.Id(21)]
        public int PeakBusUtilization { get; set; }
        [Hagar.Id(22)]
        public int TripId { get; set; }
        [Hagar.Id(23)]
        public int CurrentBusUtilization { get; set; }
        [Hagar.Id(24)]
        public int TotalSnapshots { get; set; }
        [Hagar.Id(25)]
        public bool ProtectLampOn { get; set; }
        [Hagar.Id(26)]
        public bool AmberWarningLampOn { get; set; }
        [Hagar.Id(27)]
        public bool RedStopLampOn { get; set; }
        [Hagar.Id(28)]
        public bool MalfunctionIndicatorLampOn { get; set; }
        [Hagar.Id(29)]
        public bool FlashProtectLampOn { get; set; }
        [Hagar.Id(30)]
        public bool FlashAmberWarningLampOn { get; set; }
        [Hagar.Id(31)]
        public bool FlashRedStopLampOn { get; set; }
        [Hagar.Id(32)]
        public bool FlashMalfunctionIndicatorLampOn { get; set; }
        [Hagar.Id(33)]
        public int ConversionMethod { get; set; }
        [Hagar.Id(34)]
        public int OccurrenceCount { get; set; }
        [Hagar.Id(35)]
        public int PreTriggerSamples { get; set; }
        [Hagar.Id(36)]
        public int PostTriggerSamples { get; set; }
        [Hagar.Id(37)]
        public double AllLampsOnTime { get; set; }
        [Hagar.Id(38)]
        public int AmberLampCount { get; set; }
        [Hagar.Id(39)]
        public double AmberLampTime { get; set; }
        [Hagar.Id(40)]
        public int RedLampCount { get; set; }
        [Hagar.Id(41)]
        public double RedLampTime { get; set; }
        [Hagar.Id(42)]
        public int MilLampCount { get; set; }
        [Hagar.Id(43)]
        public double MilLampTime { get; set; }
        [Hagar.Id(44)]
        public double EngineStartAmbient { get; set; }
        [Hagar.Id(45)]
        public double EngineStartCoolant { get; set; }
        [Hagar.Id(46)]
        public double TotalDistance { get; set; }
        [Hagar.Id(47)]
        public double TotalEngineHours { get; set; }
        [Hagar.Id(48)]
        public double TotalIdleFuel { get; set; }
        [Hagar.Id(49)]
        public double TotalIdleHours { get; set; }
        [Hagar.Id(50)]
        public double TotalFuel { get; set; }
        [Hagar.Id(51)]
        public double TotalPtoFuel { get; set; }
        [Hagar.Id(52)]
        public Guid TransactionId { get; set; }
        [Hagar.Id(53)]
        public string MessageId { get; set; }
        [Hagar.Id(54)]
        public short LampId { get; set; }
        [Hagar.Id(55)]
        public short EngineFamilyId { get; set; }
    }
}