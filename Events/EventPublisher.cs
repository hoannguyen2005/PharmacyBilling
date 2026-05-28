using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PharmacyBillingService.Events
{
    public class EventPublisher : IEventPublisher
    {
        private readonly ILogger<EventPublisher> _logger;

        public EventPublisher(ILogger<EventPublisher> logger)
        {
            _logger = logger;
        }

        public Task PublishAsync<T>(string eventName, T eventData)
        {
            var jsonString = JsonSerializer.Serialize(eventData, new JsonSerializerOptions { WriteIndented = true });
            
            _logger.LogInformation("================================================================================");
            _logger.LogInformation("PUBLISHING EVENT: {EventName}", eventName);
            _logger.LogInformation("Payload:\n{Payload}", jsonString);
            _logger.LogInformation("================================================================================");
            
            return Task.CompletedTask;
        }
    }
}
