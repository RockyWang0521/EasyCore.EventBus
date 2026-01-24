# Analysis command log

Commands executed during the review:

- ls
- find .. -name AGENTS.md -print
- ls src
- rg -n "class|interface|record|struct" src/EasyCore.EventBus -S
- sed -n '1,200p' src/EasyCore.EventBus/Local/LocalEventBus.cs
- sed -n '1,200p' src/EasyCore.EventBus/Distributed/DistributedEventBus.cs
- sed -n '1,220p' src/EasyCore.EventBus/EasyCoreEventExtend.cs
- sed -n '1,200p' src/EasyCore.EventBus/Options/EventBusOptions.cs
- sed -n '1,220p' src/EasyCore.EventBus/HostedService/EventBusHostedService.cs
