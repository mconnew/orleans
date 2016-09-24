---
layout: page
title: Orleans NuGet Packages
---

# Nuget包
<!-- # Nuget Packages -->

## Orleans NuGet包[v1.2.0](https://github.com/dotnet/orleans/releases/tag/v1.2.0)
<!--## Orleans NuGet packages as of [v1.2.0](https://github.com/dotnet/orleans/releases/tag/v1.2.0)-->

<!--There are 4 key NuGet packages you will need to use in most scenarios:-->
在大多数场景中，你需要用到4个关键的NuGet包：

### [Microsoft.Orleans.OrleansCodeGenerator.Build](http://www.nuget.org/packages/Microsoft.Orleans.OrleansCodeGenerator.Build/)

```
PM> Install-Package Microsoft.Orleans.OrleansCodeGenerator.Build 
```

<!--Build support for grain interfaces and implementation projects. Add it to your grain interfaces and implementation projects to enable code generation of grain references and serializers. `Microsoft.Orleans.Templates.Interfaces` and `Microsoft.Orleans.Templates.Grains` packages are obsolete and provided only for backward compatibility and migration.-->
grain接口和实现工程的构建支持。将它添加到你的grain接口和实现工程中来实现grain引用和序列化的代码生成。`Microsoft.Orleans.Templates.Interfaces`和`Microsoft.Orleans.Templates.Grains`包已经被弃用，仅仅是为了向后兼容和迁移而保留。

### [Microsoft.Orleans.Core](http://www.nuget.org/packages/Microsoft.Orleans.Core/)

```
PM> Install-Package Microsoft.Orleans.Core
```

<!--Contains Orleans.dll, which defines most of Orleans public types and Orleans Client. Reference it for building libraries and client applications that use Orleans types but don't need any of the included providers.-->
包括Orleans.dll定义了大多数的Orleans公共类型和Orleans客户端。引用它来构建使用Orleans类型但是不使用任何包含的provider的库或者客户端应用。

### [Microsoft.Orleans.Server](http://www.nuget.org/packages/Microsoft.Orleans.Server/)

```
PM> Install-Package Microsoft.Orleans.Server
```

<!--Includes everything you need to run a silo.-->
包括运行一个silo所需要的一切。


### [Microsoft.Orleans.Client](http://www.nuget.org/packages/Microsoft.Orleans.Client/)

```
PM> Install-Package Microsoft.Orleans.Client
```

<!--Includes everything you need for an Orleans client (frontend).-->
包括Orleans客户端（前端）所需要的一切。

---

## 额外的包
<!--## Additional Packages-->

<!--The below packages provide additional functionality.-->
下面的包提供额外的功能。

### [Microsoft.Orleans.OrleansServiceBus](http://www.nuget.org/packages/Microsoft.Orleans.OrleansServiceBus/)

```
PM> Install-Package Microsoft.Orleans.OrleansServiceBus
```
<!--Includes the stream provider for Azure Event Hubs.-->
包括使用Azure Event Hubs所需的stream provider。

### [Microsoft.Orleans.OrleansHost](http://www.nuget.org/packages/Microsoft.Orleans.OrleansHost/)

```
PM> Install-Package Microsoft.Orleans.OrleansHost
```
<!--Includes the default silo host - OrleansHost.exe. Can be used for on-premises deployments or as an out-of-process silo host in Azure Worker Role. Included in Microsoft.Orleans.Server.-->
包括默认的silo宿主——OrleansHost.exe。可以用作内部部署或者以Azure Worker Role作为一个进程外silos。包含在Microsoft.Orleans.Server中。

### [Microsoft.Orleans.OrleansAzureUtils](http://www.nuget.org/packages/Microsoft.Orleans.OrleansAzureUtils/)

```
PM> Install-Package Microsoft.Orleans.OrleansAzureUtils
```
<!--Contains a wrapper class that simplifies instantiation of silos and clients in Azure Worker/Web roles, Azure Table based membership provider, and persistence and stream providers for Azure Storage.-->
包含一个封装类来简化silo和client作为Azure Worker/Web roles实例化，基于Azure Table的鉴权provider，和Azure Storage的持久化和流provider。


