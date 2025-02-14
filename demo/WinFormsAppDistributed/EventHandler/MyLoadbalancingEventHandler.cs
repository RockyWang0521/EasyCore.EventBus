using EasyCore.EventBus.Event;
using WinFormsApp.EventMessage;

namespace WinFormsAppDistributed.EventHandler
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
