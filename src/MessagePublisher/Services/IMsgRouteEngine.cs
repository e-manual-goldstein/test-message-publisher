namespace MessagePublisher.Services
{
    public interface IMsgRouteEngine
    {
        void RouteMessage(string routingKey, string serializedPreview);
    }
}