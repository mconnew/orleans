---
layout: page
title: Orleans Streams Quick Start
---

# Orleans流快速入门

这个指南将会展示设置和使用Orleans Streams的快速方法。
阅读这个文档的其他部分来学习更多关于流的细节。
<!--This guide will show you a quick way to setup and use Orleans Streams.-->
<!--To learn more about the details of the streaming features, read other parts of this documentation.-->

## 必要的设置
<!--## Required Configurations-->

在这个指导中，我们用一个使用了grain消息给订阅者发送流数据的简单消息流。使用内存存储提供者存储订阅者列表，尽管在实际生产环境中不是一个明智的选择。
<!--In this guide we'll use a Simple Message based Stream which uses grain messaging to send stream data to subscribers. We will use the in-memory storage provider to store lists of subscriptions so it is not a wise choise for real production applications.-->

``` xml
<Globals>
    <StorageProviders>
      <Provider Type="Orleans.Storage.MemoryStorage" Name="Default" />
      <Provider Type="Orleans.Storage.MemoryStorage" Name="PubSubStore" />
    </StorageProviders>
    <StreamProviders>
      <Provider Type="Orleans.Providers.Streams.SimpleMessageStream.SimpleMessageStreamProvider" Name="SMSProvider"/>
    </StreamProviders>
```

现在我们可以创建流，使用它们作为生产者发送数据同时作为订阅者接收数据。
<!--Now we can create streams, send data using them as producers and also receive data as subscribers.-->

## 创建事件
<!--## Producing Events-->

<!--Producing events for streams is relatively easy. You should first get access to the stream provider which you defined in the config above (`SMSProvider`) and then choose a stream and push data to it.-->
对于流来说创建事件相对简单。你首先需要访问到你上面再配置文件中(`SMSProvider`) 定义的流提供者，并且选择一个流推送数据给它。

``` csharp
//Pick a guid for a chat room grain and chat room stream
var guid = some guid identifying the chat room
//Get one of the providers which we defined in config
var streamProvider = GetStreamProvider("SMSProvider");
//Get the reference to a stream
var stream = streamProvider.GetStream<int>(guid, "RANDOMDATA");
```

<!--As you can see our stream has a GUID and a namespace. This will make it easy to identify unique streams. For example, in a chat room namespace can "Rooms" and GUID be the owning RoomGrain's GUID.-->
你能看到我们的流有一个GUID和一个命名空间。这使得鉴别唯一的流变得容易。例如，在一个聊天室中，命名空间是"Rooms"并且GUID是特有的RoomGrain的GUID。

<!--Here we use the GUID of some known chat room. Now using the `OnNext` method of the stream we can push data to it. Let's do it inside a timer and using random numbers. You could use any other data type for the stream as well.-->
这里我们使用一些已知的聊天室的GUID。现在可以使用流的`OnNext`方法给它推送数据。我们在一个定时器中做这些并且使用随机数字。你也可以使用任何其他的数据类型。

``` csharp
RegisterTimer(s =>
        {
            return stream.OnNextAsync(new System.Random().Next());
        }, null, TimeSpan.FromMilliseconds(1000), TimeSpan.FromMilliseconds(1000));
```

## 订阅和接收流数据
<!--## Subscribing and receiving streaming data-->

<!--For receiving data we can use implicit/explicit subscriptions, which are fully described in other pages of the manual. Here we use implicit subscriptions which are easier. When a grain type wants to implicitly subscribe to a stream it uses the attribute `ImplicitStreamSubscription (namespace)]`.-->
接收数据我们可以使用显式/隐式的订阅，这在本手册的其它页会详细介绍。这里我们使用较简单的隐式订阅。当一个grain类型想要隐式地描述一个流的时候，它使用`ImplicitStreamSubscription (namespace)]`特性。

<!--For our case we'll define a ReceiverGrain like this:-->
在我们的例子中，我们这样定义一个ReceiverGrain：

``` csharp
[ImplicitStreamSubscription("RANDOMDATA")]
public class ReceiverGrain : Grain, IRandomReceiver
```

<!--Now whenever some data is pushed to the streams of namespace RANDOMDATA as we have in the timer, a grain of type `ReceiverGrain` with the same guid of the stream will receive the message. Even if no activations of the grain currently exist, the runtime will automatically create a new one and send the message to it.-->
现在无论何时给命名空间是RANDOMDATA的流推送数据，具有相同guid类型是`ReceiverGrain`的grain将会收到消息。甚至当前没有激活的grain存在，运行时会自动创建一个新的并且发送消息给它。

<!--In order for this to work however, we need to complete the subscription process by setting our `OnNext` method for receiving data. So our `ReceiverGrain` should call in its `OnActivateAsync` something like this-->
为了让这个工作，我们需要通过设置我们的接受数据的`OnNext`方法完成订阅的过程。`ReceiverGrain`应该在它的`OnActivateAsync`有类似如下的调用。

``` csharp
//Create a GUID based on our GUID as a grain
var guid = this.GetPrimaryKey();
//Get one of the providers which we defined in config
var streamProvider = GetStreamProvider("SMSProvider");
//Get the reference to a stream
var stream = streamProvider.GetStream<int>(guid, "RANDOMDATA");
//Set our OnNext method to the lambda which simply prints the data, this doesn't make new subscriptions
await stream.SubscribeAsync<int>(async (data, token) => Console.WriteLine(data));
```

<!--We are all set now. The only requirement is that something triggers our producer grain's creation and then it will registers the timer and starts sending random ints to all interested parties.-->
所有的都设置好了。现在唯一需要的是触发我们的生产者grain创建并且之后它将注册定时器并且开始发送随机整数给所有感兴趣的订阅者。

<!--Again, this guide skips lots of details and is only good for showing the big picture. Read other parts of this manual and other resources on RX to gain a good understanding on what is available and how.-->
此外，这个指南跳过了许多细节并且只是展示了重点。阅读手册的其他部分和其他的关于RX的资源来更好地理解可以和如何干什么。

<!--Reactive programming can be a very powerful approach to solve many problems. You could for example use LINQ in the subscriber to filter numbers and do all sorts of interesting stuff.-->
响应式编程是解决许多问题的有效方式。例如你可以在订阅者中使用LINQ来过滤数字和各式各样有趣的事。


## Next
[Orleans Streams Programming APIs](Streams-Programming-APIs.md)