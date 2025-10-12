using EasyCore.EventBus.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinFormsAppDistributed.EventMessage;

namespace WinFormsAppDistributed.EventHandler
{
    public class MyDistributedEventHandler : IDistributedEventHandler<DistributedEventMessage>
    {
        public async Task HandleAsync(DistributedEventMessage eventMessage)
        {
            // Do something with the event message

            throw new NotImplementedException();

            await Task.CompletedTask;
        }
    }
}
