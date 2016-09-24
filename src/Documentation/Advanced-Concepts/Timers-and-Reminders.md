---
layout: page
title: Timers and Reminders
---

# 定时器和提醒器
<!-- # Timers and Reminders -->

<!--The Orleans runtime provides two mechanisms, called timers and reminders, that enable the developer to specify periodic behavior for grains.-->
Orleans运行时提供两种机制来让开发者可以指定grain的一些周期性行为，叫做定时器和提醒器。

# 定时器
<!--# Timers-->

## 描述
<!--## Description-->
<!--**Timers** are used to create periodic grain behavior that isn't required to span multiple activations (instantiations of the grain). It is essentially identical to the standard .**NET System.Threading.Timer** class. In addition, it is subject to single threaded execution guarantees within the grain activation that it operates.-->
**定时器** 用来创建周期性的不需要跨越多个激活（grain的实例）的grain行为。它本质上是一种.NET标准。**NET System.Threading.Timer**类。此外，在grain激活中保证以单线程执行。

 <!--Each activation may have zero or more timers associated with it. The runtime executes each timer routine within the runtime context of the activation that it is associated with.-->
 每一个激活可能有0个或者多与它有关的定时器。每个定时器例程在与它相关的运行时上下文中执行。

## 用法
<!--## Usage-->
<!--To start a timer, use the **Grain.RegisterTimer** method, which returns an  **IDisposable** reference:-->
使用**Grain.RegisterTimer**方法启动一个定时器，返回**IDisposable**引用：

``` csharp
protected IDisposable RegisterTimer(Func<object, Task> asyncCallback, object state, TimeSpan dueTime, TimeSpan period)
```

<!--* asyncCallback is the function to be invoked when the timer ticks.-->
<!--* state is an object that will be passed to asyncCallback when the timer ticks.-->
<!--* dueTime specifies a quantity of time to wait before issuing the first timer tick.-->
<!--* period specifies the period of the timer.-->
* asyncCallback是定时器触发的时候调用的函数。
* state是定时器触发时传递给asyncCallback的一个对象。
* dueTime指定第一次触发之前的等待时间。
* period指定触发的周期。

 <!--Cancel the timer by disposing it.-->
 通过析构来取消一个定时器。

 <!--A timer will cease to trigger if the activation is deactivated or when a fault occurs and its silo crashes.-->
如果激活已经注销了或者当错误发生导致了它的silo崩溃，定时器将停止触发。

 <!--Important Considerations-->
 重要考量

<!--* When activation collection is enabled, the execution of a timer callback does not change the activation's state from idle to in use. This means that a timer cannot be used to postpone deactivation of otherwise idle activations.-->
当激活集合已经启用，一个定时器的回掉函数执行不会把激活的状态从闲置变为在使用。这意味着一个定时器不能用来阻止限制的激活注销。
<!--* The period passed to **Grain.RegisterTimer** is the amount of time that passes from the moment the Task returned by **asyncCallback** is resolved to the moment that the next invocation of **asyncCallback** should occur. This not only makes it impossible for successive calls to **asyncCallback** to overlap but also makes it so that the length of time **asyncCallback** takes to complete affects the frequency at which **asyncCallback** is invoked. This is an important deviation from the semantics of **System.Threading.Timer**.-->
传递给**Grain.RegisterTimer**的period是从**asyncCallback**返回的Task被求值得那一刻到下一个**asyncCallback**被调用经过的时间。这样不光使得对**asyncCallback**的成功调用不可能重叠并且完成**asyncCallback**花费的时间会影响**asyncCallback**调用的频率。
<!--* Each invocation of **asyncCallback** is delivered to an activation on a separate turn and will never run concurrently with other turns on the same activation. Note however, **asyncCallback** invocations are not delivered as messages and are thus not subject to message interleaving semantics. This means that invocations of **asyncCallback** should be considered to behave as if running on a reentrant grain with respect to other messages to that grain.-->
每一个**asyncCallback**的调用在单独的回合被传递给一个激活并且永远不会在同一个激活中与跟其他回合并发执行。然而请注意，**asyncCallback**调用不会被当作信息传递并且因此不受消息交错语义的影响。这表示在grain的消息消方面看，**asyncCallback**的调用应该被当作表现的像运行在一个可重入的grain。

## 提醒器
<!--# Reminders-->

## 描述
<!--## Description-->

<!--Reminders are similar to timers with a few important differences:-->
提醒器类似定时器，但是有一些重要的不同：

<!--* Reminders are persistent and will continue to trigger in all situations (including partial or full cluster restarts) unless explicitly cancelled.-->
<!--* Reminders are associated with a grain, not any specific activation.-->
<!--* If a grain has no activation associated with it and a reminder ticks, one will be created. e.g.: If an activation becomes idle and is deactivated, a reminder associated with the same grain will reactivate the grain when it ticks next.-->
<!--* Reminders are delivered by message and are subject to the same interleaving semantics as all other grain methods.-->
<!--* Reminders should not be used for high-frequency timers-- their period should be measured in minutes, hours, or days.-->
* 提醒器是持久化的并且在所有情况下（包括部分和全部的集群重启）将会持续触发除非主动取消。
* 提醒器跟一个grain相关，而不是任何特定的激活。
* 如果一个grain没有相关的激活并且一个提醒器出发了，一个激活将会被创建。例如：如果一个激活变成闲置的并且注销了，相同grain相关的一个额提醒器下一次触发的时候将会重新激活grain。

