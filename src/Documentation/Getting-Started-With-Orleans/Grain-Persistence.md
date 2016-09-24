---
layout: page
title: Grain Persistence
---

## grain持久化的目标
<!--## Grain Persistence Goals-->

<!--1. Allow different grain types to use different types of storage providers (e.g., one uses Azure table, and one uses an ADO.NET one) or the same type of storage provider but with different configurations (e.g., both use Azure table, but one uses storage account #1 and one uses storage account #2)-->
<!--2. Allow configuration of a storage provider instance to be swapped (e.g., Dev-Test-Prod) with just config file changes, and no code changes required.-->
<!--3. Provide a framework to allow additional storage providers to be written later, either by the Orleans team or others.-->
<!--4. Provide a minimal set of production-grade storage providers-->
<!--5. Storage providers have complete control over how they store grain state data in persistent backing store. Corollary: Orleans is not providing a comprehensive ORM storage solution, but allows custom storage providers to support specific ORM requirements as and when required.-->
1. 允许不同的grain类型使用不同的存储（例如，一个使用Azure table，一个使用ADO.NET的 Azure）或者同样类型的存储但是使用不同的配置（例如，都使用Azure table但是一个使用存储帐号＃1，一个使用存储帐号＃2）
2. 允许仅仅改变配置文件而不改变代码来实现一个存储提供者的配置转换（例如，开发－测试－生产之间转换）。
3. 提供一个框架来允许添加之后编写的额外的存储提供者，不管是Orleans团队编写的还是其他人编写的。
4. 提供一小部分生产级别的存储提供者。
5. 存储提供者对于如何在持久化后段存储存储grain的状态有完全的控制权。结论是：Orleans不提供全面的ORM解决方案，但是当有需要 时允许定制存储提供者来支持特定的ORM需求。

## grain持久化API
<!--## Grain Persistence API-->

<!--Grain types can be declared in one of two ways:-->
grain类型可以用一下其中一种方式声明：

<!--* Extend `Grain` if they do not have any persistent state, or if they will handle all persistent state themselves, or-->
<!--* Extend `Grain<T>` if they have some persistent state that they want the Orleans runtime to handle.-->
<!--Stated another way, by extending `Grain<T>` a grain type is automatically opted-in to the Orleans system managed persistence framework.-->
* 扩展`Grain`，如果他们没有任何持久化的状态或者他们自己能够处理所有持久化的状态，或者
* 扩展`Grain<T>`，如果他们有想要Orleans运行处理的持久化的状态。
换句话说，使用扩展`Grain<T>`的方式声明grain类型就是自动选择了Orleans系统管理的持久化框架。

<!--For the remainder of this section, we will only be considering Option #2 / `Grain<T>` because Option #1 grains will continue to run as now without any behavior changes.-->
这节其余的部分，我们只考虑第二种情况`Grain<T>`，因为第一种情况grain会继续运行不会有任何的行为变化。

## grain状态存储
<!--## Grain State Stores-->

<!--Grain classes that inherit from `Grain<T>` (where `T` is an application-specific state data type derived from `GrainState`) will have their state loaded automatically from a specified storage.-->
继承自`Grain<T>`（T是一个派生自`GrainState`的应用相关的状态数据）的grain类将会村特定的存储中自动地加载他们的状态。

<!--Grains will be marked with a `[StorageProvider]` attribute that specifies a named instance of a storage provider to use for reading / writing the state data for this grain.-->
grain将会被一个指定了存储提供者命名实例的`[StorageProvider]`特性所标记，用来为grain读取／写入状态数据。

``` csharp
[StorageProvider(ProviderName="store1")]
public class MyGrain<MyGrainState> ...
{
  ...
}
```

<!--The Orleans Provider Manager framework provides a mechanism to specify & register different storage providers and storage options in the silo config file.-->
Orleans提供者管理框架提供了一个指定&注册不同存储提供者的机制并且把选项存储silo的配置文件中。

```xml
<OrleansConfiguration xmlns="urn:orleans">
    <Globals>
    <StorageProviders>
        <Provider Type="Orleans.Storage.MemoryStorage" Name="DevStore" />
        <Provider Type="Orleans.Storage.AzureTableStorage" Name="store1"
            DataConnectionString="DefaultEndpointsProtocol=https;AccountName=data1;AccountKey=SOMETHING1" />
        <Provider Type="Orleans.Storage.AzureBlobStorage" Name="store2"
            DataConnectionString="DefaultEndpointsProtocol=https;AccountName=data2;AccountKey=SOMETHING2"  />
    </StorageProviders>
```

## 配置存储提供者
<!--## Configuring Storage Providers-->

### AzureTableStorage
<!--### AzureTableStorage-->

```xml
<Provider Type="Orleans.Storage.AzureTableStorage" Name="TableStore"
    DataConnectionString="UseDevelopmentStorage=true" />
```

<!--The following attributes can be added to the `<Provider />` element to configure the provider:-->
下面的特性可以被添加到`<Provider />`元素中来配置提供者：

<!--* __`DataConnectionString="..."`__ (mandatory) - The Azure storage connection string to use-->
<!--* __`TableName="OrleansGrainState"`__ (optional) - The table name to use in table storage, defaults to `OrleansGrainState`-->
<!--* __`DeleteStateOnClear="false"`__ (optional) - If true, the record will be deleted when grain state is cleared, otherwise an null record will be written, defaults to `false`-->
<!--* __`UseJsonFormat="false"`__ (optional) - If true, the json serializer will be used, otherwise the Orleans binary serializer will be used, defaults to `false`-->
<!--* __`UseFullAssemblyNames="false"`__ (optional) - (if `UseJsonFormat="true"`) Serializes types with full assembly names (true) or simple (false), defaults to `false`-->
<!--* __`IndentJSON="false"`__ (optional) - (if `UseJsonFormat="true"`) Indents the serialized json, defaults to `false`-->
* __`DataConnectionString="..."`__ （必选） － Azure storage的连接字符串
* __`TableName="OrleansGrainState"`__ （可选） － 表存储用的表名，默认是`OrleansGrainState`
* __`DeleteStateOnClear="false"`__ （可选） － 如果是true，在清除的时候记录会被删除，否则会写入一条null数据，默认是`false`
* __`UseJsonFormat="false"`__ （可选） － 如果是true，将使用json序列化，否则将会使用Orleans二进制序列化，默人是`false`
* __`UseFullAssemblyNames="false"`__ （可选） － （如果`UseJsonFormat="true"`） 序列化的类型带有完整的程序集名字（true）或者简单的名字（false）, 默认是`false`
* __`IndentJSON="false"`__ （可选） － （如果`UseJsonFormat="true"`） 缩进序列化后的json，默人是`false`

<!--> __Note:__ state should not exceed 64KB, a limit imposed by Table Storage.-->
> __注意：__ 状态不要超出64KB，Azure Table Storage的强制限制。

### AzureBlobStorage
<!--### AzureBlobStorage-->

```xml
<Provider Type="Orleans.Storage.AzureTableStorage" Name="BlobStore"
    DataConnectionString="UseDevelopmentStorage=true" />
```

<!--The following attributes can be added to the `<Provider />` element to configure the provider:-->
下面的特性可以被添加到`<Provider />`元素中来配置提供者：

<!--* __`DataConnectionString="..."`__ (mandatory) - The Azure storage connection string to use-->
<!--* __`ContainerName="grainstate"`__ (optional) - The blob storage container to use, defaults to `grainstate`-->
<!--* __`UseFullAssemblyNames="false"`__ (optional) - Serializes types with full assembly names (true) or simple (false), defaults to `false`-->
<!--* __`IndentJSON="false"`__ (optional) - Indents the serialized json, defaults to `false`-->
* __`DataConnectionString="..."`__ (必选) - Azure storage的连接字符串
* __`ContainerName="grainstate"`__ (可选) - 使用的blob storage container，默认是`grainstate`
* __`UseFullAssemblyNames="false"`__ (可选) - 序列化的类型带有完整的程序集名字（true）或者简单的名字（false）, 默认是`false`
* __`IndentJSON="false"`__ (可选) - 序列化后的json包含缩进，默认是`false`

### DynamoDBStorageProvider

```xml
<Provider Type="Orleans.Storage.DynamoDBStorageProvider" Name="DDBStore"
    DataConnectionString="Service=us-wes-1;AccessKey=MY_ACCESS_KEY;SecretKey=MY_SECRET_KEY;" />
```

* __`DataConnectionString="..."`__ (必选) - DynamoDB使用的连接字符串。 你可以设置`Service`,`AccessKey`, `SecretKey`, `ReadCapacityUnits`和`WriteCapacityUnits`。
* __`TableName="OrleansGrainState"`__ (可选) - 表存储用的表名，默认是`OrleansGrainState`
* __`DeleteStateOnClear="false"`__ (可选) - 如果是true，在清除的时候记录会被删除，否则会写入一条null数据，默认是`false`
* __`UseJsonFormat="false"`__ (可选) - 如果是true，将使用json序列化，否则将会使用Orleans二进制序列化，默人是`false`
* __`UseFullAssemblyNames="false"`__ (可选) - (如果 `UseJsonFormat="true"`) 序列化的类型带有完整的程序集名字（true）或者简单的名字（false）, 默认是`false`
* __`IndentJSON="false"`__ (可选) - (如果`UseJsonFormat="true"`) 缩进序列化后的json，默人是`false`

<!--
* __`DataConnectionString="..."`__ (mandatory) - The DynamoDB storage connection string to use. You can set `Service`,`AccessKey`, `SecretKey`, `ReadCapacityUnits` and `WriteCapacityUnits` in it.
* __`TableName="OrleansGrainState"`__ (optional) - The table name to use in table storage, defaults to `OrleansGrainState`
* __`DeleteStateOnClear="false"`__ (optional) - If true, the record will be deleted when grain state is cleared, otherwise an null record will be written, defaults to `false`
* __`UseJsonFormat="false"`__ (optional) - If true, the json serializer will be used, otherwise the Orleans binary serializer will be used, defaults to `false`
* __`UseFullAssemblyNames="false"`__ (optional) - (if `UseJsonFormat="true"`) Serializes types with full assembly names (true) or simple (false), defaults to `false`
* __`IndentJSON="false"`__ (optional) - (if `UseJsonFormat="true"`) Indents the serialized json, defaults to `false`
-->



### ADO.NET Storage Provider (SQL Storage Provider)

Note that to use the it is necessary to deploy the database script to the database. It can be found in the The scripts are located in the Nuget library, similar to `\packages\Microsoft.Orleans.OrleansSqlUtils.n.n.n\lib\net<version>\SQLServer\` depending on version and database vendor.



```xml
<Provider Type="Orleans.SqlUtils.StorageProvider.SqlStorageProvider" Name="SqlStore" DataConnectionString="Data Source = (localdb)\MSSQLLocalDB; Database = OrleansTestStorage; Integrated Security = True; Asynchronous Processing = True; Max Pool Size = 200;" />


```

* __`DataConnectionString="..."`__ (mandatory) - The SQL connection string to use.






* __`UseJsonFormat="false"`__ (optional) - If true, the json serializer will be used, otherwise the Orleans binary serializer will be used, defaults to `false`.
* __`UseXmlFormat="false"`__ (optional) - If true, the .NET XML serializer will be used, otherwise the Orleans binary serializer will be used, defaults to `false`.
* __`UseBinaryFormat="false"`__ (the default) - If true, the Orleans binary data format will be used.

Note that pool size of 200 is quite a low figure.

The following is an example of programmatic configuration.

``` csharp
//props["RootDirectory"] = @".\Samples.FileStorage";
//config.Globals.RegisterStorageProvider<Samples.StorageProviders.OrleansFileStorage>("TestStore", props);
props[Orleans.Storage.AdoNetStorageProvider.DataConnectionStringPropertyName] = @"Data Source = (localdb)\MSSQLLocalDB; Database = OrleansTestStorage; Integrated Security = True; Asynchronous Processing = True; Max Pool Size = 200;";
props[Orleans.Storage.AdoNetStorageProvider.UseJsonFormatPropertyName] = "true"; //Binary, the default option, is more efficient. This is for illustrative purposes.
config.Globals.RegisterStorageProvider<Orleans.Storage.AdoNetStorageProvider>("TestStore", props);
```

A quick way to test this is to (see in the aforementioned the few commented lines)

1. Open `\Samples\StorageProviders`.
2. On the package manager console, run: `Install-Package Microsoft.Orleans.OrleansSqlUtils -project Test.Client`.
3. Update all the Orleans packages in the solution, run: `Get-Package | where Id -like 'Microsoft.Orleans.*' | foreach { update-package $_.Id }` (this is a precaution to make sure the packages are on same version).
4. Go to `OrleansHostWrapper.cs` and to the following

The ADO.NET persistence has functionality to version data and define arbitrary (de)serializers with arbitrary application rules and streaming, but currently
there is no method to expose them to application code. More information in [ADO.NET Persistence Rationale](#ADONETPersistenceRationale).


### MemoryStorage

```xml
<Provider Type="Orleans.Storage.MemoryStorage" Name="MemoryStorage"  />
```
<!--> __Note:__ This provider persists state to volatile memory which is erased at silo shut down. Use only for testing.-->
> __注意：__ 这个提供者将状态持久化到独立内存中，在silo关闭的时候将被清除。只用来测试用。

<!--* __`NumStorageGrains="10"`__ (optional) - The number of grains to use to store the state, defaults to `10`-->
* __`NumStorageGrains="10"`__ (可选) - 用来存储状态的grain的个数，默认是`10`

### ShardedStorageProvider

```xml
<Provider Type="Orleans.Storage.ShardedStorageProvider" Name="ShardedStorage">
    <Provider />
    <Provider />
    <Provider />
</Provider>
```
<!--Simple storage provider for writing grain state data shared across a number of other storage providers.-->
一个简单的写入分片到若干存储提供者的数据的简单存储提供者。
<!--A consistent hash function (default is Jenkins Hash) is used to decide which-->
<!--shard (in the order they are defined in the config file) is responsible for storing-->
<!--state data for a specified grain, then the Read / Write / Clear request-->
<!--is bridged over to the appropriate underlying provider for execution.-->
使用一个一致性哈希函数（默认是Jenkins Hash）来决定哪个分片对指定的grain的状态数据存储进行相应，然后读取/写入/清除请求路由到适当的潜在的提供者来执行。

## 关于存储提供者特别说明的
<!--## Notes on Storage Providers-->

<!--If there is no `[StorageProvider]` attribute specified for a `Grain<T>` grain class, then a provider named `Default` will be searched for instead.-->
如果没有给一个`Grain<T>`grain类指定`[StorageProvider]`特性，将会搜索使用名为`Default`的提供者。
<!--If not found then this is treated as a missing storage provider.-->
如果没有找到，就当作缺少存储提供者。

<!--If there is only one provider in the silo config file, it will be considered to be the `Default` provider for this silo.-->
如果silo配置文件中只有一个提供者，它会被当作silo的`Default`提供者。

<!--A grain that uses a storage provider which is not present and defined in the silo configuration when the silo loads will fail to load, but the rest of the grains in that silo can still load and run.-->
一个使用不存在或者没有在silo配置中定义的提供者的grain将会在加载的时候失败，但是其他的grain还是会加载并且运行。
<!--Any later calls to that grain type will fail with an `Orleans.Storage.BadProviderConfigException` error specifying that the grain type is not loaded.-->
之后任何对这个grain的调用都会失败得到一个表示那个grain类型没有被加载的`Orleans.Storage.BadProviderConfigException`错误。

<!--The storage provider instance to use for a given grain type is determined by the combination of the storage provider name defined in the `[StorageProvider]` attribute on that grain type, plus the provider type and configuration options for that provider defined in the silo config.-->
一个grain类型最终使用的存储提供者是通过`[StorageProvider]`特性中的提供者名称加上提供者的类型和silo配置中定义的提供者配置选项决定。

<!--Different grain types can use different configured storage providers, even if both are the same type: for example, two different Azure table storage provider instances, connected to different Azure storage accounts (see config file example above).-->
不同的grain类型使用不同配置的存储提供者，即使是同一个类型：例如，两个不同的Azure table存储提供者实例，连接到不同的Azure storge account（参考上面的配置文件例子）。

<!--All configuration details for storage providers is defined statically in the silo configuration that is read at silo startup.-->
所有的存储提供者的配置细节静态地卸载silo配置中在silo启动时读取。
<!--There are _no_ mechanisms provided at this time to dynamically update or change the list of storage providers used by a silo.-->
现在 _没有_ 动态地更新或者改变silo的使用的存储提供者的机制。
<!--However, this is a prioritization / workload constraint rather than a fundamental design constraint.-->
然而，这是一个优先级/工作量限制，而不是一个基本的设计约束。

## 状态存储API
<!--## State Storage APIs-->

<!--There are two main parts to the grain state / persistence APIs: Grain-to-Runtime and Runtime-to-Storage-Provider.-->
grain状态/持久化API有两个部分：Grain到与形时和运行时到存储提供者。

## grain状态存储API
<!--## Grain State Storage API-->

<!--The grain state storage functionality in the Orleans Runtime will provide read and write operations to automatically populate / save the `GrainState` data object for that grain.-->
Orleans运行时的状态存储功能提供读取和写入操作来自动的填充/保存grain的`GrainState`数据对象。
<!--Under the covers, these functions will be connected (within the code generated by Orleans client-gen tool) through to the appropriate persistence provider configured for that grain.-->
这些功能将会通过为grain配置的恰当的持久化提供者来默默地完成。

## grain状态读写函数
<!--## Grain State Read / Write Functions-->

<!--Grain state will automatically be read when the grain is activated, but grains are responsible for explicitly triggering the write for any changed grain state as and when necessary.-->
当grain激活的时候grain状态会自动被读取，但是当需要的时候grain需要显示地触发任何grain状态的写操作。
<!--See the [Failure Modes](#FailureModes) section below for details of error handling mechanisms.-->
阅读[失败模式](#FailureModes)一节，了解更多错误处理机制的细节。

<!--`GrainState` will be read automatically (using the equivalent of `base.ReadStateAsync()`) _before_ the `OnActivateAsync()` method is called for that activation.-->
激活的时候，`GrainState`(等同于`base.ReadStateAsync()`)在`OnActivateAsync()`方法被调用之前会被调用。
<!--`GrainState` will not be refreshed before any method calls to that grain, unless the grain was activated for this call.-->
`GrainState`在grain的任何方法被调用之前不会更新，除非这次调用的时候这个grain已经激活了。

<!--During any grain method call, a grain can request the Orleans runtime to write the current grain state data for that activation to the designated storage provider by calling `base.WriteStateAsync()`.-->
在grain的任何方法被调用的时候，一个grain可以要求Orleans运行时把那个激活的当前的状态数据通过调用`base.WriteStateAsync()`写入到指定的存储提供者中。
<!--The grain is responsible for explicitly performing write operations when they make significant updates to their state data.-->
当grain状态数据发生显著的更新的时候，grain负责显示地执行写入操作。
<!--Most commonly, the grain method will return the `base.WriteStateAsync()` `Task` as the final result `Task` returned from that grain method, but it is not required to follow this pattern.-->
大多说情况下，grain方法返回`base.WriteStateAsync()` `Task`作为最终结果`Task`返回，但是它并不要求遵循此模式。
<!--The runtime will not automatically update stored grain state after any grain methods.-->
运行时在任何grain方法后不会自动更新存储的grain。

<!--During any grain method or timer callback handler in the grain, the grain can request the Orleans runtime to re-read the current grain state data for that activation from the designated storage provider by calling `base.ReadStateAsync()`.-->
在任何grain方法或者timer回掉函数中，grain可以要求Orleans运行时通过调用`base.ReadStateAsync()`从指定的存储提供者重读当前的grain激活的状态数据。
<!--This will completely overwrite any current state data currently stored in the grain state object with the latest values read from persistent store.-->
这将会使用从持久化存储中读出的最新值完全重写当前grain状态对象中存储的状态数据。

<!--An opaque provider-specific `Etag` value (`string`) _may_ be set by a storage provider as part of the grain state metadata populated when state was read.-->
当状态读取时存储提供者 _可能_ 将一个不透明的提供者指定的`Etag`值(`string`)作为grain状态数据的一部分填充进去。
<!--Some providers may choose to leave this as `null` if they do not use `Etag`s.-->
一些不适用`Etag`的提供者会选择将这个值留作`null`。

<!--Conceptually, the Orleans Runtime will take a deep copy of the grain state data object for its own use during any write operations. Under the covers, the runtime _may_ use optimization rules and heuristics to avoid performing some or all of the deep copy in some circumstances, provided that the expected logical isolation semantics are preserved.-->
概念上，Orleans运行在任何写操作的时候时会对grain状态数据对象进行深拷贝为自己使用。表面之下，运行时在一些环境中 _可能_ 使用优化策略和启发式方法来避免进行一些或者全部的深拷贝，通过这个能实现期望的逻辑隔离语义。

## Sample Code for Grain State Read / Write Operations

Grains must extend the `Grain<T>` class in order to participate in the Orleans grain state persistence mechanisms.
The `T` in the above definition will be replaced by an application-specific grain state class for this grain; see the example below.

The grain class should also be annotated with a `[StorageProvider]` attribute that tells the runtime which storage provider (instance) to use with grains of this type.

``` csharp
public interface MyGrainState : GrainState
{
  public int Field1 { get; set; }
  public string Field2 { get; set; }
}

[StorageProvider(ProviderName="store1")]
public class MyPersistenceGrain : Grain<MyGrainState>, IMyPersistenceGrain
{
  ...
}
```

## grain状态读取
<!--## Grain State Read-->

<!--The initial read of the grain state will occur automatically by the Orleans runtime before the grain’s `OnActivateAsync()` method is called; no application code is required to make this happen.-->
最初的grain状态读取将会有Orleans运行在grain的`OnActivateAsync()`方法调用前自动发生；不需要使用应用代码来触发。
<!--From that point forward, the grain’s state will be available through the `Grain<T>.State` property inside the grain class.-->
此后，可以通过grain类中的`Grain<T>.State`属性来读取grain的状态。

## grain状态写入
<!--## Grain State Write-->

<!--After making any appropriate changes to the grain’s in-memory state, the grain should call the `base.WriteStateAsync()` method to write the changes to the persistent store via the defined storage provider for this grain type.-->
在对grain的内存中的状态进行任何适当改动后，grain应该调用`base.WriteStateAsync()`方法通过已经grain类型已经定义的存储提供者将改变写入到持久化存储。
<!--This method is asynchronous and returns a `Task` that will typically be returned by the grain method as its own completion Task.-->
这个方法是异步的并且返回一个`Task`。这个`Task`通常作为grain方法它自己的完成Task返回。


``` csharp
public Task DoWrite(int val)
{
  State.Field1 = val;
  return base.WriteStateAsync();
}
```

## grain状态刷新
<!--## Grain State Refresh-->

<!--If a grain wishes to explicitly re-read the latest state for this grain from backing store, the grain should call the `base.ReadStateAsync()` method.-->
如果一个grain希望显示地从后存储中重新读取这个grain的最新状态，这个grain应该调用`base.ReadStateAsync()`方法。
<!--This will reload the grain state from persistent store, via the defined storage provider for this grain type, and any previous in-memory copy of the grain state will be overwritten and replaced when the `ReadStateAsync()` `Task` completes.-->
这将通过已为这个grain定义的存储提供者从持久化存储中重新加载grain状态，并且当`ReadStateAsync()` `Task`完成时。任何之前内存中的grain状态的拷贝会被重写和替换，

``` csharp
public async Task<int> DoRead()
{
  await base.ReadStateAsync();
  return State.Field1;
}
```

## grain持久化操作的失败模式
<!--## Failure Modes for Grain State Persistence Operations <a name="FailureModes"></a>-->

### grain状态读取操作的失败模式
<!--### Failure Modes for Grain State Read Operations-->

<!--Failures returned by the storage provider during the initial read of state data for that particular grain will result in the activate operation for that grain to be failed; in this case, there will _not_ be any call to that grain’s `OnActivateAsync()` life cycle callback method.-->
存储提供者返回的在初始化读取特定grain的状态数据时的失败将会导致那个grain的激活操作失败；这样的话，那个grain的`OnActivateAsync()`生命周期的回掉方法将 _不会_ 被调用。
<!--The original request to that grain which caused the activation will be faulted back to the caller the same way as any other failure during grain activation.-->
引起那个grain激活的原始请求也会失败，想其他grain激活期间的失败一样返回给调用者。
<!--Failures encountered by the storage provider to read state data for a particular grain will result in the `ReadStateAsync()` `Task` to be faulted.-->
特定的grain的存储提供者读取状态数据遇到的失败将会导致`ReadStateAsync()` `Task`失败。
<!--The grain can choose to handle or ignore that faulted `Task`, just like any other `Task` in Orleans.-->
grain可以选择处理或者忽略这个失败`Task`，就像Orleans中其他的`Task`一样。

<!--Any attempt to send a message to a grain which failed to load at silo startup time due to a missing / bad storage provider config will return the permanent error `Orleans.BadProviderConfigException`.-->
任何向silo启动时因缺少/错误的存贮提供者配置而不能加载的grain发送消息的尝试都将返回一个永久错误`Orleans.BadProviderConfigException`。

### grain状态写入操作的失败模式
<!--### Failure Modes for Grain State Write Operations-->

<!--Failures encountered by the storage provider to write state data for a particular grain will result in the `WriteStateAsync()` `Task` to be faulted.-->
特定的grain的存储提供者写入状态数据遇到的失败会导致`WriteStateAsync()` `Task`失败。
<!--Usually, this will mean the grain call will be faulted back to the client caller provided the `WriteStateAsync()` `Task` is correctly chained in to the final return `Task` for this grain method.-->
通常，这代表grain调用将会传递给提供了`WriteStateAsync()` `Task`的客户端调用者，并且作为grain方法最终返回的`Task`.
<!--However, it will be possible for certain advanced scenarios to write grain code to specifically handle such write errors, just like they can handle any other faulted `Task`.-->
然而，也可能在确定的高级场景中来编写grain代码特别处理这样的写入错误，就像能处理其他失败的`Task`一样。

<!--Grains that execute error-handling / recovery code _must_ catch exceptions / faulted `WriteStateAsync()` `Task`s and not re-throw to signify that they have successfully handled the write error.-->
执行错误处理/回复的grain代码 _必须_ 捕获`WriteStateAsync()` `Task`的异常/失败比去年给不重新抛出，表明已经成功处理了错误。

## 存储提供者框架
<!--## Storage Provider Framework-->

<!--There is a service provider API for writing additional persistence providers – `IStorageProvider`.-->
有一个服务提供者API来写额外的持久化存储提供者 – `IStorageProvider`。

<!--The Persistence Provider API covers read and write operations for GrainState data.-->
持久化提供者API包括对grain状态数据的读取和写入操作。

``` csharp
public interface IStorageProvider
{
  Logger Log { get; }
  Task Init();
  Task Close();

  Task ReadStateAsync(string grainType, GrainId grainId, GrainState grainState);
  Task WriteStateAsync(string grainType, GrainId grainId, GrainState grainState);
}
```

## 存储提供者语义
<!--## Storage Provider Semantics-->

<!--Any attempt to perform a write operation when the storage provider detects an `Etag` constraint violation _should_ cause the write `Task` to be faulted with transient error `Orleans.InconsistentStateException` and wrapping the underlying storage exception.-->
当进行任何写操作的时候存储提供者检测到`Etag`违反约束，就 _应该_ 以一个瞬时错误 `Orleans.InconsistentStateException`引发写`Task`失败并且包装底层的存储异常。

``` csharp
public class InconsistentStateException : AggregateException
{
  /// <summary>The Etag value currently held in persistent storage.</summary>
  public string StoredEtag { get; private set; }
  /// <summary>The Etag value currently held in memory, and attempting to be updated.</summary>
  public string CurrentEtag { get; private set; }

  public InconsistentStateException(
    string errorMsg,
    string storedEtag,
    string currentEtag,
    Exception storageException
    ) : base(errorMsg, storageException)
  {
    this.StoredEtag = storedEtag;
    this.CurrentEtag = currentEtag;
  }

  public InconsistentStateException(string storedEtag, string currentEtag, Exception storageException)
    : this(storageException.Message, storedEtag, currentEtag, storageException)
  { }
}
```


<!--Any other failure conditions from a write operation _should_ cause the write `Task` to be broken with an exception containing the underlying storage exception.-->
来自写操作的任何其他错误情况 _应该_ 引发写`Task`以一个包含了底层存储异常信息的异常来终止。

## 数据映射
<!--## Data Mapping-->

<!--Individual storage providers should decide how best to store grain state – blob (various formats / serialized forms) or column-per-field are obvious choices.-->
独立的存储提供者应该决定如何存储grain状态 - blob（多种格式/序列化的形式）或者每列一个字段是显而易见的选择。

<!--The basic storage provider for Azure Table encodes state data fields into a single table column using Orleans binary serialization.-->
基本的Azure Table存储提供者将状态数据字段通过Orleans二进制序列化编码成单个表列。


## ADO.NET Persistence Rationale <a name="ADONETPersistenceRationale"></a>

The principles for ADO.NET backed persistence storage are:

1. Keep business critical data safe an accessible while data, the format of data and code evolve.
2. Take advantenge of vendor and storage specific functionality.

In practice this means adhering to [ADO.NET implementation goals](../Runtime-Implementation-Details/Relational-Storage.md)
and some added implementation logic in ADO.NET specific storage provider that allow evolving the shape of the data in the storage.

In addition to the usual storage provider capabilities, the ADO.NET provider has built-in capability to

1. Change storage data format from one format to another format (e.g. from JSON to binary) when roundtripping state.
2. Shape the type to be saved or read from the storage in arbitrary ways. This helps to evolve the version state.
3. Stream data out of the database.

Both `1.` and `2.` can be applied on arbitrary decision parameters, such as *grain ID*, *grain type*, *payload data*.

This happen so that one chooses a format, e.g. [Simple Binary Encoding (SBE)](https://github.com/real-logic/simple-binary-encoding) and implements
(IStorageDeserializer)[https://github.com/dotnet/orleans/blob/master/src/OrleansSQLUtils/Storage/Provider/IStorageDeserializer.cs] and [IStorageSerializer](https://github.com/dotnet/orleans/blob/master/src/OrleansSQLUtils/Storage/Provider/IStorageSerializer.cs).
The built-in (de)serializers have been built using this method. The [OrleansStorageDefault<format>(De)Serializer](https://github.com/dotnet/orleans/tree/master/src/OrleansSQLUtils/Storage/Provider) can be used as examples
on how to implement other formats.

When the (de)serializers have been implemented, they need to ba added to the `StorageSerializationPicker` property in [AdoNetStorageProvider](https://github.com/dotnet/orleans/blob/master/src/OrleansSQLUtils/Storage/Provider/AdoNetStorageProvider.cs).
This is an implementation of [IStorageSerializationPicker](https://github.com/dotnet/orleans/blob/master/src/OrleansSQLUtils/Storage/Provider/IStorageSerializationPicker.cs). By default
[StorageSerializationPicker](https://github.com/dotnet/orleans/blob/master/src/OrleansSQLUtils/Storage/Provider/StorageSerializationPicker.cs) will be used. And example of changing data storage format
or using (de)serializers can be seen at [RelationalStorageTests]https://github.com/dotnet/orleans/blob/master/test/TesterInternal/StorageTests/Relational/RelationalStorageTests.cs).

Currently there is no method to expose this to Orleans application consumption as there is no method to access the framework created [AdoNetStorageProvider](https://github.com/dotnet/orleans/blob/master/src/OrleansSQLUtils/Storage/Provider/AdoNetStorageProvider.cs) instance.
