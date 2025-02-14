using EasyCore.EventBus.Event;
using WebApplicationDistributed.EventMessage;

namespace WebApplicationDistributed.EventHandler
{
    public class MyDistributedEventHandler : IDistributedEventHandler<WebDistributedEventMessage>
    {
        public async Task HandleAsync(WebDistributedEventMessage eventMessage)
        {
            // Do something with the event message

            await Task.CompletedTask;
        }
    }
}
