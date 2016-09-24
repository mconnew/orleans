---
documentType: index
title: Microsoft Orleans
tagline: A straightforward approach to building distributed, high-scale applications in .NET
---
<style>
.subtitle {
    font-size:20px;
}
.main_logo {
    width:100%
}
.jumbotron{
    text-align: center;
}
</style>


<div class="jumbotron">
    <div class="container">
      <img src="images/logo.svg" class="main_logo" />
      <h1 class="title"><small class="subtitle">一个使用.NET构建分布式、高可扩展应用的简捷方法</small></h1>
      <div class="options">
        <a class="btn btn-lg btn-primary" href="https://github.com/dotnet/orleans">获取代码</a> <a class="btn btn-lg btn-primary" href="Documentation/Introduction.md">阅读文档</a>
      </div>
    </div>
</div>

<p class="lead">
    <!-- Orleans is a framework that provides a straightforward approach to building distributed high-scale computing applications, without the need to learn and apply complex concurrency or other scaling patterns.
    It was created by Microsoft Research and designed for use in the cloud. -->
    Orleans提供了一个简单直接的构建分布式高可扩展计算应用，并且不需要学习和掌握复杂的并发或者其他的扩展模式。 它是Microsoft Research为了在云端使用而设计并且创建。
</p>

<p class="lead">
    <!-- Orleans has been used extensively in Microsoft Azure by several Microsoft product groups, most notably by 343 Industries as a platform for all of Halo 4 and Halo 5 cloud services, as well as by a growing number of other companies. -->
    Oreleans已经广泛地被Microsoft数个产品组在Microsoft Azure中使用，最知名的是343 Industries用作Halo Reach、Halo 4 和Halo 5的云服务平台，除此之外在别的一些企业中也有应用。
</p>

---

<div class="row">
    <div class="col-md-4">
        <h3>默认开启开启</h3>

        Orleans处理了构建分布式系统的复杂性问题，使得你的应用可以横向扩展到数百个服务器上。
        <!-- Orleans handles the complexity of building distributed systems, enabling your application
        to scale to hundreds of servers. -->
    </div>
    <div class="col-md-4">
        <h3>低延迟</h3>

        Orleans允许你在内存中保存状态，这样你的应用可以快速响应收到的请求。
        <!-- Orleans allows you to keep the state you need in memory, so your application can rapidly respond
        to incoming requests. -->
    </div>
    <div class="col-md-4">
        <h3>简化并发</h3>

        Orleans允许你写简单的单线程C#代码来使用actor间的异步消息传递处理并发。
        <!-- Orleans allows you to write simple, single threaded C# code, handling concurrency with asynchronous
        message passing between actors. -->
    </div>
</div>

---

<!-- In Orleans, actors are called 'grains', and are described using an interface. Async methods are used to indicate which messages the actor can receive: -->
在Orleans中，actor被称作‘grains’,并且使用接口描述。使用异步方法来指明actor接收什么样的消息：

``` csharp
public interface IMyGrain : IGrainWithStringKey
{
    Task<string> SayHello(string name);
}
```

<!-- The implementation is executed inside the Orleans framework: -->
Orleans框架会执行这些实现：

``` csharp
public class MyGrain : IMyGrain
{
    public Task<string> SayHello(string name)
    {
        return Task.FromResult($"Hello {name}");
    }
}
```

<!-- You can then send messages to the grain by creating a proxy object, and calling the methods: -->
你可以通过代理对象来把把消息发送给grain，并且调用这些方法：

``` csharp
var grain = GrainClient.GrainFactory.GetGrain<IMyGrain>("grain1");
await grain.SayHello("World");
```

<!-- ## Where Next?

To learn more about the concepts in Orleans, read the [introduction](Documentation/Introduction.md).

There are a number of [step-by-step tutorials](Tutorials/index.md).

Discuss your Orleans questions on the [gitter chat room](https://gitter.im/dotnet/orleans).

Fork the code on the [GitHub Respository](https://github.com/dotnet/orleans). -->

## 接下来

学习更多Orleans中的概念，阅读 [简介](Documentation/Introduction.md)。

还有一些[step-by-step指南](Tutorials/index.md)。

有关于Orleans的问题，可以来[gitter聊天室](https://gitter.im/dotnet/orleans)讨论。

Fork本项目[GitHub Repository](https://github.com/dotnet/orleans)。