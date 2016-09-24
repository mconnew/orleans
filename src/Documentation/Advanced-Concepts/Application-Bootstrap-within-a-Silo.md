---
layout: page
title: Application Bootstrapping within a Silo
---

# Silo中的应用引导
<!-- # Application Bootstrapping within a Silo -->

<!--There are several scenarios where application want to run some "auto-exec" functions when a silo comes online. -->
在一些场景下档silo上线的时候应用想要运行一些“自动执行”的功能。

<!--Some examples include, but are not limited to: -->
<!--* Starting background timers to perform periodic housekeeping tasks -->
<!--* Pre-loading some cache grains with data downloaded from external backing storage. -->
包括却不限于这些例子：
* 启动后台计时器来定期执行管理任务
* 用从外部后端存储下载的数据预加载一些缓存grain

<!--We have now added support for this auto-run functionality through configuring "bootstrap providers" for Orleans silos. For example:-->
我们现在通过配置Orleans silo的“引导提供者”添加自动运行功能的支持。例如：

``` xml
<OrleansConfiguration xmlns="urn:orleans">
  <Globals>
    <BootstrapProviders>
      <Provider Type="My.App.BootstrapClass1" Name="bootstrap1" />
      <Provider Type="My.App.BootstrapClass2" Name="bootstrap2" />
    </BootstrapProviders>
  </Globals>
</OrleansConfiguration>
```

<!--It is also possible to register Bootstrap provider programaticaly, via calling one of the:-->
也可以通过编码注册引导提供者，通过调用一下其一：

``` csharp
public void RegisterBootstrapProvider(string providerTypeFullName, string providerName, IDictionary<string, string> properties = null)

public void RegisterBootstrapProvider<T>(string providerName, IDictionary<string, string> properties = null) where T : IBootstrapProvider 
```
<!--on the [`Orleans.Runtime.Configuration.GlobalConfiguration`](https://github.com/dotnet/orleans/blob/master/src/Orleans/Configuration/GlobalConfiguration.cs) class.-->
在[`Orleans.Runtime.Configuration.GlobalConfiguration`](https://github.com/dotnet/orleans/blob/master/src/Orleans/Configuration/GlobalConfiguration.cs)类中。

<!--These bootstrap providers are C# classes that implement the `Orleans.Providers.IBootstrapProvider` interface.-->
这个引导提供者是一个实现了`Orleans.Providers.IBootstrapProvider`接口的C#类。

<!--When each silo starts up, the Orleans runtime will instantiate each of the listed app bootstrap classes, and then call their Init method in an appropriate runtime execution context that allows those classes to act as a client and send messages to grains.-->
当每个silo启动的时候，Orleans运行时会实例化每一个列出的引导类，之后在核实的运行时执行上下文中调用他们的Init方法，这样这些类可以作为一个客户端并且发送消息给grain。

``` csharp
Task Init(
    string name, 
    IProviderRuntime providerRuntime, 
    IProviderConfiguration config)
```

<!--Any Exceptions that are thrown from an Init method of a bootstrap provider will be reported by the Orleans runtime in the silo log, then the silo startup will be halted. -->
任何一个引导提供者的Init方法抛出的异常都会被Orleans运行时记录在silo的log中，然后silo的启动会被暂停。

<!--This fail-fast approach is the standard way that Orleans handles silo start-up issues, and is intended to allow any problems with silo configuration and/or bootstrap logic to be easily detected during testing phases rather than being silently ignored and causing unexpected problems later in the silo lifecycle.-->
这种快速失败的方法是Orleans处理silo启动问题的标准方法，并且为了任何silo的配置和/或引导逻辑的问题在测试期间容易被检测到而不是默默忽略并且之后在silo生命周期中引起不可预料的问题。