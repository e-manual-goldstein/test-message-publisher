# Message Publisher (WPF)

Desktop tool to **load a NuGet package** from a **v3 feed** (for example Nexus), discover **message types** that inherit a configured **base type**, edit scalar properties in a **dynamic form**, and **serialize** the result to **JSON** (MSMQ send is left for a later milestone).

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (Windows)
- Target: **`net8.0-windows`** (WPF)

## Build and run

```bash
dotnet build MessagePublisher.sln -c Release
dotnet run --project src/MessagePublisher/MessagePublisher.csproj
```

## Usage

1. **Feed URL (v3)**: NuGet v3 service index, for example `https://api.nuget.org/v3/index.json`, or your Nexus v3 endpoint.
2. **Package id** and **Version**: The package that contains your message DTOs.
3. **Base type (full name)**: Full name of the abstract/interface/base class in that package, e.g. `MyCompany.Messages.MessageBase`.
4. **User / Password** (optional): For authenticated Nexus feeds.
5. Click **Load package** — assemblies under `lib/<TFM>` are resolved (prefer `net8.0` … `netstandard2.0` … `net45`).
6. Pick a **message type** and fill fields; **Build serialized JSON** shows the payload string.

## Project layout

- [`src/MessagePublisher/Services/NuGetPackageLoader.cs`](src/MessagePublisher/Services/NuGetPackageLoader.cs) — download `.nupkg` via NuGet client APIs.
- [`src/MessagePublisher/Services/PackageAssemblyResolver.cs`](src/MessagePublisher/Services/PackageAssemblyResolver.cs) — choose `lib/` TFM folder.
- [`src/MessagePublisher/Services/MessagePackageLoadContext.cs`](src/MessagePublisher/Services/MessagePackageLoadContext.cs) — collectible `AssemblyLoadContext`.
- [`src/MessagePublisher/Services/MessageTypeDiscovery.cs`](src/MessagePublisher/Services/MessageTypeDiscovery.cs) — find concrete assignable types.
- [`src/MessagePublisher/ViewModels/PropertyEditorViewModel.cs`](src/MessagePublisher/ViewModels/PropertyEditorViewModel.cs) — parsing and validation per property.
- [`src/MessagePublisher/Services/JsonMessageSerializer.cs`](src/MessagePublisher/Services/JsonMessageSerializer.cs) — JSON serialization.

## Notes

- **Wire format**: JSON is the default; adjust `JsonMessageSerializer` if the service expects XML or another format.
- **net45-only packages**: Often load at runtime on .NET 8; if discovery fails, confirm the base type name and dependencies in the package.
