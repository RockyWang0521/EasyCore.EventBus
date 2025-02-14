using EasyCore.EventBus.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinFormsAppDistributed.EventMessage
{
    public class DistributedEventMessage : IEvent
    {
        public string Message { get; set; }
    }
}
