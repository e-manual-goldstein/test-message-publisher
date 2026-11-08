# test-message-publisher

## Message Publisher (WPF)

Desktop tool to **load a NuGet package** from a **v3 feed** (for example Nexus), discover **message types** that inherit a configured **base type**, edit scalar properties in a **dynamic form**, and **serialize** the result to **JSON** (MSMQ send is left for a later milestone).

### Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (Windows)
- Target: **`net8.0-windows`** (WPF)

### Solution layout (stand-in for private packages)

| Project | Role |
|---------|------|
| [`src/BaseMessageModule`](src/BaseMessageModule) | Package **B** — abstract `BaseMessage` |
| [`src/MessageTypes`](src/MessageTypes) | Package **A** — concrete message DTOs (references BaseMessageModule) |
| [`src/Serialisation`](src/Serialisation) | Package **C** — `EventBus`, `MessageBuilder`, `XmlTextSerialiser` (+ interfaces) |
| [`src/MessagePublisher`](src/MessagePublisher) | WPF host — **project-references all three** (via [`Directory.Build.props`](src/MessagePublisher/Directory.Build.props)) |

Namespaces (`BaseMessageModule`, `MessageTypes`, `Serialisation`) are placeholders; replace with your corporate names via `GlobalUsings` when you swap in real assemblies.

### Build and run

```bash
dotnet build MessagePublisher.sln -c Release
dotnet run --project src/MessagePublisher/MessagePublisher.csproj
```

### Configuration (`appsettings.json`)

Package ids and the base type full name are **fixed defaults** (not edited in the UI):

| Setting | Meaning |
|--------|---------|
| `MessageTypesPackageId` | **Package A** — concrete message types |
| `BaseTypePackageId` | **Package B** — assembly/NuGet package that defines the base type (referenced by A) |
| `BaseTypeFullName` | CLR full name of the base message type in package B |
| `PreferredFeedName` | Optional — matches a `packageSources` name from NuGet.config |

### Usage

1. **NuGet source**: Dropdown populated from **NuGet.config** (same hierarchy Visual Studio uses).
2. **Versions**: Dropdowns query the selected feed for **Package A** and **Package B**.
3. **Credentials** (optional): Override NuGet.config stored credentials if needed.
4. For local development (no NuGet), click **Load referenced modules** — discovers types from the in-repo `MessageTypes` project against `BaseMessageModule.BaseMessage`.
5. When packages are published to your feed, use **Load packages (NuGet)** — downloads B then A; merges in any referenced-module types not already found.
6. Pick a **message type** and fill fields; **Build serialized payload** uses `MessageBuilder` / `XmlTextSerialiser` from the **Serialisation** project (XML wire format).

### Project layout

- [`src/MessagePublisher/Services/NuGetPackageLoader.cs`](src/MessagePublisher/Services/NuGetPackageLoader.cs) — download `.nupkg` via NuGet client APIs.
- [`src/MessagePublisher/Services/PackageAssemblyResolver.cs`](src/MessagePublisher/Services/PackageAssemblyResolver.cs) — choose `lib/` TFM folder.
- [`src/MessagePublisher/Services/MessagePackageLoadContext.cs`](src/MessagePublisher/Services/MessagePackageLoadContext.cs) — collectible `AssemblyLoadContext`.
- [`src/MessagePublisher/Services/MessageTypeDiscovery.cs`](src/MessagePublisher/Services/MessageTypeDiscovery.cs) — find concrete assignable types.
- [`src/MessagePublisher/ViewModels/PropertyEditorViewModel.cs`](src/MessagePublisher/ViewModels/PropertyEditorViewModel.cs) — parsing and validation per property.
- [`src/MessagePublisher/Services/ModuleCompositionRoot.cs`](src/MessagePublisher/Services/ModuleCompositionRoot.cs) — wires Serialisation module types for the host.
- [`src/Serialisation/XmlTextSerialiser.cs`](src/Serialisation/XmlTextSerialiser.cs) — XML payload format.

### Notes

- **Wire format**: XML via `XmlTextSerialiser` (stand-in for proprietary package C). `JsonMessageSerializer` remains in the repo only as an unused reference implementation.
- **net45-only packages**: Often load at runtime on .NET 8; if discovery fails, confirm the base type name and dependencies in the package.

---

## Node (Cloud Agent environment)

Minimal Node workspace used to validate the [Cursor Cloud Agent environment](https://cursor.com/schemas/environment.schema.json) (`install` runs `npm ci --ignore-scripts` from the repo root).

Add dependencies and scripts in `package.json` as the project grows.
