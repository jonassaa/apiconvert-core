# Apiconvert.Core

# Apiconvert.Core

[![NuGet Version](https://img.shields.io/nuget/v/Apiconvert.Core.svg)](https://www.nuget.org/packages/Apiconvert.Core/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Apiconvert.Core.svg)](https://www.nuget.org/packages/Apiconvert.Core/)
[![Build](https://github.com/apiconvert/apiconvert-core/actions/workflows/ci.yml/badge.svg)](https://github.com/apiconvert/apiconvert-core/actions/workflows/ci.yml)
[![License](https://img.shields.io/github/license/apiconvert/apiconvert-core.svg)](LICENSE)

Core conversion and mapping engine for Apiconvert.


Apiconvert.Core is the core library for converting API descriptions and generating interoperable models. It provides a composable conversion engine, rule configuration, and contracts for generation targets.

## Quickstart

Install from NuGet:

```bash
dotnet add package Apiconvert.Core
```

Minimal example:

```csharp
using Apiconvert.Core.Converters;
using Apiconvert.Core.Rules;
using Apiconvert.Core.Contracts;

// Create rules/config (example placeholders)
var rules = new ConversionRules
{
    // configure rules here
};

var engine = new ConversionEngine(rules);

// Convert input into generation models (example placeholders)
var result = engine.Convert(input);

// Use result (generation models / contracts)
```

## Features

- Deterministic conversion pipeline
- Rule-based configuration
- Contracts for generation / interop targets
- Nullable-aware, side-effect-free core paths

## Concepts

- **ConversionEngine**: Orchestrates conversion execution.
- **Rules**: Models + configuration for conversion behavior.
- **Contracts**: Shared DTOs for generators and interop.

## Usage

Typical flow:

1. Build rules/configuration.
2. Instantiate `ConversionEngine`.
3. Convert input models to generation models.
4. Consume generation models in your tooling.

## Compatibility

- Target framework: .NET (see NuGet package metadata)
- `Nullable` enabled

## Versioning

This project follows SemVer. Breaking changes are documented in release notes.

## Contributing

Build:

```bash
dotnet build Apiconvert.Core.sln
```

Test:

```bash
dotnet test Apiconvert.Core.sln
```

## License

Proprietary. See `LICENSE`.
