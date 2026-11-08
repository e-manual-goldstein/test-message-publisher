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
4. Click **Load packages** — downloads B then A, loads both `lib/<TFM>` folders, discovers types in A assignable to the base from B.
5. Pick a **message type** and fill fields; **Build serialized payload** shows the wire-format string (JSON by default, or your package plugin when configured).

#### Serialization plugin and dependency injection

Package **B** (base type) often ships proprietary serialization alongside the base message type. Configure it in `appsettings.json`:

| Setting | Section | Meaning |
|--------|---------|---------|
| `SerializerTypeFullName` | `SerializationPlugin` | Class in loaded packages A/B that performs serialization. Empty = built-in JSON. |
| `SerializerMethodName` | `SerializationPlugin` | Static or instance method returning `string`; prefers `(object, Type)` or `(object)`. |
| `UseStaticMethod` | `SerializationPlugin` | When `true`, the method is invoked statically (no DI instance). |
| `DependencyInjection.Services` | `DependencyInjection` | Optional contract, implementation type full name, and `Singleton` or `Transient` lifetime. |

After **Load packages**, a small constructor-injection container is built over assemblies from A and B so the serializer (and registered services) can be constructed. The status line reports JSON vs plugin serialization.

### Project layout

- [`src/MessagePublisher/Services/NuGetPackageLoader.cs`](src/MessagePublisher/Services/NuGetPackageLoader.cs) — download `.nupkg` via NuGet client APIs.
- [`src/MessagePublisher/Services/PackageAssemblyResolver.cs`](src/MessagePublisher/Services/PackageAssemblyResolver.cs) — choose `lib/` TFM folder.
- [`src/MessagePublisher/Services/MessagePackageLoadContext.cs`](src/MessagePublisher/Services/MessagePackageLoadContext.cs) — collectible `AssemblyLoadContext`.
- [`src/MessagePublisher/Services/MessageTypeDiscovery.cs`](src/MessagePublisher/Services/MessageTypeDiscovery.cs) — find concrete assignable types.
- [`src/MessagePublisher/ViewModels/PropertyEditorViewModel.cs`](src/MessagePublisher/ViewModels/PropertyEditorViewModel.cs) — parsing and validation per property.
- [`src/MessagePublisher/Services/JsonMessageSerializer.cs`](src/MessagePublisher/Services/JsonMessageSerializer.cs) — default JSON serialization.
- [`src/MessagePublisher/Services/PluginMessageSerializer.cs`](src/MessagePublisher/Services/PluginMessageSerializer.cs) — reflection-based plugin serializer.
- [`src/MessagePublisher/Services/PluginServiceContainer.cs`](src/MessagePublisher/Services/PluginServiceContainer.cs) — package-scoped DI container.

### Notes

- **Wire format**: JSON is the default when `SerializationPlugin` is unset; otherwise the configured package method defines the payload format.
- **net45-only packages**: Often load at runtime on .NET 8; if discovery fails, confirm the base type name and dependencies in the package.

---

## Node (Cloud Agent environment)

Minimal Node workspace used to validate the [Cursor Cloud Agent environment](https://cursor.com/schemas/environment.schema.json) (`install` runs `npm ci --ignore-scripts` from the repo root).

Add dependencies and scripts in `package.json` as the project grows.
