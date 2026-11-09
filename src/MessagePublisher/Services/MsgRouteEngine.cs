using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using MessagePublisher.Configuration;

namespace MessagePublisher.Services;

public static class MsgRouteEngine
{
    private static readonly IReadOnlyDictionary<string, string> QueueByRoutingKey = LoadQueueAddresses();

    public static void RouteMessage(string routingKey, string message)
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

        MSMQSendPort.SendMessage(message, queueAddress);
    }

    private static IReadOnlyDictionary<string, string> LoadQueueAddresses()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        var addresses = configuration.GetSection(MsmqAddressSettings.SectionName)
            .Get<Dictionary<string, string>>();

        if (addresses == null || addresses.Count == 0)
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        return new Dictionary<string, string>(addresses, StringComparer.OrdinalIgnoreCase);
    }
}
