using BillingService.Domain.Events;

namespace BillingService.Application.Common;

public interface IEventPublisher
{
    Task PublicarAsync<TEvent>(TEvent evento) where TEvent : IntegrationEvent;
}
