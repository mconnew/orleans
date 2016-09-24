---
layout: page
title: Clients
---


## Orleans和客户端代码
<!--## Orleans and Client Code-->

<!--An Orleans application consists of two distinct parts: the Orleans (grain based) part, and the client part.-->
一个Orleans应用包含两个分开的部分：Orleans（基于grain的）部分和客户端部分。

<!--The Orleans part is comprised of application grains hosted by Orleans Runtime servers called silos.-->
Orleans部分包含寄宿在Orleans运行时服务器（silo）的上的应用grain。
<!--Grain code is executed by the runtime under scheduling restrictions and guarantees inherent in the Orleans programming model, detailed previously.-->
grain代码是在运行时的调度约束下执行，并且grain代码保证是遵守Orleans编程模型编写的，这在前面已经介绍过。

<!--The client part, usually a web front-end, connects to the Orleans part via a thin layer of Orleans Client library that enables communication of the client code with grains hosted by the Orleans part via grain references.-->
客户端部分，通常是个前端web，通过很轻量的一个Orleans客户端库来接到Orleans部分，这样可以是的客户端的代码与寄宿在Orleans的grain通过grain的引用进行通信。
<!--The client part in this context means a client to the Orleans part, but it can run as part of a client or server applications.-->
这里说的客户端部分指的是一个连接到Orleans部分的客户端，但是它可以作为一个客户端或者服务端应用的一部分。

<!--For example, an ASP.NET application running on a web server can be a client part of an Orleans application.-->
例如，一个运行在web服务器上的ASP.NET应用就可以是一个客户端部分。
<!--The client part executes on top of the .NET thread pool, and is not subject to scheduling restrictions and guarantees of the Orleans Runtime.-->
客户端部分在.NET线程池之上执行，并且不受Orleans运行时的调度约束和Orleans编程模型约束。

## Next
<!--Next we look how to receive asynchronous messages, or push data, from a grain.-->
下面我们来看一下如何从一个grain接受一步消息活着推送数据。

[Client Observers](Observers.md)