# Chess Engine

A chess engine and interactive web application built with **C# and ASP.NET Core (.NET 10)**. The project implements chess board representation, move generation, position evaluation, and computer move selection, with a web interface for playing and analyzing positions.

## Overview

Chess Engine is a personal software engineering project focused on implementing chess logic and building a web application around it.

The project explores algorithmic problem-solving, performance optimization, object-oriented design, and full-stack development using the .NET ecosystem.

## Features

- **Chess board representation:** Manage chess positions and piece placement using bitboards.
- **Move generation:** Generate candidate moves for chess positions.
- **Position evaluation:** Evaluate board positions to help determine favorable moves.
- **Search depth configuration:** Explore different search depths when calculating computer moves.
- **Position loading:** Support Forsyth–Edwards Notation (FEN) for representing chess positions.
- **Web application:** Interact with the engine through a browser-based interface.
- **Containerization:** Docker support for building and deploying the application.

## Technology Stack

| Category | Technologies |
|---|---|
| Language | C# |
| Framework | ASP.NET Core / .NET 10 |
| Frontend | HTML, CSS, JavaScript |
| Architecture | MVC, Services, Dependency Injection |
| Algorithms | Bitboards, Move Generation, Position Evaluation |
| Deployment | Docker, Render |
| Version Control | Git, GitHub |

## Architecture

The application separates chess logic from the web application through dedicated models, interfaces, services, and controllers.

```text
ChessEngine/
├── Controllers/       # HTTP request handling
├── Interfaces/        # Service abstractions
├── Models/            # Chess domain models
├── Services/          # Chess engine and application logic
├── Types/             # Shared types and definitions
├── ViewModels/        # Data passed to application views
├── Views/              # Razor views
├── wwwroot/            # Static frontend assets
├── Program.cs          # Application configuration
├── Dockerfile          # Container configuration
└── ChessEngine.csproj  # Project configuration
```

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Git

### Clone the Repository

```bash
git clone https://github.com/irfaan-jamarussadiq/ChessEngine.git

cd ChessEngine
```

### Run Locally

Restore dependencies:

```bash
dotnet restore
```

Build the application:

```bash
dotnet build
```

Run the application:

```bash
dotnet run
```

Open the local URL printed in the terminal to access the application.

## Docker

Build the Docker image:

```bash
docker build -t chess-engine .
```

Run the container:

```bash
docker run -p 8080:8080 chess-engine
```

Access the application at:

```text
http://localhost:8080
```

## Engineering Goals

This project serves as an exploration of several software engineering concepts:

- Efficient chess position representation and manipulation.
- Algorithmic approaches to move generation and position evaluation.
- Performance considerations when searching chess positions.
- Separation of concerns and maintainable application architecture.
- Building and deploying a containerized .NET web application.

## Future Improvements

- Improve move generation and search performance.
- Implement additional search optimizations.
- Expand automated testing for chess rules and position validation.
- Add performance benchmarks across different search depths.
- Improve the user interface and analysis experience.

## Author

**Irfaan Jamarussadiq**

- [GitHub](https://github.com/irfaan-jamarussadiq)
- [LinkedIn](https://www.linkedin.com/in/irfaanja)

---

*Built as a personal project to explore chess programming, algorithms, and software engineering with C# and .NET.*
