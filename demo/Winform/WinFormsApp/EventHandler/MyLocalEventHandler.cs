using EasyCore.EventBus.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinFormsApp.EventMessage;

namespace WinFormsApp.EventHandler
{
    public class MyLocalEventHandler : ILocalEventHandler<LocalEventMessage>
    {
        public async Task HandleAsync(LocalEventMessage eventMessage)
        {
            // Do something with the event message

            await Task.CompletedTask;
        }
    }
}
