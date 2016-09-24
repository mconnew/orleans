---
layout: page
title: Silos
---


<!--An Orleans silo is a server that hosts and executes Orleans grains. It has one listening port for silo-to-silo messaging and another for client-to-silo messaging. Typically, one silo is run per machine.-->
一个Orleans silo是一个执行Orleans grain的宿主服务器。silo到silo的消息和其他的client到silo的消息都通过同一个监听端口。通常一个机器运行一个silo。

## Cluster
<!--A number of silos can work together by forming an Orleans cluster. Orleans runtime fully automates cluster management. -->
一定数量的silo可以一起工作组成一个Orleans集群。Orleans运行时完全自动化管理集群。
<!--All silos use a shared membership store that is updated dynamically and helps coordinate cluster management.-->
所有的silo使用一个共享的自动更新的鉴权存储，并且帮助协调集群管理。
<!--Silos learn about each others' status by reading the shared store. At any time, a silo can join a cluster by registering in a the shared store. This way the cluster can can scale-out dynamically at runtime.-->
Silo通过读取共享存储得知其他的silo的状态。任何时候，一个silo可以通过在共享存储中注册来加入一个群。这样集群可以在运行时动态的扩容。
<!--Orleans provides resilience and availability by removing unresponsive silos from the cluster.-->
Orlean通过从集群中移除不响应的silo来保证集群的可靠性和可用性。

<!--For an in-depth detailed documentation of how Orleans manages a cluster, read about [Cluster Management](/orleans/Runtime-Implementation-Details/Cluster-Management).-->
想了解更多Orlean如何管理集群的细节，请阅读[集群管理](/orleans/Runtime-Implementation-Details/Cluster-Management)。

## Next
<!--Next we look at what a client is and how it interacts in the Orleans architecture.-->
下面我们来了解什么是client和它如何与Orleans互动。

[Clients](Clients.md)
