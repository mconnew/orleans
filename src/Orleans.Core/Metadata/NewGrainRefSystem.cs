using System;
using System.Collections.Generic;
using System.Text;

namespace Orleans.Metadata
{
    namespace NewGrainRefSystem
    {
        using System;
        using System.Collections.Immutable;
        using System.Linq;
        using System.Runtime.CompilerServices;
        using System.Threading;
        using System.Threading.Tasks;
        using Orleans.CodeGeneration;
        using Orleans.Runtime;

        namespace NewNewNew
        {
            public interface IGrainReference
            {
                GrainId GrainId { get; }

                Type InterfaceType { get; }

                // Eg, to set TTL, InvokeMethodOptions.Unordered, InvokeMethodOptions.OneWay?, SystemTargetSilo?, Category?
                // How does ResponseTimeout get propagated?
                void PrepareMessage(object /*IMessage*/ message);
            }

            // Per 'GrainId'
            // Imr = InvokeMethodRequest. Named this way because it uses messages with Body = InvokeMethodRequest instance to send messages.
            // An alternative can use a different mechanism
            public abstract class ImrGrainReferenceBase : IGrainReference
            {
                private readonly ImrGrainReferencePrototype prototype;
                private readonly SpanId key;

                public ImrGrainReferenceBase(ImrGrainReferencePrototype prototype, SpanId key)
                {
                    this.prototype = prototype;
                    this.key = key;
                }

                public GrainId GrainId => new GrainId(this.prototype.GrainType, this.key);

                public abstract Type InterfaceType { get; }
            }

            public class ImrGrainReferencePrototype
            {
                public ImrGrainReferencePrototype(GrainType grainType, Type interfaceType, IGrainReferenceRuntime grainRuntime)
                {
                    this.GrainType = grainType;
                    this.InterfaceType = interfaceType;
                    this.Runtime = grainRuntime;
                }

                // Everything shared by a grain for given type
                public GrainType GrainType { get; }

                public Type InterfaceType { get; }

                public IGrainReferenceRuntime Runtime { get; }

                public InvokeMethodOptions InvokeMethodOptions { get; set; }
            }

            public interface IGrainReferenceActivator
            {
                bool CanCreate(GrainType grainType, Type interfaceType);
                public IGrainReference CreateGrainReference(GrainId grainId, Type interfaceType);
            }

            /// <summary>
            /// The central point 
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

                public IGrainReference CreateGrainReference<TInterface>(GrainId grainId)
                {
                    var key = (grainId.Type, typeof(TInterface));
                    if (this.factories.TryGetValue(key, out var factory))
                    {
                        return factory.CreateGrainReference(grainId, typeof(TInterface));
                    }

                    return CreateSlow(grainId);

                    IGrainReference CreateSlow(GrainId grainId)
                    {
                        var key = (grainId.Type, typeof(TInterface));
                        lock (this.lockObj)
                        {
                            var grainType = grainId.Type;
                            if (!this.factories.TryGetValue(key, out var factory))
                            {
                                foreach (var item in this.grainContextFactories)
                                {
                                    if (item.CanCreate(grainType, typeof(TInterface)))
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

                            return factory.CreateGrainReference(grainId, typeof(TInterface));
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
                private ImmutableDictionary<GrainType, ContextActivator> factories = ImmutableDictionary<GrainType, ContextActivator>.Empty;

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
                    if (this.factories.TryGetValue(grainId.Type, out var factory))
                    {
                        return factory.CreateContext(grainId, interfaceType);
                    }

                    return CreateSlow(grainId);

                    IGrainReference CreateSlow(GrainId grainId)
                    {
                        lock (this.lockObj)
                        {
                            var grainType = grainId.Type;
                            if (!this.factories.TryGetValue(grainType, out var factory))
                            {
                                var prototype = this.CreatePrototype(grainType, interfaceType);
                                factory = new ContextActivator(prototype, this.configureActivationActions);
                                this.factories = this.factories.SetItem(grainType, factory);
                            }

                            return factory.CreateContext(grainId, interfaceType);
                        }
                    }
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

                private struct ContextActivator
                {
                    private readonly ImrGrainReferencePrototype prototype;
                    private readonly IConfigureGrainReference[] configureActions;

                    public ContextActivator(ImrGrainReferencePrototype prototype, IConfigureGrainReference[] configureActions)
                    {
                        this.prototype = prototype;
                        this.configureActions = configureActions.Where(c => c.CanConfigure(prototype.GrainType, prototype.InterfaceType)).ToArray();
                    }

                    public ImrGrainReferenceBase CreateContext(GrainId grainId, Type interfaceType)
                    {
                        var result = new ImrGrainReferenceBase(this.prototype, grainId.Key);
                        foreach (var configure in this.configureActions)
                        {
                            configure.Configure(result, interfaceType);
                        }

                        return result;
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
