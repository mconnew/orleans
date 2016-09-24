---
layout: page
title: Troubleshooting Deployments
---

# 部署故障排除

<!--This page gives some general guidelines for troubleshooting any issues that occur while deploying to Azure Cloud Services. -->
这个页面给出一些在部署到Azure云服务的时候发生的问题的故障检测指导。
<!--These are very common issues to watch out for. Be sure to check the logs for more information.-->
这些都是要注意的很常见的问题。无比检查log获取更多信息。

## 遇到SiloUnavailableException
<!--## Getting a SiloUnavailableException-->

<!--First check to make sure that you are actually starting the silos before attempting to initialize the client. Sometimes the -->
<!--silos take a long time to start so it can be beneficial to try to initialize the client multiple times. If it still throws an -->
<!--exception, then there might be another issue with the silos.-->
首先检查确保你在初始化客户端之前一定启动了silo。有时silo会花很长时间启动所以有时候最好多试几次初始化客户端。如果还是出错，可能是silo有什么问题。

<!--Check the silo configuration and make sure that the silos are starting up properly.-->
检查silo的配置并且确保silo启动了。

## 常见的连接字串问题
<!--## Common Connection String Issues-->
<!---	Using the local connection string when deploying to Azure – the website will fail to connect-->
<!---	Using different connection strings for the silos and the front end (web and worker roles) – the website will fail to -->
<!--initialize the client because it cannot connect to the silos-->
- 部署到Azure的时候使用本地的连接字串 - 网页会连接失败。
- silo服务端和前段（web和woker角色）使用了不同的连接字串 - 网页将会初始化失败因为无法连接到silo

<!--The connection string configuration can be checked in the Azure Portal. The logs may not display properly if the connection -->
<!--strings are not set up correctly.-->
连接字串配置可以在Azure Portal检查。如果连接字串没有配置正确log文件可能不会显示。

## 不正确地修改配置文件
<!--## Modifying the Configuration Files Improperly-->

<!--Make sure that the proper endpoints are configured in the ServiceDefinition.csdef file or else the deployment will not work.-->
<!--It will give errors saying that it cannot get the endpoint information.-->
确保ServiceDefinition.csdef文件中配置了正确的终结点，否则部署不能正确工作。会报出不能获取终结点信息的错误。

## 缺少log
<!--## Missing Logs-->
<!--Make sure that the connection strings are set up properly.-->
确保已经设置了连接字串。

<!--It is likely that the Web.config file in the web role or the app.config file in the worker role were modified improperly. -->
可能是Web.config文件中的web role或者app.config文件中的worker role没有正确配置。
<!--Incorrect versions in these files can cause issues with the deployment. Be careful when dealing with updates.-->
这些文件中的版本不正确可能导致部署问题。小心处理升级为你。

## 版本问题
<!--## Version Issues-->
<!--Make sure that the same version of Orleans is used in every project in the solution. Not doing this can lead to the worker-->
<!--role recycling. Check the logs for more information. Visual Studio provides some silo startup error messages in the deployment history.-->
确保解决方案的每个工程中使用了相同版本的Orleans。不这样做会导致woker role回收。检查log获取更多信息。在部署历史中Visual Studio 提供一些silo启动的错误消息。

## Role Keeps Recycling
<!--## Role Keeps Recycling-->
<!--- Check that all the appropriate Orleans assemblies are in the solution and have Copy Local set to True.-->
<!--- Check the logs to see if there is an unhandled exception while initializing.-->
<!--- Make sure that the connection strings are correct.-->
<!--- Check the Azure Cloud Services troubleshooting pages for more information.-->
- 检查确保所有恰当的Orleans程序集在解决方案中并且设置Copy Local为true。
- 检查log看看是否有初始化过程中没有处理的异常。
- 确保链接字串正确。
- 检查 Azure Cloud Services 故障排除页面获取更多信息。

## 如何检查log
<!--## How to Check Logs-->
<!--- Use the cloud explorer in Visual Studio to navigate to the appropriate storage table or blob in the storage account. The WADLogsTable is a good starting point for looking at the logs.-->
<!--- You might only be logging errors. If you want informational logs as well, you will need to modify the configuration to set the logging severity level. -->
- 使用Visual Studio中的cloud explorer定位到合适的存储表或者存储账户中的blob。从WADLogsTable开始寻找log不错。

<!--Programmatic configuration:-->
可编程配置：
<!--- When creating a `ClusterConfiguration` object, set `config.Defaults.DefaultTraceLevel = Severity.Info`.-->
<!--- When creating a `ClientConfiguration` object, set `config.DefaultTraceLevel = Severity.Info`.-->
- 当创建一个`ClusterConfiguration`对象，设置`config.Defaults.DefaultTraceLevel = Severity.Info`.
- 当创建一个`ClientConfiguration`对象，设置`config.DefaultTraceLevel = Severity.Info`.

<!--Declarative configuration:-->
声明式配置：
<!--- Add `<Tracing DefaultTraceLevel="Info" />` to the `OrleansConfiguration.xml` and/or the `ClientConfiguration.xml` files.-->
添加`<Tracing DefaultTraceLevel="Info" />`到`OrleansConfiguration.xml`和/或`ClientConfiguration.xml`文件。

<!--In the `diagnostics.wadcfgx` file for the web and worker roles, make sure to set the `scheduledTransferLogLevelFilter` attribute in the `Logs` element to `Information`, as this is an additional layer of trace filtering that defines which traces are sent to the `WADLogsTable` in Azure Storage.-->
在web和worker role的`diagnostics.wadcfgx`文件中，确保`Logs`中的`scheduledTransferLogLevelFilter`属性设置成`Information`,这是一个用来定义哪些跟踪信息发送到Azure存储的`WADLogsTable`的跟踪过滤的额外层。

<!--You can find more information about this in the [Orleans Configuration Guide] (Orleans-Configuration-Guide/).-->

你可以在 [Orleans配置向导](../Orleans-Configuration-Guide/index.md) 中找到更多信息