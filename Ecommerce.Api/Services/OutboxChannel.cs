using System.Threading.Channels;

namespace Ecommerce.Api.Services;

public record PaymentSimulationJob(string OrderNumber, decimal Amount, string Currency);

public class OutboxChannel
{
    public Channel<PaymentSimulationJob> Channel { get; } =
        System.Threading.Channels.Channel.CreateUnbounded<PaymentSimulationJob>();
}