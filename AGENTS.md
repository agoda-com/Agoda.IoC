# AGENTS.md

## Project overview

`Agoda.IoC` is a C# library for registering classes into an IoC container via
attributes (`[RegisterSingleton]`, `[RegisterTransient]`, etc.) instead of
hand-written configuration classes. It ships **two independent flavors** that
solve the same problem differently:

- **Runtime / reflection** — assembly-scans at startup and registers into the
  container. Packages: `Agoda.IoC.Core`, `Agoda.IoC.NetCore`,
  `Agoda.IoC.Unity`, `Agoda.IoC.AutofacExt`. Entry point: `AutoWireAssembly(...)`.
- **Compile-time source generator** — scans source during build and emits a
  `RegisterFrom<AssemblyName>()` extension method (no reflection at runtime).
  Packages: `Agoda.IoC.Generator`, `Agoda.IoC.Generator.Abstractions`.

Target framework is `net6.0`. Published to NuGet.

## Project layout

| Path | What it is |
|---|---|
| `src/Agoda.IoC.sln` | Solution — build/test everything from here |
| `src/Agoda.IoC.Core` | Shared runtime attributes + registration model |
| `src/Agoda.IoC.NetCore` | `IServiceCollection` runtime registration (`AutoWireAssembly`) |
| `src/Agoda.IoC.Unity` | Legacy Unity 3.5 container support |
| `src/Agoda.IoC.AutofacExt` | Autofac runtime support (no open-generic support) |
| `src/Generator/Agoda.IoC.Generator` | Roslyn source generator + analyzers |
| `src/Generator/Agoda.IoC.Generator.Abstractions` | Attributes/interfaces for the generator |
| `src/Generator/Example/GeneratorExample` | Runnable generator usage example |
| `src/*.UnitTests`, `src/Generator/UnitTests` | NUnit test projects |
| `src/ProjectsUnderTest/*` (`Valid*`, `Invalid1..18`) | Compile fixtures consumed by generator/analyzer tests — not standalone |
| `README.md` | Runtime library docs | 
| `Generator.md` | Source generator docs |

## How to build / test

Everything runs from the `src/` directory against `Agoda.IoC.sln`. CI builds and
packs in **Debug** (not Release).

```bash
cd src
dotnet restore Agoda.IoC.sln
dotnet build Agoda.IoC.sln --configuration Debug
dotnet test Agoda.IoC.sln --configuration Debug
```

Before pushing: build + run the full test suite. Tests are NUnit; runtime tests
use FluentAssertions/Shouldly/NSubstitute, generator tests use
Verify (`Verify.SourceGenerators`) snapshot assertions.

## Important notes

- **Two attribute sets with the same names.** Runtime attributes live in
  `Agoda.IoC.Core`; generator attributes live in
  `Agoda.IoC.Generator.Abstractions`. They are not interchangeable — match the
  attribute's namespace to the flavor you're editing.
- **Scoped naming differs by flavor.** Runtime uses `[RegisterPerRequest]`;
  the generator uses `[RegisterScoped]`. Both map to `AddScoped`.
- **Generator/abstractions are referenced as analyzers** in consuming projects
  (`OutputItemType="Analyzer"`), so changes take effect at compile time — a
  clean rebuild may be needed to pick up generator changes.
- **`ProjectsUnderTest/Invalid*` are intentionally invalid.** They exist to
  trigger analyzer diagnostics from the test suite; don't "fix" their code.
- Verify snapshot tests write `*.received.txt` on mismatch; review and rename to
  `*.verified.txt` only when the change to generated output is intended.
