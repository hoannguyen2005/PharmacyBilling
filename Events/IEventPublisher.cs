using System.Threading.Tasks;

namespace PharmacyBillingService.Events
{
    public interface IEventPublisher
    {
        Task PublishAsync<T>(string eventName, T eventData);
    }
}
