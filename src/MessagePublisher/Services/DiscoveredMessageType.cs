using System;

namespace MessagePublisher.Services;

public sealed record DiscoveredMessageType(Type ClrType, string ShortName);
