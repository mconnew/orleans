---
layout: page
title: Debugging and Symbols
---


<!--An Orleans-based application can be easily debugged during development by simply attaching debugger to the silo host process, such as a host crteated with the Orleans Dev/Test Host project template, OrleansHost.exe, Azure Compute Emulator or any other host process.-->
通过简单地给silo宿主进程附加调试器，可以很容易地在开发阶段调试基于Orleans的应用，例如一个使用Dev/Test宿主工程模板开发的宿主，OrleansHost.exe、Auzure Compute Emulator或者任何其他的宿主进程。
<!--In production, it is rarely a good idea to stop a silo at a breakpoint because the frozen silo will soon get voted dead by the cluster membership protocol and will not be able to communicate with other silos in the cluster.-->
在生产环境，使用断点调试silo不是个好想法，因为暂停住的silo很快会因为集群成员协议被投票判定为死亡并且不会再与集群中其他的silo进行通讯。
<!--Hence, in productions tracing is the primary 'debugging' mechanism.-->
因此，在生产环境追踪是主要的“调试”机制。
 

## Symbols
<!--Symbols for Orleans binaries are published to [https://nuget.smbsrc.net](https://nuget.smbsrc.net) symbols server. Add it to the list of symbols server in the Visual Studio options under Debugging/Symbols for debugging Orleans code. Make sure there is traling slash in the URL. Visual Studio 2015 has a bug with parsing it.-->
Orleans二进制可执行程序的调试符文件发布在调试服务器 [https://nuget.smbsrc.net](https://nuget.smbsrc.net) 。将这个调试符服务器添加到Visual Studio 调试符服务器列表配置中在 Debugging/Symbols。确保URL结尾有有斜杠。Visual Studio 2015在解析URL的时候有个BUG。

## Sources

<!--You can download zipped sources for specific releases of Orleans from the [Releases page](https://github.com/dotnet/orleans/releases).-->
你可以从[Releases page](https://github.com/dotnet/orleans/releases)下载压缩了特定发布版本的Orleans源码。
