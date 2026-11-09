using System;

namespace MessagePublisher.Services;

public class MSMQSendPort : IMSMQSenderService
{
    public void SendMessage(string message, string queueAddress)
    {
        throw new NotImplementedException();
    }
}
