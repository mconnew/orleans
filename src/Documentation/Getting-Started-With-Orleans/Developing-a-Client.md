---
layout: page
title: Developing a Client
---


<!--Once we have our grain type implemented, we can write a client application that uses the type.-->
一旦我们实现了grain类型，我们就可以写一个客户端应用来说使用这个类型。

<!--The following Orleans DLLs from either the `[SDK-ROOT]\Binaries\PresenceClient_ or _[SDK-ROOT]\Samples\References` directories need to be referenced in the client application project:-->
下面的来自`[SDK-ROOT]\Binaries\PresenceClient_ or _[SDK-ROOT]\Samples\References`目录的Orleans DLL需要引入到客户端的应用工程中：

* Orleans.dll
* OrleansRuntimeInterfaces.dll

<!--Almost any client will involve use of the grain factory class.-->
几乎任何客户端都会用到grain的工厂类。
<!--The `GetGrain()` method is used for getting a grain reference for a particular ID.-->
`GetGrain()`用来得到一个特定ID的grain引用。
<!--As was already mentioned, grains cannot be explicitly created or deleted.-->
就像之前所说的，grain不能够显式地创建和销毁。

``` csharp
GrainClient.Initialize();

// 硬编码了玩家ID
Guid playerId = new Guid("{2349992C-860A-4EDA-9590-000000000006}");
IPlayerGrain player = GrainClient.GrainFactory.GetGrain<IPlayerGrain>(playerId);

IGameGrain game = player.CurrentGame.Result;
var watcher = new GameObserver();
var observer = GrainClient.GrainFactory.CreateObjectReference<IGameObserver>(watcher);
await game.SubscribeForGameUpdates();
```
<!--``` csharp-->
<!--GrainClient.Initialize();-->

<!--// Hardcoded player ID-->
<!--Guid playerId = new Guid("{2349992C-860A-4EDA-9590-000000000006}");-->
<!--IPlayerGrain player = GrainClient.GrainFactory.GetGrain<IPlayerGrain>(playerId);-->

<!--IGameGrain game = player.CurrentGame.Result;-->
<!--var watcher = new GameObserver();-->
<!--var observer = GrainClient.GrainFactory.CreateObjectReference<IGameObserver>(watcher);-->
<!--await game.SubscribeForGameUpdates();-->
<!--```-->

<!--If this code is used from the main thread of a console application, you have to call `Wait()` on the task returned by `game.SubscribeForGameUpdates()` because `await` does not prevent the `Main()` function from returning, which will cause the client process to exit.-->
如果这段代码用在控制台应用的主线程，你需要在`game.SubscribeForGameUpdates()`返回后调用`Wait()`，因为`await`不会组织 `Main()`函数返回，这样会使进程退出。

<!--See the Key Concepts section for more details on the various ways to use `Task`s for execution scheduling and exception flow.-->
阅读关键概念部分来获取更多`Task`执行调度和异常流的不同用法的细节。

## 找到或者创建grain
<!--## Find or create grains-->

<!--After establishing a connection by calling `GrainClient.Initialize()`, static methods in the generic factory class may be used to get a reference to a grain, such as `GrainClient.GrainFactory.GetGrain<IPlayerGrain>()` for the `PlayerGrain`. The grain interface is passed as a type argument to `GrainFactory.GetGrain<T>()`.-->
在通过调用`GrainClient.Initialize()`创建链接后，泛型工厂类中的静态方法可以用来获取一个grain的引用，例如`GrainClient.GrainFactory.GetGrain<IPlayerGrain>()`来获取`PlayerGrain`。grain接口作为类参数传递给`GrainFactory.GetGrain<T>()`。

## 向grain发送消息
<!--## Sending messages to grains-->

<!--The programming model for communicating with grains from a client is almost the same as from a grain.-->
客户端与grain的通信编程模型跟grain之间的通信编程模型一样。
<!--The client holds grain references which implement a grain interface like `IPlayerGrain`.-->
client持有一个实现了grain接口`IPlayerGrain`的grain引用。
<!--It invokes methods on that grain reference, and these return asynchronous values: `Task`/`Task<T>`, or another grain interface inheriting from `IGrain`.-->
它调用那个grain引用或者其他继承自`IGrain`的grain接口的方法，并且这些方法有异步返回值`Task`/`Task<T>`。
<!--The client can use the `await` keyword or `ContinueWith()` method to queue continuations to be executed when these asynchronous values resolve, or the `Wait()` method to block the current thread.-->
当这些异步返回值求值得时候，客户端可以使用`await`关键字或者`ContinueWith()`方法来伫列被运行的延续体，或者使用`Wait()`方法阻塞当前线程。

