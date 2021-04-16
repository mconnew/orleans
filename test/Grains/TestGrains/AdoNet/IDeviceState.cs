using System;
using Orleans.SqlUtils.StorageProvider.GrainInterfaces;

namespace Orleans.SqlUtils.StorageProvider.GrainClasses
{
    [Serializable]
    [Orleans.GenerateSerializer]
    public class DeviceState
    {
        [Orleans.Id(0)]
        public ICustomerGrain Owner { get; set; }
        [Orleans.Id(1)]
        public string SerialNumber { get; set; }
        [Orleans.Id(2)]
        public long EventId { get; set; }
        [Orleans.Id(3)]
        public int VehicleId { get; set; }
        [Orleans.Id(4)]
        public short CustomerId { get; set; }
        [Orleans.Id(5)]
        public short CompanyId { get; set; }
        [Orleans.Id(6)]
        public short SoftwareId { get; set; }
        [Orleans.Id(7)]
        public short StatusId { get; set; }
        [Orleans.Id(8)]
        public short LifeCycleId { get; set; }
        [Orleans.Id(9)]
        public int DateKey { get; set; }
        [Orleans.Id(10)]
        public int TimeKey { get; set; }
        [Orleans.Id(11)]
        public short MillisecondKey { get; set; }
        [Orleans.Id(12)]
        public int FaultId { get; set; }
        [Orleans.Id(13)]
        public short SystemId { get; set; }
        [Orleans.Id(14)]
        public short EventTypeId { get; set; }
        [Orleans.Id(15)]
        public int LocationId { get; set; }
        [Orleans.Id(16)]
        public double Latitude { get; set; }
        [Orleans.Id(17)]
        public double Longitude { get; set; }
        [Orleans.Id(18)]
        public DateTime TriggerTime { get; set; }
        [Orleans.Id(19)]
        public long Altitude { get; set; }
        [Orleans.Id(20)]
        public long Heading { get; set; }
        [Orleans.Id(21)]
        public int PeakBusUtilization { get; set; }
        [Orleans.Id(22)]
        public int TripId { get; set; }
        [Orleans.Id(23)]
        public int CurrentBusUtilization { get; set; }
        [Orleans.Id(24)]
        public int TotalSnapshots { get; set; }
        [Orleans.Id(25)]
        public bool ProtectLampOn { get; set; }
        [Orleans.Id(26)]
        public bool AmberWarningLampOn { get; set; }
        [Orleans.Id(27)]
        public bool RedStopLampOn { get; set; }
        [Orleans.Id(28)]
        public bool MalfunctionIndicatorLampOn { get; set; }
        [Orleans.Id(29)]
        public bool FlashProtectLampOn { get; set; }
        [Orleans.Id(30)]
        public bool FlashAmberWarningLampOn { get; set; }
        [Orleans.Id(31)]
        public bool FlashRedStopLampOn { get; set; }
        [Orleans.Id(32)]
        public bool FlashMalfunctionIndicatorLampOn { get; set; }
        [Orleans.Id(33)]
        public int ConversionMethod { get; set; }
        [Orleans.Id(34)]
        public int OccurrenceCount { get; set; }
        [Orleans.Id(35)]
        public int PreTriggerSamples { get; set; }
        [Orleans.Id(36)]
        public int PostTriggerSamples { get; set; }
        [Orleans.Id(37)]
        public double AllLampsOnTime { get; set; }
        [Orleans.Id(38)]
        public int AmberLampCount { get; set; }
        [Orleans.Id(39)]
        public double AmberLampTime { get; set; }
        [Orleans.Id(40)]
        public int RedLampCount { get; set; }
        [Orleans.Id(41)]
        public double RedLampTime { get; set; }
        [Orleans.Id(42)]
        public int MilLampCount { get; set; }
        [Orleans.Id(43)]
        public double MilLampTime { get; set; }
        [Orleans.Id(44)]
        public double EngineStartAmbient { get; set; }
        [Orleans.Id(45)]
        public double EngineStartCoolant { get; set; }
        [Orleans.Id(46)]
        public double TotalDistance { get; set; }
        [Orleans.Id(47)]
        public double TotalEngineHours { get; set; }
        [Orleans.Id(48)]
        public double TotalIdleFuel { get; set; }
        [Orleans.Id(49)]
        public double TotalIdleHours { get; set; }
        [Orleans.Id(50)]
        public double TotalFuel { get; set; }
        [Orleans.Id(51)]
        public double TotalPtoFuel { get; set; }
        [Orleans.Id(52)]
        public Guid TransactionId { get; set; }
        [Orleans.Id(53)]
        public string MessageId { get; set; }
        [Orleans.Id(54)]
        public short LampId { get; set; }
        [Orleans.Id(55)]
        public short EngineFamilyId { get; set; }
    }
}