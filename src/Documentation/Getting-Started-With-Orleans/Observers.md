---
layout: page
title: Client Observers
---


<!--There are situations in which a simple message/response pattern is not enough, and the client needs to receive asynchronous notifications.-->
有些情况下简单的消息/响应模式是不够的，并且客户端需要收到异步通知。
<!--For example, a user might want to be notified when a new instant message has been published by a friend.-->
例如，一个用户可能想要在他的朋友发布新即时消息的时候收到通知。

<!--Client observers is a mechanism that allows notifying clients asynchronously.-->
客户端观察者是一个允许异步通知客户端的机制。
<!--An observer is a one-way asynchronous interface that inherits from `IGrainObserver`, and all its methods must be void.-->
一个观察者是一个继承自`IGrainObserver`的单向的异步接口，并且它的所有方法都是void的。
<!--The grain sends a notification to the observer by invoking it like a grain interface method, except that it has no return value, and so the grain need not depend on the result.-->
grain通过像调用grain接口的方法一样调用观察者的方法来发送一个通知给观察者，不同是观察者方法没有返回值并且grain不会依赖调用结果。
<!--The Orleans runtime will ensure one-way delivery of the notifications.-->
Orleans运行时将会确保单向传递通知的成功。 
<!--A grain that publishes such notifications should provide an API to add or remove observers.-->
发布通知的grain应该提供API来添加或者删除观察者。
<!--In addition, it is usually convenient to expose a method that allows an existing subscription to be cancelled.-->
另外，为了方便通常会暴露一个取消已有订阅的方法。
<!--Grain developers may use the Orleans `ObserverSubscriptionManager<T>` generic class to simplify development of observed grain types.-->
grain的开发者可以使用Orleans的`ObserverSubscriptionManager<T>`泛型类来简化被观察的grain类型的开发。

<!--To subscribe to a notification, the client must first create a local C# object that implements the observer interface.-->
为了订阅一个通知，客户端必须首先创建一个本地的实现了观察者接口的C#对象。
<!--It then calls a static method on the observer factory, `CreateObjectReference()`, to turn the C# object into a grain reference, which can then be passed to the subscription method on the notifying grain.-->
然后调用然后调用观察者工厂的`CreateObjectReference()`方法来把C#对象转换成一个grain引用，这样就可以传递给通知grain的订阅方法了。

<!--This model can also be used by other grains to receive asynchronous notifications.-->
这个模型也可以用作其他grain接口异步通知。
<!--Unlike in the client subscription case, the subscribing grain simply implements the observer interface as a facet, and passes in a reference to itself (e.g. `this.AsReference<IMyGrainObserverInterface>`).-->
不像客户端订阅的情况，订阅的grain只要实现观察者接口，并且把自身作为一个引用传递给自己（例如this.AsReference<IChirperViewer>）。

## 代码例子
<!--## Code Example-->

<!--Let's assume that we have a grain that periodicaly sends messages to clients. For simplicity, the message in our example will be a  string. We first define the interface on the client that will receive the message.-->
让我们假设有一个grain周期性地给客户端发送消息。为了简单期间，我们例子中的消息将会是一个字符串。我们首先在客户端定义接收这个消息的接口。

<!--the interface will look like this-->
这个接口将会是这样

``` csharp
public interface IChat : IGrainObserver
{
    void ReceiveMessage(string message);
}

```

<!--The only special thing is that the interface should inherit from `IGrainObserver`. Now any client that wants to observe those messages should implement a class which implements `IChat`.-->
唯一特殊的一点是这个接口要继承自`IGrainObserver`。现在任何想要接受这些消息的客户端应该实现一个实现了`IChat`接口的类。

<!--The simplest case would be something like this:-->
最简单的例子是这样的：

``` csharp
public class Chat : IChat
{
    public void ReceiveMessage(string message)
    {
        Console.WriteLine(message);
    }
}
```

<!--Now on the server we should have a Grain which sends these chat messages to clients. The Grain also should have a mechanism for clients to subscribe and unsubscribe themselves to receive notifications. For subscription the Grain can use the utility class `ObserverSubscriptionManager`:-->
现在在服务器上我应该有一个发送聊天消息给客户端的grain。这个grain也应该有让客户端来订阅和退订他们自己的机制。订阅这个grain可以使用实用类`ObserverSubscriptionManager`:

