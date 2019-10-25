# Introducing Orleans 3.0

Today we are excited to announce the Orleans 3.0 release. A great number of improvements and fixes went in, as well as several new features, since Orleans 2.0. These changes were driven by the experience of many people running Orleans-based applications in production in a wide range of scenarios and environments, and by the ingenuity and passion of the global Orleans community that always strives to make the codebase better, faster, and more flexible. A BIG Thank You to all who contributed to this release in various ways!

## Major changes since Orleans 2.0

Orleans 2.0 was released a little over 18 months ago and since then Orleans has made significant strides. Some of the headline changes since 2.0 are:

* Distributed ACID transactions — multiple grains can join a transaction regardless of where their state is stored
* A new scheduler, which alone increased performance by over 30% in some cases
* A new code generator based on Roslyn code analysis
* Rewritten cluster membership for improved recovery speed
* Co-hosting support

As well as many, many other improvements and fixes.

Aligning Orleans with the rest of .NET is one of our ongoing efforts. Ultimately, we aim to contribute to a strong .NET ecosystem. A part of this involves removing our own implementations of various things and collaborating on, and investing in, shared libraries instead. The re Examples include logging, options, dependency injection, hosting, and now — with Orleans 3.0 — networking.

## Networking layer replacement with ASP.NET Bedrock

Support for securing communication with [TLS](https://en.wikipedia.org/wiki/Transport_Layer_Security) has been a major ask for some time, both from [the community](https://github.com/dotnet/orleans/issues/828) as well as from internal partners. With the 3.0 release we are introducing TLS support, available via the [Microsoft.Orleans.Connections.Security](https://www.nuget.org/packages/Microsoft.Orleans.Connections.Security) package. For more information, see the [TransportLayerSecurity sample](https://github.com/dotnet/orleans/tree/master/Samples/3.0/TransportLayerSecurity).

Implementing TLS support was a major undertaking due to how the networking layer in previous versions of Orleans was implemented: it could not be easily adapted to use `SslStream`, which is the most common method for implementing TLS. With TLS as our driving force, we embarked upon a journey to rewrite Orleans' networking layer.

Orleans 3.0 replaces its entire networking layer with one built on top of Project Bedrock, an initiative from the ASP.NET team. The goal of Bedrock is to help developers to build fast and robust network clients and servers.

The ASP.NET team and the Orleans team worked together to design abstractions which support both network clients and servers, are transport-agnostic, and can be customized using middleware. These abstractions allow us to change the network transport via configuration, without modifying internal, Orleans-specific networking code. Orleans' TLS support is implemented as a Bedrock middleware and our intention is for this to be made generic so that it can be shared with others in the .NET ecosystem.

Although the impetus for this undertaking was to enable TLS support, we see an approximately **30% improvement in throughput** on average in our nightly load tests.

The networking layer rewrite also involved replacing our custom buffer pooling with reliance on `MemoryPool<byte>` and in making this change, serialization now takes more advantage of `Span<T>`. Some code paths which previously relied on blocking via dedicated threads calling `BlockingCollection<T>` are now using `Channel<T>` to pass messages asynchronously. This results in less dedicated threads, moving the work to the .NET thread pool instead.

The core wire protocol for Orleans has remained stable since its initial release. With Orleans 3.0 we have added support for progressively upgrading the network protocol via protocol negotiation. The protocol negotiation support added in Orleans 3.0 enables future enhancements, such as customizing the core serializer, while maintaining backwards compatibility. One benefit of the new networking protocol is support for full-duplex silo-to-silo connections rather than the half-duplex connection pairs established between silos today. The protocol version can be configured via `ConnectionOptions.ProtocolVersion`.

## Co-hosting via the Generic Host

Co-hosting Orleans with other frameworks, such as ASP.NET Core, in the same process is now easier than before thanks to the [.NET Generic Host](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host?view=aspnetcore-3.0).

Here is an example of adding Orleans alongside ASP.NET Core to a host using `UseOrleans`:

``` csharp
var host = new HostBuilder()
  .ConfigureWebHostDefaults(webBuilder =>
  {
    // Configure ASP.NET Core
    webBuilder.UseStartup<Startup>();
  })
  .UseOrleans(siloBuilder =>
  {
    // Configure Orleans
    siloBuilder.UseLocalHostClustering();
  })
  .ConfigureLogging(logging =>
  {
    /* Configure cross-cutting concerns such as logging */
  })
  .ConfigureServices(services =>
  {
    /* Configure shared services */
  })
  .UseConsoleLifetime()
  .Build();

// Start the host and wait for it to shutdown.
await host.RunAsync();
```

Using the generic host builder, Orleans will share a service provider with other hosted services. This grants these services access to Orleans. For example, a developer can inject `IClusterClient` or `IGrainFactory` into an ASP.NET Core MVC controller and call grains directly from their MVC application.

## Reliability improvements

Clusters now recover more quickly from failures thanks to extended gossiping. In previous versions of Orleans, silos would send membership gossip messages to other silos, instructing them to update membership information. Gossip messages now include versioned, immutable snapshots of cluster membership. This improves convergence time after a silo joins or leaves the cluster (for example during upgrade, scaling, or after a failure) and alleviates contention on the shared membership store, allowing for quicker cluster transitions. Failure detection has also been improved, with more diagnostic messages and refinements to ensure faster, more accurate detection. Failure detection involves silos in a cluster collaboratively monitoring each other, with each silo sending periodic health probes to a subset of other silos. Silos and clients also now proactively disconnect from silos which have been declared defunct and they will deny connections to such silos.

Messaging errors are now handled more consistently, resulting in prompt errors being propagated back to the caller. This helps developers to discover errors more quickly. For example, when a message cannot be fully serialized or deserialized, a detailed exception will be propagated back to the original caller.

## Improved extensibility for streams

Streams can now have custom data adaptors, allowing them to ingest data in any format. This gives developers greater control over how stream items are represented in storage. It also gives the stream provider control over how data is written, allowing steams to integrate with legacy systems and/or non-Orleans services.

## Join the effort

Now that Orleans 3.0 is out the door we are turning our attention to future releases — and we have some exciting plans! Come and join our warm, welcoming Orleans community on [GitHub](https://github.com/dotnet/orleans) and [Gitter](https://gitter.im/dotnet/orleans) and help us as we continue to raise the bar for distributed application runtimes.
