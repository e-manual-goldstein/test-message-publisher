# test-message-publisher

## Message Publisher (WPF)

Desktop tool to discover **message types** that inherit a configured **base type**, edit scalar properties in a **dynamic form**, and **serialize** the payload via the in-repo **Serialisation** module (XML). MSMQ send is left for a later milestone.

### Requirements

- Windows with **.NET Framework 4.5.2** or later (all projects target **`net452`**)
- [.NET SDK](https://dotnet.microsoft.com/download) (to build SDK-style projects targeting net452) on Windows, or Visual Studio 2019+
- **Newtonsoft.Json** for reading `appsettings.json` (no Microsoft.Extensions.Configuration on net452)

### Solution layout (stand-in for private packages)

| Project | Role |
|---------|------|
| [`src/BaseMessageModule`](src/BaseMessageModule) | Abstract `Event` |
| [`src/MessageTypes`](src/MessageTypes) | Concrete message DTOs (references BaseMessageModule) |
| [`src/Serialisation`](src/Serialisation) | `EventBus`, `MessageBuilder`, `XmlTextSerialiser` (+ interfaces) |
| [`src/MessagePublisher`](src/MessagePublisher) | WPF host — **project-references all three** (via [`Directory.Build.props`](src/MessagePublisher/Directory.Build.props)) |

Shared **`src/Directory.Build.props`** sets `TargetFramework` **net452** for every project under `src/`.

Namespaces (`BaseMessageModule`, `MessageTypes`, `Serialisation`) are placeholders; replace with your corporate names via `GlobalUsings` when you swap in real assemblies.

### Build and run

```bash
dotnet build MessagePublisher.sln -c Release
dotnet run --project src/MessagePublisher/MessagePublisher.csproj
```

(On Windows; WPF requires a Windows environment.)

### Configuration (`appsettings.json`)

| Setting | Meaning |
|--------|---------|
| `MessagePublisher:BaseTypeFullName` | CLR full name of the base message type (default `BaseMessageModule.Event`) |
| `MSMQAddresses` | Routing key → MSMQ queue path for `MsgRouteEngine.RouteMessage` |

### Usage

1. Start the app — message types load automatically from the referenced **MessageTypes** project.
2. Use **Reload message types** if needed after rebuilding module projects.
3. Pick a **message type**, edit fields, then **Build serialized payload** (`MessageBuilder` / `XmlTextSerialiser`).

### Project layout

- [`src/MessagePublisher/Services/MsgRouteEngine.cs`](src/MessagePublisher/Services/MsgRouteEngine.cs) — resolve routing key to queue and invoke send port.
- [`src/MessagePublisher/Services/MSMQSendPort.cs`](src/MessagePublisher/Services/MSMQSendPort.cs) — MSMQ send (stub).
- [`src/MessagePublisher/Services/ReferencedModuleDiscovery.cs`](src/MessagePublisher/Services/ReferencedModuleDiscovery.cs) — find concrete types in referenced MessageTypes.
- [`src/MessagePublisher/ViewModels/PropertyEditorViewModel.cs`](src/MessagePublisher/ViewModels/PropertyEditorViewModel.cs) — parsing and validation per property.
- [`src/MessagePublisher/Services/ModuleCompositionRoot.cs`](src/MessagePublisher/Services/ModuleCompositionRoot.cs) — wires Serialisation module types for the host.
- [`src/Serialisation/XmlTextSerialiser.cs`](src/Serialisation/XmlTextSerialiser.cs) — XML payload format.

### Notes

- **Wire format**: XML via `XmlTextSerialiser` (stand-in for proprietary serialisation package).

---

## Node (Cloud Agent environment)

Minimal Node workspace used to validate the [Cursor Cloud Agent environment](https://cursor.com/schemas/environment.schema.json) (`install` runs `npm ci --ignore-scripts` from the repo root).

Add dependencies and scripts in `package.json` as the project grows.