``` csharp
class HelloGrain : Grain, IHello
{
    private ObserverSubscriptionManager<IChat> _subsManager;

    public override async Task OnActivateAsync()
    {
        // We created the utility at activation time.
        _subsManager = new ObserverSubscriptionManager<IChat>();
        await base.OnActivateAsync();
    }

    // Clients call this to subscribe.
    public async Task Subscribe(IChat observer)
    {
        _subsManager.Subscribe(observer);
    }

    //Also clients use this to unsubscribe themselves to no longer receive the messages.
    public async Task UnSubscribe(IChat observer)
    {
        _SubsManager.Unsubscribe(observer);
    }
}
```

<!--To send the message to clients the `Notify` method of the `ObserverSubscriptionManager<IChat>` instance can be used. The method takes an `Action<T>` method or lambda expression (where `T` is of type `IChat` here). You can call any method on the interface to send it to clients. In our case we only have one method `ReceiveMessage` and our sending code on the server would look like this:-->
给客户端发消息可以使用`ObserverSubscriptionManager<IChat>`实例`Notify`方法。这个方法接受一个`Action<T>`方法或者lambda表达式（这里的T是`IChat`的类型）

``` csharp
public Task SendUpdateMessage(string message)
{
    _SubsManager.Notify(s => s.ReceiveMessage(message));
    return TaskDone.Done;
}

```

<!--Now our server has a method to send messages to observer clients, two methods for subscribing/unsubscribing and the client implemented a class to be able to observe the grain messages. The last step is to create an observer reference on the client using our previously implemented `Chat` class and let it receive the messages after subscribing it.-->
现在我们的服务器已经有一个发送消息给观察者客户端的方法，有两个订阅/退订的方法和一个实现了一个可以观察grain消息的类的客户端。最后的异步是在客户端创建一个使用我们之前实现的`Chat` 类的观察者的引用并且让它在订阅后接受消息。

<!--The code would look like this:-->
代码如下：

``` csharp
//首先创建grain的引用
var friend = GrainClient.GrainFactory.GetGrain<IHello>(0);
Chat c = new Chat();

//为chat创建一个用来订阅可观察的grain的引用。
var obj = await GrainClient.GrainFactory.CreateObjectReference<IChat>(c);
//订阅这个实例来接受消息。
await friend.Subscribe(obj);
```
<!--``` csharp-->
<!--//First create the grain reference-->
<!--var friend = GrainClient.GrainFactory.GetGrain<IHello>(0);-->
<!--Chat c = new Chat();-->

<!--//Create a reference for chat usable for subscribing to the observable grain.-->
<!--var obj = await GrainClient.GrainFactory.CreateObjectReference<IChat>(c);-->
<!--//Subscribe the instance to receive messages.-->
<!--await friend.Subscribe(obj);-->
<!--```-->

<!--Now whenever our grain on the server calls the `SendUpdateMessage` method, all subscribed clients will receive the message. In our client code, the `Chat` instance in variable `c` will receive the message and output it to the console.-->
现在无论何时我们服务器上的grain调用`SendUpdateMessage`方法，所有订阅了得客户端都将受到消息。在我们的客户端代码里，`Chat`的实例变量`c`会受到消息并且打印到控制台。

<!--**Note:** Objects passed to `CreateObjectReference` are held via a [`WeakReference<T>`](https://msdn.microsoft.com/en-us/library/system.weakreference) and will therefore be garbage collected if no other references exist. Users should maintain a reference for each observer which they do not want to be collected.-->
**注意：** 传递给`CreateObjectReference`的对象是通过通过一个[`WeakReference<T>`](https://msdn.microsoft.com/en-us/library/system.weakreference)实现并且因此如果没有其他的引用存在会被垃圾收集掉。用户应该为每一个不像被垃圾回收的观察者维护一个引用。

<!--**Note:** Support for observers might be removed in a future version and replaced with a Simple Message Stream [SMS](http://dotnet.github.io/orleans/Orleans-Streams/), which can support the same concept with more power, flexibility, and reliability.-->
**注意：** 对观察者的支持可能在将来会被移除并且被简单消息流[SMS](http://dotnet.github.io/orleans/Orleans-Streams/)取代，简单消息流能更强大灵活和可靠地支持同样的功能。

## Next

<!--Next we look at [Developing a Grain](Developing-a-Grain)-->
下面我们看一下[开发一个grain](Developing-a-Grain.md)