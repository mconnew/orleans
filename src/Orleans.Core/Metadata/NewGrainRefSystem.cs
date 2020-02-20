using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Orleans.Runtime;

namespace Orleans.Metadata
{
    namespace NewGrainRefSystem
    {
        namespace NewNewNew
        {
            public interface IGrainReferenceActivator
            {
                bool CanCreate(GrainType grainType, Type interfaceType);
                public IGrainReference CreateGrainReference(GrainId grainId, Type interfaceType);
            }

            /// <summary>
            /// The central point for creating grain references.
            /// </summary>
            public sealed class GrainReferenceActivator
            {
                private readonly object lockObj = new object();
                private readonly IGrainReferenceActivator[] grainContextFactories;
                private readonly DefaultGrainReferenceActivator defaultActivator;
                private ImmutableDictionary<(GrainType, Type), IGrainReferenceActivator> factories = ImmutableDictionary<(GrainType, Type), IGrainReferenceActivator>.Empty;

                public GrainReferenceActivator(DefaultGrainReferenceActivator defaultActivator, IEnumerable<IGrainReferenceActivator> grainReferenceActivators)
                {
                    this.grainContextFactories = grainReferenceActivators.ToArray();
                    this.defaultActivator = defaultActivator;
                }

                public IGrainReference CreateGrainReference(GrainId grainId, Type interfaceType)
                {
                    if (this.factories.TryGetValue((grainId.Type, interfaceType), out var factory))
                    {
                        return factory.CreateGrainReference(grainId, interfaceType);
                    }

                    return CreateSlow(grainId, interfaceType);

                    IGrainReference CreateSlow(GrainId grainId, Type interfaceType)
                    {
                        var key = (grainId.Type, interfaceType);
                        lock (this.lockObj)
                        {
                            var grainType = grainId.Type;
                            if (!this.factories.TryGetValue(key, out var factory))
                            {
                                foreach (var item in this.grainContextFactories)
                                {
                                    if (item.CanCreate(grainType, interfaceType))
                                    {
                                        factory = item;
                                        this.factories = this.factories.SetItem(key, factory);
                                        break;
                                    }
                                }
                            }

                            // Fallback to the default.
                            if (factory is null)
                            {
                                factory = this.defaultActivator;
                                this.factories = this.factories.SetItem(key, factory);
                            }

                            return factory.CreateGrainReference(grainId, interfaceType);
                        }
                    }
                }
            }

            public class DefaultGrainReferenceActivator : IGrainReferenceActivator
            {
                private readonly object lockObj = new object();
                private readonly IConfigureGrainReferencePrototype[] configurePrototypeActions;
                private readonly IConfigureGrainReference[] configureActivationActions;
                private readonly IGrainReferenceRuntime grainRuntime;
                private ImmutableDictionary<GrainType, ReferenceFactory> factories = ImmutableDictionary<GrainType, ReferenceFactory>.Empty;

                public DefaultGrainReferenceActivator(
                    IEnumerable<IConfigureGrainReference> configureActivationActions,
                    IEnumerable<IConfigureGrainReferencePrototype> configurePrototypeActions,
                    IGrainReferenceRuntime grainRuntime)
                {
                    this.configurePrototypeActions = configurePrototypeActions.ToArray();
                    this.configureActivationActions = configureActivationActions.ToArray();
                    this.grainRuntime = grainRuntime;
                }

                public bool CanCreate(GrainType grainType, Type interfaceType) => true;

                public IGrainReference CreateGrainReference(GrainId grainId, Type interfaceType)
                {
                    var factory = this.GetFactory(grainId, interfaceType);
                    return factory.CreateContext(grainId);
                }

                private ReferenceFactory GetFactory(GrainId grainId, Type interfaceType)
                {
                    if (!this.factories.TryGetValue(grainId.Type, out var factory))
                    {
                        factory = this.CreateFactory(grainId.Type, interfaceType);
                    }

                    return factory;
                }

                private ReferenceFactory CreateFactory(GrainType grainType, Type interfaceType)
                {
                    lock (this.lockObj)
                    {
                        if (!this.factories.TryGetValue(grainType, out var factory))
                        {
                            var prototype = this.CreatePrototype(grainType, interfaceType);
                            factory = new ReferenceFactory(prototype, this.configureActivationActions);
                            this.factories = this.factories.SetItem(grainType, factory);
                        }

                        return factory;
                    }
                }

                public void BindGrainReference(IGrainReference reference, GrainId grainId, Type interfaceType)
                {
                    var factory = this.GetFactory(grainId, interfaceType);
                    factory.BindContext(reference);
                }

                private ImrGrainReferencePrototype CreatePrototype(GrainType grainType, Type interfaceType)
                {
                    var prototype = new ImrGrainReferencePrototype(grainType, interfaceType, this.grainRuntime);

                    // Configure the prototype
                    foreach (var configure in this.configurePrototypeActions)
                    {
                        if (!configure.CanConfigure(grainType, interfaceType)) continue;

                        configure.Configure(prototype, interfaceType);
                    }

                    return prototype;
                }

                private readonly struct ReferenceFactory
                {
                    private readonly ImrGrainReferencePrototype prototype;
                    private readonly IConfigureGrainReference[] configureActions;

                    public ReferenceFactory(ImrGrainReferencePrototype prototype, IConfigureGrainReference[] configureActions)
                    {
                        this.prototype = prototype;
                        this.configureActions = configureActions.Where(c => c.CanConfigure(prototype.GrainType, prototype.InterfaceType)).ToArray();
                    }

                    public readonly IGrainReference CreateContext(GrainId grainId)
                    {
                        var result = new ImrGrainReferenceBase(this.prototype, grainId.Key);
                        foreach (var configure in this.configureActions)
                        {
                            configure.Configure(result);
                        }

                        return result;
                    }

                    public readonly void BindContext(IGrainReference reference)
                    {
                        if (reference is GrainReference grainReference)
                        {
                            grainReference.Bind(this.prototype);
                        }

                        foreach (var configure in this.configureActions)
                        {
                            configure.Configure(reference, this.prototype.InterfaceType);
                        }
                    }
                }
            }

            public interface IConfigureGrainReferencePrototype
            {
                bool CanConfigure(GrainType grainType, Type interfaceType);

                // TODO: extract an interface for the prototype type, or use a builder pattern or something similar.
                // need to work out what's important there...
                void Configure(ImrGrainReferencePrototype prototype, Type interfaceType);
            }

            public interface IConfigureGrainReference
            {
                bool CanConfigure(GrainType grainType, Type interfaceType);
                void Configure(IGrainReference context, Type interfaceType);
            }
        }
    }
}
