using EasyCore.EventBus.Distributed;
using Microsoft.AspNetCore.Mvc;

namespace Web.Kafka.Publish.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PublishController : ControllerBase
    {
        private readonly IDistributedEventBus _distributedEventBus;

        public PublishController(IDistributedEventBus distributedEventBus)
        {
            _distributedEventBus = distributedEventBus;
        }

        [HttpPost]
        public async Task Publish()
        {
            var em = new WebEventMessage()
            {
                Message = "Hello, world!"
            };

            await _distributedEventBus.PublishAsync(em);

            var em2 = new WebEventMessage2()
            {
                Message = "Hello, world!"
            };

            await _distributedEventBus.PublishAsync(em2);

            var em3 = new WebEventMessage3()
            {
                Message = "Hello, world!"
            };

            await _distributedEventBus.PublishAsync(em3);

            var em4 = new WebEventMessage4()
            {
                Message = "Hello, world!"
            };

            await _distributedEventBus.PublishAsync(em4);
        }
    }
}
