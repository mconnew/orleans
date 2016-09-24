---
layout: page
title: 简介
---

# 简介
<!-- # Introduction -->

<!-- Orleans is a framework that provides a straightforward approach to building distributed high-scale computing applications, without the need to learn and apply complex concurrency or other scaling patterns. It was created by Microsoft Research and designed for use in the cloud. Orleans has been used extensively in Microsoft Azure by several Microsoft product groups, most notably by 343 Industries as a platform for all of Halo Reach, Halo 4 and Halo 5 cloud services, as well as by a number of other companies. -->

Orleans提供了一个简单直接的构建分布式高可扩展计算应用，并且不需要学习和掌握复杂的并发或者其他的扩展模式。它由Microsoft Research创建和设计在云端使用。Oreleans已经广泛地被Microsoft数个产品组在Microsoft Azure中使用，最知名的是343 Industries用作Halo Reach、Halo 4 和Halo 5的云服务平台，除此之外在别的一些企业中也有应用。
 
<!-- Following Orleans' release as an open source framework on January 2015, it has quickly gained popularity and recognition. Leveraging an active developer community and the dedication of the Orleans team, features are added and improved on a daily basis. Microsoft Research continues to invest in Orleans, making it the framework of choice for .NET distributed development. -->

自从Orleans作为开源框架在2015年1月发布以来，很快地得到了欢迎和认可。受活跃的开发者社区和Orleans团队的影响，特性每天都在增和改善。Microsoft Research持续资助Orleans，使它成为.NET 分布式开发的首选。

### 背景
<!-- ### Background  -->

<!--Cloud applications and services are inherently parallel and distributed. They are also interactive and dynamic; often requiring near real time direct interactions between cloud entities. Such applications are very difficult to build today. The development process demands expert level programmers and typically requires expensive iterations of the design and the architecture, as the workload grows. -->
云引用和服务天生地是并行和分布式的。它们也是互相配合和动态的；经常需求近乎实时的云实例之间的交互。现在构建这样的应用十分困难。开发过程需要专家级的程序员并且往往在工作量增长的同时需要昂贵的重新设计和重新架构。

<!--Most of today’s high scale properties are built with the SOA paradigm. Rendering of a single web page by Amazon or Google or Facebook involves complex interactions of hundreds of SOA services that are independently built, deployed and managed. The fact that each individual service scales well by itself does not guarantee scalability of a composition of such services. -->
现今的大多数高扩展性应用是通过SOA模式构建的。渲染一个亚马逊、Google或者Facebook的web页面涉及到与数百个独立构建部署和管理的SOA服务的复杂交互。

<!-- The data scale-out mechanism of SOA is partitioning. As data size and load grow and “hot spots” come and go, a service has to dynamically repartition its state and do so without interrupting its operation. SOA challenges the programmer with a high degree of concurrency of requests within partitions. But existing tools do not provide good support for safe and efficient concurrency and distributed parallelism. -->
SOA的数据横向扩展机制是分区法。当数据大小和负载增长出现“热点”，一个服务必须动态地重分区它的状态，并且是在不打断他的操作的情况下完成。SOA给程序开发者提出了在分区内的高度并发请求的挑战。但是现有的工具并没有很好的提供安全高效的并发和分布式并行化支持。

<!--The stateless N-tier model delegates the partitioning problem to the storage layer. It often requires caching in the stateless layer to get acceptable performance, adding complexity and introducing cache consistency issues. -->
无状态的N层模型在存储层解决分区问题。它经常需要添加缓存层来得到可接受的性能，增加了复杂性并且引入了缓存一致性问题。

### Actors 
<!--### Actors-->

<!--The actor model supports fine-grain individual objects—actors—that are isolated from each other and light-weight enough to allow modeling of an individual entity as an actor. They communicate via asynchronous message passing, which enables direct communications between actors.-->
Actor模型支持细粒度的独立的对象——actor，actor各自独立并且足够轻量，支持将独立的entity建模成一个actor。他们之间可以通过异步消息传递直接进行通信。

