---
layout: page
title: Developing a Grain
---


<!--Please read about [Grains](/orleans/Getting-Started-With-Orleans/Grains) before reading this article.-->
阅读本文之前，请阅读[Grains](/orleans/Getting-Started-With-Orleans/Grains)。

Grain接口
<!--## Grain Interfaces-->

<!--Grains interact with each other by invoking methods declared as part of the respective grain interfaces.-->
Grain之间交互是通过调用声明成相应grain接口的一部分的方法来实现。
<!--A grain class implements one or more previously declared grain interfaces.-->
一个grain类实现一个或多个之前声明过的grain接口。
<!--All methods of a grain interface must return a `Task` (for `void` methods) or a `Task<T>` (for methods returning values of type `T`).-->
所有grain接口的方法都必须返回一个`Task`（对于`void`的方法）或者一个`Task<T>`（对于返回值类型为`T`的方法）。

<!--The following is an excerpt from the [Presence Service](/orleans/Samples-Overview/Presence-Service) sample:-->
下面是一段来自[在线服务](/orleans/Samples-Overview/Presence-Service)示例的节选：

<!--``` csharp-->
<!--//an example of a Grain Interface-->
<!--public interface IPlayerGrain : IGrainWithGuidKey-->
<!--{-->
<!--  Task<IGameGrain> GetCurrentGame();-->
<!--  Task JoinGame(IGameGrain game);-->
<!--  Task LeaveGame(IGameGrain game);-->
<!--}-->

<!--//an example of a Grain class implementing a Grain Interface-->
<!--public class PlayerGrain : Grain, IPlayerGrain-->
<!--{-->
<!--    private IGameGrain currentGame;-->

<!--    // Game the player is currently in. May be null.-->
<!--    public Task<IGameGrain> GetCurrentGame()-->
<!--    {-->
<!--       return Task.FromResult(currentGame);-->
<!--    }-->

<!--    // Game grain calls this method to notify that the player has joined the game.-->
<!--    public Task JoinGame(IGameGrain game)-->
<!--    {-->
<!--       currentGame = game;-->
<!--       Console.WriteLine("Player {0} joined game {1}", this.GetPrimaryKey(), game.GetPrimaryKey());-->
<!--       return TaskDone.Done;-->
<!--    }-->

<!--   // Game grain calls this method to notify that the player has left the game.-->
<!--   public Task LeaveGame(IGameGrain game)-->
<!--   {-->
<!--       currentGame = null;-->
<!--       Console.WriteLine("Player {0} left game {1}", this.GetPrimaryKey(), game.GetPrimaryKey());-->
<!--       return TaskDone.Done;-->
<!--   }-->
<!--}-->
<!--```-->
``` csharp
//一个Grain接口的例子
public interface IPlayerGrain : IGrainWithGuidKey
{
  Task<IGameGrain> GetCurrentGame();
  Task JoinGame(IGameGrain game);
  Task LeaveGame(IGameGrain game);
}

//一个实现了Grain接口的Grain类的例子
public class PlayerGrain : Grain, IPlayerGrain
{
    private IGameGrain currentGame;

    // 目前在在线的游戏玩家。可能为null。
    public Task<IGameGrain> GetCurrentGame()
    {
       return Task.FromResult(currentGame);
    }

    // 游戏grain调用这个方法，来通知有玩家加入了游戏。
    public Task JoinGame(IGameGrain game)
    {
       currentGame = game;
       Console.WriteLine("Player {0} joined game {1}", this.GetPrimaryKey(), game.GetPrimaryKey());
       return TaskDone.Done;
    }

   // 游戏grain调用这个方法，来通知有玩家离开了游戏。
   public Task LeaveGame(IGameGrain game)
   {
       currentGame = null;
       Console.WriteLine("Player {0} left game {1}", this.GetPrimaryKey(), game.GetPrimaryKey());
       return TaskDone.Done;
   }
}
```

## Grain引用
<!--## Grain Reference-->

<!--A Grain Reference is a proxy object that implements the same grain interface implemented by the corresponding grain class. Using asynchronous messaging, it provides full-duplex communication with other grains, as well as [Orleans Client](/Orleans/Getting-Started-With-Orleans/Clients) code.-->
一个Grain的引用是一个代理对象，实现了与相应grain类同样的grain接口。它使用异步消息提供与其他grain的全双工通信，就像[Orleans客户端](/Orleans/Getting-Started-With-Orleans/Clients)。
<!--A grain reference can be constructed by passing the identity of a grain to the `GrainFactory.GetGrain<T>()` method, where T is the grain interface. Developers can use grain references like any other .NET object. It can be passed to a method, used as a method return value, etc.-->
一个grain引用能通过传递一个grain的标识给`GrainFactory.GetGrain<T>()`方法来创建，T是grain接口。开发者能够想使用其他.NET对象那样使用grain引用。它能够传递给一个方法、用作方法的返回值等等。

