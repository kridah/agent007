# agent007

With license to invoke other agents :)

---

## Table of Contents
- [agent007](#agent007)
  - [Table of Contents](#table-of-contents)
  - [Overview](#overview)
  - [Features](#features)
  - [Prerequisites](#prerequisites)
  - [Getting Started](#getting-started)
    - [1. Clone the Repository](#1-clone-the-repository)
    - [2. Restore Dependencies](#2-restore-dependencies)
    - [3. Set Up Ollama](#3-set-up-ollama)
    - [4. Set Up the Database (SQLite \& EF Core)](#4-set-up-the-database-sqlite--ef-core)
    - [5. Run the Application](#5-run-the-application)
  - [Configuration](#configuration)
  - [Usage](#usage)
  - [Project Structure](#project-structure)
  - [Troubleshooting](#troubleshooting)
  - [License](#license)

---

## Overview

**agent007** is a modern .NET 9 Blazor Server application designed for interactive AI-powered chat and agent-based workflows. The backend is found in `src/Agent007.csproj` and provides a robust, extensible platform for experimenting with AI models and multi-agent systems.

## Features
- Blazor Server web UI
- AI chat and multi-agent orchestration
- Ollama integration for local LLMs
- Modern .NET 9 architecture
- Easy extensibility and configuration
- SQLite database with Entity Framework Core

## Prerequisites
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Ollama](https://ollama.com/) (for local AI model support)
- Git

## Getting Started

Follow these steps to get the project running locally:

### 1. Clone the Repository

```zsh
git clone https://github.com/kridah/agent007.git
cd agent007
```

### 2. Restore Dependencies

```zsh
dotnet restore
```

### 3. Set Up Ollama
- Download and install Ollama from [ollama.com](https://ollama.com/).
- Start Ollama locally (see their docs for details).
- The app will connect to Ollama using the default settings in `src/appsettings.Development.json`.

### 4. Set Up the Database (SQLite & EF Core)

This project uses a SQLite database managed by Entity Framework Core. You need to apply the database migrations before running the app for the first time:

```zsh
dotnet ef database update --project src/Agent007.csproj
```

If you get an error about missing `dotnet-ef`, install it with:

```zsh
dotnet tool install --global dotnet-ef
```

Migrations are found in `src/Migrations/`. This step will create or update the `src/chat.db` SQLite database as needed.

### 5. Run the Application

```zsh
dotnet run --project src/Agent007.csproj
```

- The app will start and display a local URL (e.g., https://localhost:5001 or http://localhost:5000).
- Open the URL in your browser to access the agent007 web interface.

## Configuration

- App settings are found in `src/appsettings.json` and `src/appsettings.Development.json`.
- Ollama connection settings can be adjusted under the `Ollama` section.
- Database is SQLite by default (`src/chat.db`).

## Usage
- Interact with the chat and agent features via the web UI.
- For development, you can modify Razor components in `src/Pages/` and backend logic in `src/Data/` or `src/LLM/`.

## Project Structure
```
agent007/
├── src/
│   ├── Agent007.csproj      # Main project file
│   ├── Program.cs           # App startup
│   ├── Pages/               # Blazor pages
│   ├── Data/                # Data and service logic
│   ├── LLM/                 # AI/LLM integration
│   ├── Migrations/          # EF Core migrations
│   ├── appsettings.json     # App configuration
│   └── chat.db              # SQLite database
├── AppHost/                 # Aspire AppHost (optional)
├── ServiceDefaults/         # Shared service defaults
├── docs/                    # Documentation
└── README.md                # This file
```

## Troubleshooting
- If you see errors about missing dependencies, ensure you have the .NET 9 SDK installed.
- If AI features do not work, make sure Ollama is running locally and the settings match.
- If you see errors about the database, make sure you have run the EF Core migrations as described above.
- For port conflicts, change the port in `src/appsettings.Development.json` or via launch settings.

## License
See [docs/LICENSE](docs/LICENSE) for license information.

---

For more details, see the [docs/](docs/) folder or open an issue if you need help.
