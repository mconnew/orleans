---
layout: page
title: Running the Application
---


## 设置到Orleans的连接
<!--## Configuring Connections to Orleans-->

<!--To allow applications to communicate with grains from outside Orleans, the framework includes a client library.-->
框架提供一个客户端库，来让Orleans外的应用于grain进行通信。
<!--This client library might be used by a desktop or mobile application, or by a front end server that renders interactive web pages or exposes a web services API.-->
刻个客户端库可以被桌面或者移动应用使用，或者渲染web页的前段服务器使用或者暴露一个web服务API。
<!--The client library provides an API for writing asynchronous clients that communicate with Orleans grains.-->
客户端库提供了编写与Orleans grain通信的异步客户端所需的API。
<!--Once the client library is connected to an Orleans gateway, a client can send messages to grains, receive responses and receive asynchronous notifications from grains via observers.-->
一旦客户端库连接上Orleans的网关，客户端可以发送消息给grain，收取响应并且通过观察者收取来自grain的异步通知。

## 连接网关
<!--## Connecting to a Gateway-->

<!--To establish a connection, a client calls `GrainClient.Initialize()`.-->
客户端调用`GrainClient.Initialize()`建立一个连接。
<!--This will connect to the gateway silo at the IP address and port specified in the _ClientConfiguration.xml_ file.-->
这会连接到 _ClientConfiguration.xml_ 文件中指定的IP地址和端口的网关silo。
<!--This file must be placed in the same directory as the _Orleans.dll_ library used by the client.-->
这个文件必须与客户端使用的 _Orleans.dll_ 放在相同目录。
<!--As an alternative, a configuration object can be passed to `GrainClient.Initialize()` programmatically instead of loading it from a file.-->
一个替代方案是，可以以编程的方式传递一个配置对象给`GrainClient.Initialize()`而不是从文件读取。

## 配置客户端
<!--## Configuring the Client-->

<!--In _ClientConfiguration.xml_, the `Gateway` element specifies the address and port of the gateway endpoint that need to match those in _OrleansConfiguration.xml_ on the silo side:-->
在_ClientConfiguration.xml_中，`Gateway`指定网关终结点的IP地址和端口，需要与silo的_OrleansConfiguration.xml_配置一致。

```xml
<ClientConfiguration xmlns="urn:orleans">
    <Gateway Address="<IP address or host name of silo>" Port="30000" />
</ClientConfiguration>
```

<!--If an Orleans-based application runs in Windows Azure, the client automatically discovers silo gateways and shouldn't be statically configured.-->
如果一个基于Orleans的应用运行在Windows Azure中，客户端自动发现silo网关并且不应该被静态配置。
<!--Refer to the [Azure application sample](../Samples-Overview/Azure-Web-Sample) for an example of how to configure the client.-->
参考[Azure应用示例](../Samples-Overview/Azure-Web-Sample.md)中的例子来学习怎么配置客户端。

## 配置Silo
<!--## Configuring Silos-->

<!--In _OrleansConfiguration.xml_, the `ProxyingGateway` element specifies the gateway endpoint of the silo, which is separate from the inter-silo endpoint defined by the Networking element and must have a different port number:-->
在_OrleansConfiguration.xml_中的`ProxyingGateway`元素指定silo的网关终结点，与`Networking`元素定义的内部终结点不同并且必须有不同的端口号：

```xml
<?xml version="1.0" encoding="utf-8"?>
<OrleansConfiguration xmlns="urn:orleans">
    <Defaults>
    <Networking Address="" Port="11111" />
    <ProxyingGateway Address="" Port="30000" />
    </Defaults>
</OrleansConfiguration>
```