<!--The following are examples of how to construct a grain reference of the `IPlayerGrain` interface defined above.-->
下面的例子是如何创建一个之前定义过的`IPlayerGrain`接口的grain引用。

<!--In Orleans Client code:-->
Orleans客户端代码：

<!--```csharp-->
<!--    //construct the grain reference of a specific player-->
<!--    IPlayerGrain player = GrainClient.GrainFactory.GetGrain<IPlayerGrain>(playerId);-->
<!--```-->
```csharp
    //创建特定玩家的grain引用
    IPlayerGrain player = GrainClient.GrainFactory.GetGrain<IPlayerGrain>(playerId);
```

<!--From inside a Grain class:-->
一个Grain类的内部：

<!--```csharp-->
<!--    //construct the grain reference of a specific player-->
<!--    IPlayerGrain player = GrainFactory.GetGrain<IPlayerGrain>(playerId);-->
<!--```-->
```csharp
    //创建特定玩家的grain引用
    IPlayerGrain player = GrainFactory.GetGrain<IPlayerGrain>(playerId);
```

## Grain方法的调用
<!--## Grain Method Invocation-->

<!--The Orleans programming model is based on Asynchronous Programming with async and await. A detailed article about the subject is [here](https://msdn.microsoft.com/en-us/library/hh191443.aspx).-->
Orleans编程模型是基于async和await的异步编程。关于这个主题的详细文章[在此](https://msdn.microsoft.com/en-us/library/hh191443.aspx)。

<!--Using the grain reference from the previous example, the following is an example of grain method invocation:-->
使用之前例子中的grain引用，下面是一个grain方法调用的例子：

<!--```csharp-->
<!--//Invoking a grain method asynchronously-->
<!--Task joinGameTask = player.JoinGame(this);-->
<!--//The `await` keyword effectively turns the remainder of the method into a closure that will asynchronously execute upon completion of the Task being awaited without blocking the executing thread.-->
<!--await joinGameTask;-->
<!--//The next lines will be turned into a closure by the C# compiler.-->
<!--players.Add(playerId);-->

<!--```-->
```csharp
//异步调用一个grain方法
Task joinGameTask = player.JoinGame(this);
//`await`关键字有效地把方法其余的部分转变成一个闭包，在这个正在等待的Task完成后，这个闭包将会异步执行，这样不会阻塞正在执行的线程。
await joinGameTask;
//C#编译器会将下面这一行转换成一个闭包。
players.Add(playerId);

```

<!--It is possible to join two or more `Task`s; the join creates a new `Task` that is resolved when all of its constituent `Task`s are completed. This is a useful pattern when a grain needs to start multiple computations and wait for all of them to complete before proceeding.-->
可以将两个或者多个`Task`联合起来；这种联合会创建一个新的`Task`，这个新的`Task`将在所有子`Task`完成后被完成。对于一个grain需要启动多个计算并且等待他们全都完成处理的时候这是一个非常有用的模式。
<!--For example, a front-end grain that generates a web page made of many parts might make multiple back-end calls, one for each part, and receive a `Task` for each result.-->
例如，一个生成由多个部分组成的web页面的前端grain可能进行多次后端调用，每个部分一次，并且每个结果收到一个`Task`。
<!--The grain would then wait for the join of all of these `Task`s; when the join is resolved, the individual `Task`s have been completed, and all the data required to format the web page has been received.-->
grain将会等在所有这些`Task`的组合；当这个组合`Task`完成，每一个独立的`Task`就完成了，并且所有用来组成web页的数据都已经收到了。

<!--Example:-->
例子：

``` csharp
List<Task> tasks = new List<Task>();
ChirperMessage chirp = CreateNewChirpMessage(text);
foreach (IChirperSubscriber subscriber in Followers.Values)
{
   tasks.Add(subscriber.NewChirpAsync(chirp));
}
Task joinedTask = Task.WhenAll(tasks);
await joinedTask;
```

## TaskDone.Done实用属性
<!--## TaskDone.Done Utility Property-->

<!--There is no "standard" way to conveniently return an already completed "void" `Task`, so Orleans sample code defines `TaskDone.Done` for that purpose.-->
没有方便返回一个已经完成的"void"`Task`的“标准”方法，所以Orleans示例代码使用`TaskDone.Done`来达到这个目的。

## Next

<!--[Developing a Client](Developing-a-Client)-->
[开发一个客户端](Developing-a-Client.md)

