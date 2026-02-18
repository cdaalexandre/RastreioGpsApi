# GPS Tracking API

A real-time GPS tracking system built with C# .NET and Azure Functions — receives coordinates from devices and generates location reports.

## Architecture

```
├── ReceberCoordenadas.cs  # Azure Function: receives GPS coordinates
├── VerRelatorio.cs        # Azure Function: generates tracking reports
├── index.html             # Web interface for viewing locations
├── host.json              # Azure Functions configuration
├── Properties/            # Project settings
└── RastreioGpsApi.sln     # Visual Studio solution
```

## Tech Stack

- **C# .NET** — Core language
- **Azure Functions** — Serverless compute for receiving and processing GPS data
- **HTML/JS** — Frontend for report visualization

## How It Works

1. GPS devices send coordinates to the `ReceberCoordenadas` endpoint
2. Data is stored and processed
3. `VerRelatorio` generates location tracking reports
4. Web interface displays device positions and movement history

## Author

**Alexandre Dias-Alves** — Software Engineering student building IoT and cloud-based tracking solutions.