### [Microsoft.Orleans.OrleansProviders](http://www.nuget.org/packages/Microsoft.Orleans.OrleansProviders/)

```
PM> Install-Package Microsoft.Orleans.OrleansProviders
```
<!--Contains a set of built-in persistence and stream providers. Included in Microsoft.Orleans.Client and Microsoft.Orleans.Server.-->
包含一系列内置的持久化和流的提供程序。包含在Microsoft.Orleans.Client和Microsoft.Orleans.Server中。

### [Microsoft.Orleans.CounterControl](http://www.nuget.org/packages/Microsoft.Orleans.CounterControl/)

```
PM> Install-Package Microsoft.Orleans.CounterControl
```
<!--Includes OrleansCounterControl.exe, which registers Windows performance counter categories for Orleans statistics and for deployed grain classes. Requires elevation. Can be executed in Azure as part of a role startup task. Included in Microsoft.Orleans.Server.-->
包括OrleansCounterControl.exe，用来在Windows性能计数器中注册Orleans和部署的grain类的的统计分类。能在Azure中作为一个启动任务执行。包含在Microsoft.Orleans.Serve中。

### [Microsoft.Orleans.OrleansManager](http://www.nuget.org/packages/Microsoft.Orleans.OrleansManager/)

```
PM> Install-Package Microsoft.Orleans.OrleansManager
```
<!--Includes Orleans management tool - OrleansManager.exe.-->
包括Orleans管理工具 —— OrleansManager.exe。

### [Microsoft.Orleans.OrleansConsulUtils](http://www.nuget.org/packages/Microsoft.Orleans.OrleansConsulUtils/)

```
PM> Install-Package Microsoft.Orleans.OrleansConsulUtils
```
<!--Includes the plugin for using Consul for storing cluster membership data.-->
包括使用Consul来存储集群成员身份数据的插件。

### [Microsoft.Orleans.OrleansZooKeeperUtils](http://www.nuget.org/packages/Microsoft.Orleans.OrleansZooKeeperUtils/)

```
PM> Install-Package Microsoft.Orleans.OrleansZooKeeperUtils
```
<!--Includes the plugin for using ZooKeeper for storing cluster membership data.-->
包括使用ZooKeeper来存储系群成员身份数据的插件。

### [Microsoft.Orleans.TestingHost](http://www.nuget.org/packages/Microsoft.Orleans.TestingHost/)

```
PM> Install-Package Microsoft.Orleans.TestingHost
```
<!--Includes the library for hosting silos in a testing project.-->
包括在测试工程中寄宿silo的库。

### [Microsoft.Orleans.OrleansCodeGenerator](http://www.nuget.org/packages/Microsoft.Orleans.OrleansCodeGenerator/)

```
PM> Install-Package Microsoft.Orleans.OrleansCodeGenerator
```
<!--Includes the run time code generator. Included in Microsoft.Orleans.Server and Microsoft.Orleans.Client.-->
包括运行时代码生成器。包含在Microsoft.Orleans.Server和Microsoft.Orleans.Client中。

### [Microsoft.Orleans.OrleansTelemetryConsumers.AI](http://www.nuget.org/packages/Microsoft.Orleans.OrleansTelemetryConsumers.AI/)

```
PM> Install-Package Microsoft.Orleans.OrleansTelemetryConsumers.AI
```
<!--Includes the telemetry consumer for Azure Application Insights.-->
包括[Azure Application Insights](https://azure.microsoft.com/zh-cn/services/application-insights/)的遥测客户端。

### [Microsoft.Orleans.OrleansTelemetryConsumers.NewRelic](http://www.nuget.org/packages/Microsoft.Orleans.OrleansTelemetryConsumers.NewRelic/)

```
PM> Install-Package Microsoft.Orleans.OrleansTelemetryConsumers.NewRelic
```
<!--Includes the telemetry consumer for NewRelic.-->
包括NewRelic的遥测客户端。

### [Microsoft.Orleans.Serialization.Bond](http://www.nuget.org/packages/Microsoft.Orleans.Serialization.Bond/)

```
PM> Install-Package Microsoft.Orleans.Serialization.Bond
```
<!--Includes support for [Bond serializer](https://github.com/microsoft/bond).-->
包括[Bond serializer](https://github.com/microsoft/bond)的支持。
