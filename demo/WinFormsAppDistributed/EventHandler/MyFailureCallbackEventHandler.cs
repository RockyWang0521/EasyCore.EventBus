using EasyCore.EventBus.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinFormsAppDistributed.EventMessage;

namespace WinFormsAppDistributed.EventHandler
{
    public class MyFailureCallbackEventHandler : IDistributedEventHandler<FailureCallbackEventMessage>
    {
        public async Task HandleAsync(FailureCallbackEventMessage eventMessage)
        {
            throw new Exception();
        }
    }
}
