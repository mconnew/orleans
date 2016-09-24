---
layout: page
title: Main Benefits
---

# 优点
<!-- # Benefits -->

<!--
The main benefits of Orleans are: **developer productivity**, even for non-expert programmers; and **transparent scalability by default** with no special effort from the programmer. We expand on each of these benefits below.
-->
Orleans的主要优点有：甚至非专家级的程序员都能够达到很高的**生产力**；**对于程序员透明的自带的可扩展性**。下面展开来讲这些有点。

<!--
### Developer Productivity
-->

### 开发者的生产力

<!--
The Orleans programming model raises productivity of both expert and non-expert programmers by providing the following key abstractions, guarantees and system services.
-->
Orleans编程模型通过提供以下抽象、保证和系统服务来提高专家级和非专家级程序员的生产力。

<!--
* **Familiar object-oriented programming (OOP) paradigm**. Actors are .NET classes that implement declared .NET actor interfaces with asynchronous methods. Thus actors appear to the programmer as remote objects whose methods can be directly invoked. This provides the programmer the familiar OOP paradigm by turning method calls into messages, routing them to the right endpoints, invoking the target actor’s methods and dealing with failures and corner cases in a completely transparent way.
-->

* **类似面向对象编程(OOP)模型**。 Actor是实现了具有异步方法的.NET actor接口的.NET类。因此actor对于程序员来说就是其方法可以直接invoke的远程对象。通过一些列完全透明的过程将方法调用转换成消息，将他们路由到正确的终结点，调用目标actor的方法并且处理错误和极端状况，最终提供给程序员一个类似面向对象编程模型。 

<!--
* **Single-threaded execution of actors**. The runtime guarantees that an actor never executes on more than one thread at a time. Combined with the isolation from other actors, the programmer never faces concurrency at the actor level, and hence never needs to use locks or other synchronization mechanisms to control access to shared data. This feature alone makes development of distributed applications tractable for non-expert programmers.
-->

* **actor的单线程执行**. 运行时保证一个actor绝对不会同时在一个以上的纤程执行。结合与其他的actor的失误隔离，程序员永远不会面临actor层面的并发问题，并且因此永远不用使用锁或者其他的同步机制来控制对共享数据的访问。这个特性使得非专家级的程序员也能跟踪调试分布式应用。

<!--
* **Transparent activation**. The runtime activates an actor as-needed, only when there is a message for it to process. This cleanly separates the notion of creating a reference to an actor, which is visible to and controlled by application code, and physical activation of the actor in memory, which is transparent to the application. In many ways, this is similar to virtual memory in that it decides when to “page out” (deactivate) or “page in” (activate) an actor; the application has uninterrupted access to the full “memory space” of logically created actors, whether or not they are in the physical memory at any particular point in time. Transparent activation enables dynamic, adaptive load balancing via placement and migration of actors across the pool of hardware resources. This features is a significant improvement on the traditional actor model, in which actor lifetime is application-managed.
-->

* **激活透明化**。 运行时只有当有消息需要actor处理的时候才激活actor。这样很清楚的区分了创建一个actor的引用（可见的并且可以通过代码控制）和实际物理内存中的actor激活（对于应用完全透明）两个概念。 很多方面，这有些类似虚拟内存决定什么时候“换入”（激活）或者什么时“换出”（停用）一个actor；应用可以持续访问actor的全部的“内存空间”，不管是否在物理内存中。通过在硬件资源池中安排和迁移actor使得透明的激活可以动态地自适应地进行负载均衡。这个特性是对传统的actor模型（actor的生命周期由应用控制）的重大改进。

<!--
* **Location transparency**. An actor reference (proxy object) that the programmer uses to invoke the actor’s methods or pass to other components only contains the logical identity of the actor. The translation of the actor’s logical identity to its physical location and the corresponding routing of messages are done transparently by the Orleans runtime. Application code communicates with actors oblivious to their physical location, which may change over time due to failures or resource management, or because an actor is deactivated at the time it is called.
-->

* **位置透明化**。 程序员用来调用actor方法或者传递给其他组件的的一个actor引用（代理对象）只包含actor的逻辑标识。actor的逻辑表示到它的物理位置的翻译工作和响应的消息的路由是完全透明的，由Orleans运行时完成的。应用代码与其他的actor通信完全不用知道其他actor的物理位置，物理位置可能在这段时间因为错误或者资源管理而而改变，或者因为一个actor在它被调用的时候已经停用。

<!--
* **Transparent integration with persistent store**. Orleans allows for declarative mapping of actors’ in-memory state to persistent store. It synchronizes updates, transparently guaranteeing that callers receive results only after the persistent state has been successfully updated. Extending and/or customizing the set of existing persistent storage providers available is straight-forward.
-->

* **与持久存储集成的透明化**。 Orleans允许actor内存中的状态到持久存储的声明式的映射。他同步更新，透明地保证只有在持久状态已经成功后调用者才收到结果。 可以很容易地扩充并且/或者定制一些列现有的持久优化provider。

