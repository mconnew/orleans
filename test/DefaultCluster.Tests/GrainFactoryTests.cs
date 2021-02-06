using System;
using Orleans;
using Orleans.Internal;
using Orleans.Runtime;
using TestExtensions;
using UnitTests.GrainInterfaces;
using UnitTests.Grains;
using Xunit;

namespace DefaultCluster.Tests
{
    public class GrainFactoryTests : HostedTestClusterEnsureDefaultStarted
    {
        public GrainFactoryTests(DefaultClusterFixture fixture) : base(fixture)
        {
        }

        [Fact, TestCategory("BVT"), TestCategory("Factory"), TestCategory("GetGrain")]
        public void GetGrain_Ambiguous()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                var g = this.GrainFactory.GetGrain<IBase>(SafeRandom.Next());
            });
        }

        [Fact, TestCategory("BVT"), TestCategory("Factory"), TestCategory("GetGrain")]
        public void GetGrain_Ambiguous_WithDefault()
        {
            var g = this.GrainFactory.GetGrain<IBase4>(SafeRandom.Next());
            Assert.False(g.Foo().Result);
        }

        [Fact, TestCategory("BVT"), TestCategory("Factory"), TestCategory("GetGrain")]
        public void GetGrain_WithFullName()
        {
            var grainFullName = typeof(BaseGrain).FullName;
            var g = this.GrainFactory.GetGrain<IBase>(SafeRandom.Next(), grainFullName);
            Assert.True(g.Foo().Result);
        }

        [Fact, TestCategory("BVT"), TestCategory("Factory"), TestCategory("GetGrain")]
        public void GetGrain_WithPrefix()
        {
            var g = this.GrainFactory.GetGrain<IBase>(SafeRandom.Next(), BaseGrain.GrainPrefix);
            Assert.True(g.Foo().Result);
        }

        [Fact, TestCategory("BVT"), TestCategory("Factory"), TestCategory("GetGrain")]
        public void GetGrain_AmbiguousPrefix()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                var g = this.GrainFactory.GetGrain<IBase>(SafeRandom.Next(), "UnitTests.Grains");
            });
        }

        [Fact, TestCategory("BVT"), TestCategory("Factory"), TestCategory("GetGrain")]
        public void GetGrain_WrongPrefix()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                var g = this.GrainFactory.GetGrain<IBase>(SafeRandom.Next(), "Foo");
            });
        }

        [Fact, TestCategory("BVT"), TestCategory("Factory"), TestCategory("GetGrain")]
        public void GetGrain_Derived_NoPrefix()
        {
            var g = this.GrainFactory.GetGrain<IDerivedFromBase>(SafeRandom.Next());
            Assert.False(g.Foo().Result);
            Assert.True(g.Bar().Result);
        }

        [Fact, TestCategory("BVT"), TestCategory("Factory"), TestCategory("GetGrain")]
        public void GetGrain_Derived_WithFullName()
        {
            var grainFullName = typeof(DerivedFromBaseGrain).FullName;
            var g = this.GrainFactory.GetGrain<IDerivedFromBase>(SafeRandom.Next(), grainFullName);
            Assert.False(g.Foo().Result);
            Assert.True(g.Bar().Result);
        }

        [Fact, TestCategory("BVT"), TestCategory("Factory"), TestCategory("GetGrain")]
        public void GetGrain_Derived_WithFullName_FromBase()
        {
            var grainFullName = typeof(DerivedFromBaseGrain).FullName;
            var g = this.GrainFactory.GetGrain<IBase>(SafeRandom.Next(), grainFullName);
            Assert.False(g.Foo().Result);
        }

        [Fact, TestCategory("BVT"), TestCategory("Factory"), TestCategory("GetGrain")]
        public void GetGrain_Derived_WithPrefix()
        {
            var g = this.GrainFactory.GetGrain<IDerivedFromBase>(SafeRandom.Next(), "UnitTests.Grains");
            Assert.False(g.Foo().Result);
            Assert.True(g.Bar().Result);
        }

        [Fact, TestCategory("BVT"), TestCategory("Factory"), TestCategory("GetGrain")]
        public void GetGrain_Derived_WithWrongPrefix()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                var g = this.GrainFactory.GetGrain<IDerivedFromBase>(SafeRandom.Next(), "Foo");
            });
        }

        [Fact, TestCategory("BVT"), TestCategory("Factory"), TestCategory("GetGrain")]
        public void GetGrain_OneImplementation_NoPrefix()
        {
            var g = this.GrainFactory.GetGrain<IBase1>(SafeRandom.Next());
            Assert.False(g.Foo().Result);
        }

        [Fact, TestCategory("BVT"), TestCategory("Factory"), TestCategory("GetGrain")]
        public void GetGrain_OneImplementation_Prefix()
        {
            var grainFullName = typeof(BaseGrain1).FullName;
            var g = this.GrainFactory.GetGrain<IBase1>(SafeRandom.Next(), grainFullName);
            Assert.False(g.Foo().Result);
        }

        [Fact, TestCategory("BVT"), TestCategory("Factory"), TestCategory("GetGrain")]
        public void GetGrain_MultipleUnrelatedInterfaces()
        {
            var g1 = this.GrainFactory.GetGrain<IBase3>(SafeRandom.Next());
            Assert.False(g1.Foo().Result);
            var g2 = this.GrainFactory.GetGrain<IBase2>(SafeRandom.Next());
            Assert.True(g2.Bar().Result);
        }

        [Fact, TestCategory("BVT"), TestCategory("Factory"), TestCategory("GetGrain")]
        public void GetStringGrain()
        {
            var g = this.GrainFactory.GetGrain<IStringGrain>(Guid.NewGuid().ToString());
            Assert.True(g.Foo().Result);
        }

        [Fact, TestCategory("BVT"), TestCategory("Factory"), TestCategory("GetGrain")]
        public void GetGuidGrain()
        {
            var g = this.GrainFactory.GetGrain<IGuidGrain>(Guid.NewGuid());
            Assert.True(g.Foo().Result);
        }
    }
}
