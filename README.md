# WslcDesktop

A simple WinUI 3 desktop app for managing Windows containers (wslc) through the `container` CLI.

It's supposed to be the Front end for the WSL containers.

## Features

- List local container images
- Build images from a `Dockerfile` or `Containerfile`
- Run containers with ports, volumes, and environment variables
- Delete images

## Requirements

- Windows 10/11
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Windows App SDK
- The `container` CLI available on your `PATH`

## Run

```bash
dotnet run
```

## Tech stack

- C# / .NET 10
- WinUI 3
- Windows App SDK

## Disclaimer

This project is like 95% vibe coded (including this README), use it at your own risk.