using System;
using System.Collections.Generic;
using MessagePublisher.Configuration;

namespace MessagePublisher.Services;

public class MsgRouteEngine : IMsgRouteEngine
{
    IMSMQSenderService _msmqSenderService;
    public MsgRouteEngine(IMSMQSenderService msmqSenderService)
    {
        _msmqSenderService = msmqSenderService;
    }

    private static readonly IReadOnlyDictionary<string, string> QueueByRoutingKey = AppSettings.GetMsmqAddresses();

    public void RouteMessage(string routingKey, string message)
    {
        if (string.IsNullOrWhiteSpace(routingKey))
            throw new ArgumentException("Routing key is required.", nameof(routingKey));
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        if (!QueueByRoutingKey.TryGetValue(routingKey.Trim(), out var queueAddress)
            || string.IsNullOrWhiteSpace(queueAddress))
        {
            throw new InvalidOperationException(
                $"No MSMQ queue address configured for routing key '{routingKey}'.");
        }

        _msmqSenderService.SendMessage(message, queueAddress);
    }
}