<!--Significantly, an actor executes with single-threaded semantics. Coupled with encapsulation of the actor’s state and isolation from other actors, this simplifies writing highly concurrent systems by removing data races from the actor’s code level. Developers using actors do not have to worry about critical regions, mutexes, lock leveling, and other complex race-prevention concerns that have nothing to do with the actual application logic. Actors are dynamically created within the pool of available hardware resources. This makes balancing of load easier compared to hash-based partitioning of SOA.-->
很明显，一个actor使用一个单线程的语义执行。加上actor的状态封装和与其他actor的事务隔离，从actor代码层面解决了数据竞争问题，使得写一个高并发系统变得很容易。开发者使用actor不用担心临界区、互斥量、锁级别和其他的复杂的对应用逻辑没用的的防竞争问题。actor是动态地在可食用的硬件资源池中创建的。这使得负载均衡相对于SOA的基于hash的分区更加简单。


<!--For the last decade, Erlang has been the most popular implementation of the traditional actor model. Facing the above-mentioned challenges of SOA, the industry started rediscovering the actor model, which stimulated renewed interest in Erlang and creation of new Erlang-like solutions: Scala actors, Akka, DCell.-->
过去的十年，Erlang是最流行的传统actor模型的实现。面对上面提到的SOA的挑战，业界开始重新探索actor模型，重新激起了人们对Erlang和类Erlang解决方案（Scala、Akka、DCell）的兴趣。

### 虚拟Actors
<!--### Virtual Actors-->

<!--Orleans is an implementation of an improved actor model that borrows heavily from Erlang and distributed objects systems, adds static typing, message indirection and actor virtualization, exposing them in an integrated programming model. Whereas Erlang is a pure functional language with its own custom VM, the Orleans programming model directly leverages .NET and its object-oriented capabilities. It provides a framework that makes development of complex distributed applications much easier and make the resulting applications scalable by design.-->
Orleans是一个改进的actor模型的实现，大量参考了Erlang和分布式对象系统，添加了静态类型、消息中间层和actor虚拟化，将这些集成在一个编程模型中。尽管Erlang是一个有它自己的自定义虚拟机纯函数式语言,Orleans编程模型还是直接利用.NET和它的面向对象的能力。它提供了一个让开发复杂分布式应用更简单的平台，并且使最终的应用在设计上具有可扩展性。

<!--Unlike actors in other systems such as Erlang or Akka, [Orleans Grains](/orleans/Getting-Started-With-Orleans/Grains) are virtual actors. They communicate via asynchronous messaging, which differs greatly from synchronous method calls, but experience has shown that purely synchronous systems do not scale well; in this case we have traded familiarity for scalability. -->
不像其他系统例如Erlang或者Akka中的actor，[Orleans Grains](/orleans/Getting-Started-With-Orleans/Grains) 是虚拟actor。与同步方法调用不同的是他们通过异步消息通信。经验已经表明纯同步系统往往不具备很好的可扩展性，因此我们的系统更容易扩展。

<!--The Orleans runtime manages the location and activation of grains similarly to the way that the virtual memory manager of an operating system manages memory pages: it activates a grain by creating an in-memory copy (an activation) on a server (an [Orleans Silo](/orleans/Getting-Started-With-Orleans/Silos)), and later it may deactivate that activation if it hasn't been used for some time.-->
Orleans运行时管理grains的位置和激活的方法类似于操作系统管理虚拟内存页：它通过创建一个内存拷贝（一个激活）在一个服务器（一个 [Orleans Silo](/orleans/Getting-Started-With-Orleans/Silos)）上激活一个grain，并且过一会儿如果这个激活一段时间没有被使用它可能停用这个激活。

<!--If a message is sent to the grain and there is no activation on any server, then the runtime will pick a location and create a new activation there. Because grains are virtual, they never fail, even if the server that currently hosts all of their activations fails. -->
如果一条消息发送给一个grain，并且任何服务器上都没有激活，那么运行时将选一个位置并且在那创建一个新的激活。因为grain是虚拟的，所以他们从不会失败，甚至当前服务器所有的激活都失败了。
<!--This eliminates the need to test to see if a grain exists, as well as the need to track failures and recreate grains as needed; the Orleans runtime does all this automatically. -->
这就排除了验证grain是否存在的必要，也派出了追踪失败并且重新创建grain的必要；Orleans运行时自动地完成这些工作。

[Read the MSR Technical Report on Orleans](http://research.microsoft.com/pubs/210931/Orleans-MSR-TR-2014-41.pdf)