## 配置
<!--## Configuration-->
<!--Reminders, being persistent, rely upon storage to function. You must specify which storage backing to use before the reminder subsystem will function. The reminder functionality is controlled by the SystemStore element in the server-side configuration. It works with either Azure Table or SQL Server as the store.-->
提醒器的持久化依赖存储功能。你必须在提醒器子系统运转之前指定提醒器使用的后端存储。提醒器功能由服务端配置中的SystemStore元素控制。可以使用Azure Table和SQL server作为存储。

``` xml
<SystemStore SystemStoreType="AzureTable" /> OR
<SystemStore SystemStoreType="SqlServer" />
```

 <!--If you just want a placeholder implementation of reminders to work with without needing to set up an Azure account or SQL database, then adding this element to the configuration file (under 'Globals') will give you a development-only implementation of the reminder system:-->
 如果你仅仅是想使用一个占位的提醒器实现来工作不需要设置Azure账户或者SQL数据库，通过添加这个元素到配置文件中（在'Globals'下），能让你得到一个提醒器系统的开发时使用的实现。

``` xml
<ReminderService ReminderServiceType="ReminderTableGrain"/>
```

## 用法
<!--## Usage-->
<!--A grain that uses reminders must implement the **IRemindable.RecieveReminder** method.-->
一个使用提醒器的grain必须实现**IRemindable.RecieveReminder**方法。

``` csharp
Task IRemindable.ReceiveReminder(string reminderName, TickStatus status)
{
    Console.WriteLine("Thanks for reminding me-- I almost forgot!");
    return TaskDone.Done;
}
```

 <!--To start a reminder, use the **Grain.RegisterOrUpdateReminder** method, which returns an **IOrleansReminder** object:-->
 使用返回一个**IOrleansReminder**对象的**Grain.RegisterOrUpdateReminder**方法来启动一个提醒器。

``` csharp
protected Task<IOrleansReminder> RegisterOrUpdateReminder(string reminderName, TimeSpan dueTime, TimeSpan period)
```

<!--* reminderName is a string that must uniquely identify the reminder within the scope of the contextual grain.-->
<!--* dueTime specifies a quantity of time to wait before issuing the first timer tick.-->
<!--* period specifies the period of the timer.-->
* reminderName是一个在grain上下文范围内唯一标识提醒器的字符串。
* dueTime指定第一次触发之前等待的时间。
* period指定定时器的周期。

<!--Since reminders survive the lifetime of any single activation, they must be explicitly cancelled (as opposed to being disposed). You cancel a reminder by calling **Grain.UnregisterReminder**:-->
因为提醒器独立于任何单个激活的生存时间，所以他们必须显式地取消（而不是被析构）。你可以通过调用**Grain.UnregisterReminder**取消一个提醒器。

``` csharp
protected Task UnregisterReminder(IOrleansReminder reminder)
```

<!--reminder is the handle object returned by **Grain.RegisterOrUpdateReminder**.-->
reminder是**Grain.RegisterOrUpdateReminder**返回的处理对象。

<!-- Instances of **IOrleansReminder** aren't guaranteed to be valid beyond the lifespan of an activation. If you wish to identify a   reminder in a way that persists, use a string containing the reminder's name.-->
**IOrleansReminder**的实例不保证在激活的寿命之前有效。如果你想有效识别一个提醒器，使用一个包含提醒器名字的字符串。

<!-- If you only have the reminder's name and need the corresponding instance of  **IOrleansReminder**, call the **Grain.GetReminder** method:-->
  如果你只有提醒器的名字并且需要相应的**IOrleansReminder**实例，调用**Grain.GetReminder**方法：

``` csharp
protected Task<IOrleansReminder> GetReminder(string reminderName)
```

## 我应该使用什么？
<!--## Which Should I Use?-->
<!--We recommend that you use timers in the following circumstances:-->
我们建议你在以下情况下使用定时器：

<!--* It doesn't matter (or is desirable) that the timer ceases to function if the activation is deactivated or failures occur.-->
<!--* If the resolution of the timer is small (e.g. reasonably expressible in seconds or minutes).-->
<!--* The timer callback can be started from `Grain.OnActivateAsync` or when a grain method is invoked.-->
* 如果激活被注销或者错误发生后定时器不再工作不重要（或者可取的）。
* 如果计时器周期很小（例如：几秒或者几分钟是合理的）。
* 定时器的回掉函数可以在`Grain.OnActivateAsync`或者grain方法被调用的时候被启动。

<!--We recommend that you use reminders in the following circumstances:-->
我们建议你在以下情况使用提醒器：

<!--* When the periodic behavior needs to survive the activation and any failures.-->
<!--* To perform infrequent tasks (e.g. reasonably expressible in minutes, hours, or days).-->
* 当周期性的行为需要不受激活的或者其他错误的影响。
* 运行不常见的任务（例如：几分钟几小时或者几天是合理的）。

## 组合定时器和提醒器
<!--## Combining Timers and Reminders-->

<!--You might consider using a combination of reminders and timers to accomplish your goal. For example, if you need a timer with a small resolution that needs to survive across activations, you can use a reminder that runs every five minutes, whose purpose is to wake up a grain that restarts a local timer that may have been lost due to a deactivation.-->
你可能考虑组合使用定时器和提醒器来完成你的目的。例如：如果你需要一个小周期的并且不受激活影响的定时器，你可以使用一个每五分钟运行的提醒器，目的是唤醒一个grain来重启可能在注销时已经丢失的本地定时器。