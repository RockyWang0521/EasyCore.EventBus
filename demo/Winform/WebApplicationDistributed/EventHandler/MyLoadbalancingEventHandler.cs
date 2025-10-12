using EasyCore.EventBus.Event;
using WebApplicationDistributed.EventMessage;

namespace WebApplicationDistributed.EventHandler
{
    public class MyLoadbalancingEventHandler : IDistributedEventHandler<LoadbalancingEventMessage>
    {
        public async Task HandleAsync(LoadbalancingEventMessage eventMessage)
        {
            // Do something with the event message

            await Task.CompletedTask;
        }
    }
}
