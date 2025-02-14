using EasyCore.EventBus.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebApplicationDistributed.EventMessage
{
    public class WebDistributedEventMessage : IEvent
    {
        public string Message { get; set; }
    }
}
