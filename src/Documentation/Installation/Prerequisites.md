---
layout: page
title: Prerequisites
---

# 必备条件
<!-- # Prerequisites -->

<!--Orleans is a set of .NET libraries. In order to use Orleans, you need [.NET Framework](http://www.microsoft.com/net) 4.5.1 or higher and a copy of [Visual Studio](https://www.visualstudio.com) 2015 or higher. Note that the Express versions of Visual Studio do not support extension packages, but you can use Orleans by adding references to the NuGet packages directly.-->
Orleans是一系列.NET库。为了使用Orleans，你需要 [.NET Framework](http://www.microsoft.com/net) 4.5.1或者更高的版本并且需要[Visual Studio](https://www.visualstudio.com) 2015或者更高的版本。注意Express版本的Visual Studio不支持扩展包，但是你还是可以通过将引用添加到NuGet包目录来使用Orleans。

<!--In production, Orleans requires persistent storage. The following technologies are supported (only need one of those):-->
在生产环境，Orleans需要持久存储。支持以下持久存储（只需要其中一个就行）:

<!--* [Azure](https://azure.microsoft.com/en-us/pricing) - Tested with [Azure SDK](http://azure.microsoft.com/en-us/downloads) 2.4 - 2.8-->
<!--* [SQL Server](https://www.microsoft.com/en-us/server-cloud/products/sql-server) 2008 or higher-->
<!--* [ZooKeeper](https://zookeeper.apache.org) 3.4.0 or higher-->
<!--* [MySQL](https://www.mysql.com) 5.0 or higher-->
<!--* [Consul](https://www.consul.io) 0.6.0 or higher-->
<!--* [DynamoDB](https://aws.amazon.com/dynamodb/) - Tested with [AWSSDK - Amazon DynamoDB 3.1.5.3](https://www.nuget.org/packages/AWSSDK.DynamoDBv2/3.1.5.3)-->
* [Azure](https://azure.microsoft.com/en-us/pricing) - 已经通过[Azure SDK](http://azure.microsoft.com/en-us/downloads) 2.4 - 2.8测试可用
* [SQL Server](https://www.microsoft.com/en-us/server-cloud/products/sql-server) 2008或者更高版本
* [ZooKeeper](https://zookeeper.apache.org) 3.4.0或者更高版本
* [MySQL](https://www.mysql.com) 5.0或者更高版本
* [Consul](https://www.consul.io) 0.6.0或者更高版本
* [DynamoDB](https://aws.amazon.com/dynamodb/) - 已经通过[AWSSDK - Amazon DynamoDB 3.1.5.3](https://www.nuget.org/packages/AWSSDK.DynamoDBv2/3.1.5.3)测试可用