<!--
* **Automatic propagation of errors**. The runtime automatically propagates unhandled errors up the call chain with the semantics of asynchronous and distributed try/catch. As a result, errors do not get lost within an application. This allows the programmer to put error handling logic at the appropriate places, without the tedious work of manually propagating errors at each level.
-->

* **错误的自动传播**。 运行时根据异步语义和分布式try/catch自动沿着调用链传播未处理的错误。最终，错误不会在应用内丢失。这使得程序员可以将错误处理逻辑放在合适的地方，不必啰嗦地在每一层手动的传播错误。

<!--### Transparent Scalability by Default-->

### 默认的透明的可扩展性

<!--The Orleans programming model is designed to guide the programmer down a path of likely success in scaling their application or service through several orders of magnitude. This is done by incorporating the proven best practices and patterns, and providing an efficient implementation of the lower level system functionality. Here are some key factors that enable scalability and performance.-->
Orleans编程模型是设计用来指导程序员可能成功地成数量级的扩展他们的应用或者服务。这是结合了过往经验的最佳实践和模式，并且提供底层系统功能的有效实现。下面说一下几个保证可扩展性和性能的关键因素。

<!--* **Implicit fine grain partitioning of application state**. By using actors as directly addressable entities, the programmer implicitly breaks down the overall state of their application. While the Orleans programming model does not prescribe how big or small an actor should be, in most cases it makes sense to have a relative large number of actors – millions or more – with each representing a natural entity of the application, such as a user account, a purchase order, etc. With actors being individually addressable and their physical location abstracted away by the runtime, Orleans has enormous flexibility in balancing load and dealing with hot spots in a transparent and generic way without any thought from the application developer.-->
* **应用程序状态的隐式细粒度划分**。 通过将actor作为直接寻址的实体，程序员隐式地划分了他们应用程序的整体状态。虽然Orleans框架没有规定一个actor的大小，在大多数情况保持相对大量（百万或者个多）的actor是有意义的，每一个actor代表应用的一个自然实体，就像用户账户或者采购订单之类的。
<!--* **Adaptive resource management**. With actors making no assumption about locality of other actors they interact with and because of the location transparency, the runtime can manage and adjust allocation of available HW resources in a very dynamic way by making fine grain decisions on placement/migration of actors across the compute cluster in reaction to load and communication patterns without failing incoming requests. By creating multiple replicas of a particular actor the runtime can increase throughput of the actor if necessary without making any changes to the application code.-->
* **自适应资源管理**。 因为actor不对与他们交互的actor的位置做任何假设并且位置透明，运行时可以动态地管理和调节现在有可用硬件资源的分配，运行时可以在计算集群中做一些细粒度的放置/迁移，达到对负载进行响应和不会请求失败的通信模式。如果有必要，运行时可以通过创建特定actor的多副本来提高吞吐量，并且不需要修改任何代码。
<!--* **Multiplexed communication**. Actors in Orleans have logical endpoints, and messaging between them is multiplexed across a fixed set of all-to-all physical connections (TCP sockets). This allows the  runtime to host a very large number (millions) of addressable entities with low OS overhead per actor. In addition, activation/deactivation of an actor does not incur the cost of registering/unregistering of a physical endpoint, such as a TCP port or a HTTP URL, or even closing a TCP connection.-->
* **多路复用通信**。 actor在Orleans框架中具有逻辑终结点，并且他们之间的通信复用一组固定的全交换物理连接（TCP scokets）。这使得运行时仅仅使用很低的系统开销就可以承载大量（百万级）的可寻址实体。另外激活/停用一个acttor不会导致物理终结点的注册/注销开销（TCP端口或者HTTP URl，甚至关闭一个TCP连接）。
<!--* **Efficient scheduling**. The runtime schedules execution of a large number of single-threaded actors across a custom thread pool with a thread per physical processor core. With actor code written in the non-blocking continuation based style (a requirement of the Orleans programming model) application code runs in a very efficient “cooperative” multi-threaded manner with no contention. This allows the system to reach high throughput and run at very high CPU utilization (up to 90%+) with great stability. The fact that a growth in the number of actors in the system and the load does not lead to additional threads or other OS primitives helps scalability of individual nodes and the whole system.-->
* **有效的调度**。 运行时在一个线程池中调度大量的单线程actor，每个线程使用一个物理处理器核。因为actor的代码是非阻塞连续风格的（Orleans编程框架的要求之一），应用代码以一种高效的“合作”多线程形式无竞争地运行。这使得系统可以达到很高的吞吐量和有很高的CPU利用率（高达90%+）仍然十分稳定。事实表明系统中actor数量和负载的增长不会导致多余的线程或者系统原语，这点提高了各个节点和整个系统的可扩展性。
<!--* **Explicit asynchrony**. The Orleans programming model makes the asynchronous nature of a distributed application explicit and guides programmers to write non-blocking asynchronous code. Combined with asynchronous messaging and efficient scheduling, this enables a large degree of distributed parallelism and overall throughput without the explicit use of multi-threading.-->
* **显式异步**。 Orleans编程模型使得分布式应用的异步特性更加明显，并且指导程序员写异步的非阻塞的代码。结合异步消息和高效的调度，可以在没显式使用多线程的情况下很大程度地提高分布式并行性和整体吞吐量。
