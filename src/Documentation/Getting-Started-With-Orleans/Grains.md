---
layout: page
title: Grains
---


## Grains (Actors)： 分布式的基本单元
<!--## Grains (Actors): Units of Distribution-->

<!--Distributed applications are inherently concurrent, which leads to complexity. One of the things that makes the actor model special and productive is that it helps reduce some of the complexities of having to grapple with concurrency.-->
分布式应用天生是并发的，这导致了它的复杂性。Actor模型减少了处理并发的复杂性，是使得Actor模型高效和具有生产品的原因之一。

<!--Actors accomplish this in two ways:-->
Actor使用两种方法来解决复杂性问题：

<!--* By providing single-threaded access to the internal state of an actor instance.-->
<!--* By not sharing data between actor instances except via message-passing.-->
* 通过单线程来访问一个actor的内部状态
* 除了通过消息传递以外actor间不共享数据

<!--Grains are the building blocks of an Orleans application, they are atomic units of isolation, distribution, and persistence. -->
<!--A typical grain encapsulates state and behavior of a single entity (e.g. a specific user).-->
Grain是Orleans应用的基石，是隔离、分布和持久化的最小单元。
一个典型的grain封装了状态和单个实体的行为（例如，一个具体的用户）。

### Turns： 执行的基本单元
<!--### Turns: Units of Execution-->

<!--The idea behind the single-threaded execution model for actors is that the invokers (remote) take turns "calling" its methods. Thus, a message coming to actor B from actor A will placed in a queue and the associated handler is invoked only when all prior messages have been serviced.-->
Actor单线程调用的背后思想是，调用器（远程）轮流“调用”它的方法。因此，一个从actor A到actor B的消息将会放到一个队列里，并且只有当前面的消息处理完后相关的处理器才会被调用。

<!--This allows us to avoid all use of locks to protect actor state, as it is inherently protected against data races. However, it may also lead to problems when messages pass back and forth and the message graph forms cycles. If A sends a message to B from one of its methods and awaits its completion, and B sends a message to A, also awaiting its completion, the application will quickly lock up. -->
这使得我们可以不是用任何锁来保护actor的状态，并且它天生避免了数据竞争问题。然而，这可能导致当消息来回传递的时候形成消息循环。如果A发送了一条消息给B等待B处理完成，并且B发送了一条消息给A，也等待A处理完成，这个应用会很快锁起来。

### 一个Grain的激活 - Grain的运行时实例
<!--### A Grain Activation - The runtime instance of a Grain-->

<!--When there is work for a grain, Orleans ensures there is an instance of the grain on one of [Orleans Silos](Silos). When there is no instance of the grain on any silo, the run-time creates one. This process is called Activation. In case a grain is using [Grain Persistence](Grain-Persistence), the run-time automatically reads the state from the backing-store upon activation. -->
<!--Orleans controls the process of activating and deactivating grains transparently. When coding a grain, a developer assumes all grains are always activated.-->
当有工作需要grain处理的时候，Orleans确保一个[Orleans Silos](Silos.md)上有一个grain的实例。当silo上没有grain实例时，运行时会创建一个。这个过程称作激活。当一个grain使用[Grain持久化](Grain-Persistence.md)，运行时在激活时从持久化存储中自动读取状态。
Orleans透明地控制激活和注销的过程。当编写一个grain的时候，开发者只要假设所有的grain永远是被激活的。

<!--A grain activation performs work in chunks and finishes each chunk before it moves on to the next. Chunks of work include method invocations in response to requests from other grains or external clients, and closures scheduled on completion of a previous chunk. The basic unit of execution corresponding to a chunk of work is known as a turn.-->
grain的激活工作是分块执行的，一块执行完成移动到下一块。每块工作包含响应其他actor或者外部客户端的请求的方法调用，并且下一个闭包在前一个分块完成后被调度。执行的基础相当于一块工作也被叫做一个turn。

<!--While Orleans may execute many turns belonging to different activations in parallel, each activation will always execute its turns one at a time. This means that there is no need to use locks or other synchronization methods to guard against data races and other multi-threading hazards. As mentioned above, however, the unpredictable interleaving of turns for scheduled closures can cause the state of the grain to be different than when the closure was scheduled, so developers must still watch out for interleaving bugs.-->
然而Orleans可能同时并行执行属于不同激活的多个turn，每一个激活将永远同一时间只执行一个turn。这表示不需要使用锁或者其他同步机制来避免数据竞争或者其他的多线程带来的问题。正如之前提到的，预定的闭包的无法预知的turn交替会导致grain的状态与被预定的时候不同，所以开发者必须注意turn交替产生的bug。

### 激活模式
<!--### Activation modes-->

<!--Orleans supports two modes: single activation mode (default), in which only one simultaneous activation of every grain is created, and stateless worker mode, in which independent activations of a grain are created to increase the throughput. -->
<!--"Independent" implies that there is no state reconciliation between different activations of the same grain. -->
<!--So this mode is appropriate for grains that hold no local state, or grains whose local state is static, such as a grain that acts as a cache of persistent state-->
Orleans支持两种模式：单激活模式（默认），每个grain仅有一个激活。和无状态可做模式，一个grain有多个独立激活创建来提高吞吐。
“独立”指的是同一个grain的不同激活之间没有状态协调。
所以这个模式适合本地无状态的grain，或者本地状态是静态的grain，例如一个用作持久状态缓存的grain。

## Next
<!--## Next-->
<!--Next we look at Silos, a unit for hosting grains.-->
下面我来看一下Silos，grain的宿主单元。

[Silos](Silos.md)