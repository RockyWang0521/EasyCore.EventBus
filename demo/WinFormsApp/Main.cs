using EasyCore.EventBus.Distributed;
using EasyCore.EventBus.Local;
using WinFormsApp.EventMessage;

namespace WinFormsApp
{
    public partial class Main : Form
    {
        private readonly ILocalEventBus _localeventBus;
        private readonly IDistributedEventBus _distributedEventBus;

        public Main(ILocalEventBus localeventBus, IDistributedEventBus distributedEventBus)
        {
            InitializeComponent();

            _localeventBus = localeventBus;

            _distributedEventBus = distributedEventBus;
        }

        private void btn_Local_Click(object sender, EventArgs e)
        {
            var eventMessage = new LocalEventMessage()
            {
                Message = "this is LocalEventBus "
            };

            _localeventBus.PublishAsync(eventMessage);
        }

        private void btn_Distributed_Click(object sender, EventArgs e)
        {
            var eventMessage = new DistributedEventMessage()
            {
                Message = "this is DistributedEventMessage "
            };
            _distributedEventBus.PublishAsync(eventMessage);
        }

        private void btn_Web_Click(object sender, EventArgs e)
        {
            var eventMessage = new WebDistributedEventMessage()
            {
                Message = "this is WebDistributedEventMessage "
            };
            _distributedEventBus.PublishAsync(eventMessage);
        }
    }
}
