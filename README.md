# test-message-publisher

## Message Publisher (WPF)

Desktop tool to **load a NuGet package** from a **v3 feed** (for example Nexus), discover **message types** that inherit a configured **base type**, edit scalar properties in a **dynamic form**, and **serialize** the result to **JSON** (MSMQ send is left for a later milestone).

### Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (Windows)
- Target: **`net8.0-windows`** (WPF)

### Build and run

```bash
dotnet build MessagePublisher.sln -c Release
dotnet run --project src/MessagePublisher/MessagePublisher.csproj
```

### Usage

1. **Feed URL (v3)**: NuGet v3 service index, for example `https://api.nuget.org/v3/index.json`, or your Nexus v3 endpoint.
2. **Package id** and **Version**: The package that contains your message DTOs.
3. **Base type (full name)**: Full name of the abstract/interface/base class in that package, e.g. `MyCompany.Messages.MessageBase`.
4. **User / Password** (optional): For authenticated Nexus feeds.
5. Click **Load package** — assemblies under `lib/<TFM>` are resolved (prefer `net8.0` … `netstandard2.0` … `net45`).
6. Pick a **message type** and fill fields; **Build serialized payload** shows the wire-format string (JSON by default, or your package plugin when configured).

### Serialization plugin and dependency injection

When your message package ships proprietary serialization (alongside the base message type), configure it in **`messagepublisher.settings.json`** next to the executable (a template ships with the project and is copied on build).

```json
{
  "serializationPlugin": {
    "serializerTypeFullName": "MyCompany.Messages.WireMessageSerializer",
    "serializerMethodName": "Serialize",
    "useStaticMethod": false
  },
  "dependencyInjection": {
    "services": [
      {
        "serviceTypeFullName": "MyCompany.Messages.IWireContext",
        "implementationTypeFullName": "MyCompany.Messages.WireContext",
        "lifetime": "singleton"
      }
    ]
  }
}
```

| Setting | Meaning |
|---------|---------|
| `serializerTypeFullName` | Class in the loaded NuGet package that performs serialization. Leave empty to use built-in JSON. |
| `serializerMethodName` | Instance or static method to call. Prefer `string Method(object)` or `string Method(object, Type)`. |
| `useStaticMethod` | When `true`, the method is invoked statically on the type (no DI instance). |
| `dependencyInjection.services` | Spring-style registrations: optional `serviceTypeFullName` (contract), required `implementationTypeFullName`, and `lifetime` (`singleton` or `transient`). |

After **Load package**, the app builds a small **constructor-injection container** over assemblies from that package so the serializer (and any registered services) can be created with dependencies. The status line reports whether JSON or the plugin serializer is active.

### Project layout

- [`src/MessagePublisher/Services/NuGetPackageLoader.cs`](src/MessagePublisher/Services/NuGetPackageLoader.cs) — download `.nupkg` via NuGet client APIs.
- [`src/MessagePublisher/Services/PackageAssemblyResolver.cs`](src/MessagePublisher/Services/PackageAssemblyResolver.cs) — choose `lib/` TFM folder.
- [`src/MessagePublisher/Services/MessagePackageLoadContext.cs`](src/MessagePublisher/Services/MessagePackageLoadContext.cs) — collectible `AssemblyLoadContext`.
- [`src/MessagePublisher/Services/MessageTypeDiscovery.cs`](src/MessagePublisher/Services/MessageTypeDiscovery.cs) — find concrete assignable types.
- [`src/MessagePublisher/ViewModels/PropertyEditorViewModel.cs`](src/MessagePublisher/ViewModels/PropertyEditorViewModel.cs) — parsing and validation per property.
- [`src/MessagePublisher/Services/JsonMessageSerializer.cs`](src/MessagePublisher/Services/JsonMessageSerializer.cs) — default JSON serialization.
- [`src/MessagePublisher/Services/PluginMessageSerializer.cs`](src/MessagePublisher/Services/PluginMessageSerializer.cs) — reflection-based plugin serializer.
- [`src/MessagePublisher/Services/PluginServiceContainer.cs`](src/MessagePublisher/Services/PluginServiceContainer.cs) — package-scoped DI container.
- [`src/MessagePublisher/messagepublisher.settings.json`](src/MessagePublisher/messagepublisher.settings.json) — plugin and DI configuration.

### Notes

- **Wire format**: JSON is the default when `serializationPlugin` is unset; otherwise the configured package method defines the payload format.
- **net45-only packages**: Often load at runtime on .NET 8; if discovery fails, confirm the base type name and dependencies in the package.

---

## Node (Cloud Agent environment)

Minimal Node workspace used to validate the [Cursor Cloud Agent environment](https://cursor.com/schemas/environment.schema.json) (`install` runs `npm ci --ignore-scripts` from the repo root).

Add dependencies and scripts in `package.json` as the project grows.