<!--The one key difference between communicating with a grain from within a client or from within another grain is the single-threaded execution model.-->
一个客户端与grain的通信和grain与grain的通信关键的不同是单线程执行模型。
<!--Grains are constrained to be single-threaded by the Orleans scheduler, while clients may be multi-threaded.-->
grain被Orleans调度器线支撑单线程的，但是客户端可能是多线程的。
<!--The client library uses the TPL thread pool to manage continuations and callbacks, and so it is up to the client to manage its own concurrency using whatever synchronization constructs are appropriate for its environment – locks, events, TPL tasks, etc.-->
客户端库使用TPL线程池来管理延续提或者毁掉，并且客户端根据自身环境适用什么同步机制（锁、事件、TPL任务、等等）来自由决定使用什么方法管理它自己的并发。

## 收到通知
<!--## Receiving notifications-->

<!--There are situations in which a simple message/response pattern is not enough, and the client needs to receive asynchronous notifications.-->
有些情况下简单的消息/响应模式是不够的，并且客户端需要收到异步通知。
<!--For example, a user might want to be notified when a new message has been published by someone that she is following.-->
例如，一个用户可能想要在他粉的某人发布新消息的时候收到通知。

<!--An observer is a one-way asynchronous interface that inherits from `IGrainObserver`, and all its methods must be `void`.-->
观察者是一个单向实现了`IGrainObserver`的单向异步接口，并且它的所有的方法都必须是`void`的。
<!--The grain sends a notification to the observer by invoking it like a grain interface method, except that it has no return value, and so the grain need not depend on the result.-->
grain通过像调用grain接口的方法一样调用观察者的方法来发送一个通知给观察者，不同是观察者方法没有返回值并且grain不会以来调用结果。
<!--The Orleans runtime will ensure one-way delivery of the notifications.-->
Orleans运行时将会确保单向传递通知的成功。
<!--A grain that publishes such notifications should provide an API to add or remove observers.-->
发布通知的grain应该提供API来添加或者删除观察者。

<!--To subscribe to a notification, the client must first create a local C# object that implements the observer interface.-->
想要订阅通知，客户端必须首先创建一个本地的实现了观察者接口的C#对象。
<!--It then calls `CreateObjectReference()` method on the grain factory, to turn the C# object into a grain reference, which can then be passed to the subscription method on the notifying grain.-->
然后调用grain工厂的`CreateObjectReference()`将C#对象转换成一个grain引用，之后会被传递给通知grain上的订阅方法。

<!--This model can also be used by other grains to receive asynchronous notifications.-->
这个模型也可以被其他的grain使用来接收异步通知。
<!--Unlike in the client subscription case, the subscribing grain simply implements the observer interface as a facet, and passes in a reference to itself (e.g. `this.AsReference<IChirperViewer>`).-->
不像客户端订阅的情况，订阅的grain只要实现观察者接口，并且把自身作为一个引用传递给自己（例如`this.AsReference<IChirperViewer>`）。

## 举例
<!--## Example-->

<!--Here is an extended version of the example given above of a client application that connects to Orleans, finds the player account, subscribes for updates to the game session the player is part of, and prints out notifications until the program is manually terminated.-->
这里是一个上面给出的客户端应用连接Orleans的扩展版本，找到玩家账号，订阅玩家所在的游戏的会话的更新，并且打印出通知，直到程序被手动关闭。

``` csharp
namespace PlayerWatcher
{
    class Program
    {
        /// <summary>
        /// Simulates a companion application that connects to the game
        /// that a particular player is currently part of, and subscribes
        /// to receive live notifications about its progress.
        /// </summary>
        static void Main(string[] args)
        {
            try
            {
                GrainClient.Initialize();

                // Hardcoded player ID
                Guid playerId = new Guid("{2349992C-860A-4EDA-9590-000000000006}");
                IPlayerGrain player = GrainClient.GrainFactory.GetGrain<IPlayerGrain>(playerId);
                IGameGrain game = null;

                while (game == null)
                {
                    Console.WriteLine("Getting current game for player {0}...", playerId);

                    try
                    {
                        game = player.CurrentGame.Result;
                        if (game == null) // Wait until the player joins a game
                            Thread.Sleep(5000);
                    }
                    catch (Exception exc)
                    {
                        Console.WriteLine("Exception: ", exc.GetBaseException());
                    }
                }

                Console.WriteLine("Subscribing to updates for game {0}...", game.GetPrimaryKey());

                // Subscribe for updates
                var watcher = new GameObserver();
                game.SubscribeForGameUpdates(GrainClient.GrainFactory.CreateObjectReference<IGameObserver>(watcher)).Wait();

                // .Wait will block main thread so that the process doesn't exit.
                // Updates arrive on thread pool threads.
                Console.WriteLine("Subscribed successfully. Press <Enter> to stop.");
                Console.ReadLine();
            }
            catch (Exception exc)
            {
                Console.WriteLine("Unexpected Error: {0}", exc.GetBaseException());
            }
        }

        /// <summary>
        /// Observer class that implements the observer interface.
        /// Need to pass a grain reference to an instance of this class to subscribe for updates.
        /// </summary>
        private class GameObserver : IGameObserver
        {
            // Receive updates
            public void UpdateGameScore(string score)
            {
                Console.WriteLine("New game score: {0}", score);
            }
        }
    }
}
```

## Next

[运行一个应用](Running-the-Application.md)