namespace MessagePublisher.Services
{
    public interface IMSMQSenderService
    {
        void SendMessage(string message, string queueAddress);
    }
}