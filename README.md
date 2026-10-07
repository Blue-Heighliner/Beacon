# Beacon

[![Release](https://img.shields.io/github/v/release/Blue-Heighliner/Beacon.svg?label=Release)](https://github.com/Blue-Heighliner/Beacon/releases/latest)
[![License: MIT](https://img.shields.io/github/license/Blue-Heighliner/Beacon.svg)](LICENSE)
[![Build](https://github.com/Blue-Heighliner/Beacon/actions/workflows/build.yml/badge.svg)](https://github.com/Blue-Heighliner/Beacon/actions/workflows/build.yml)
[![Coverage](.github/badges/badge_linecoverage.svg)](https://github.com/Blue-Heighliner/Beacon/actions/workflows/build.yml)

Beacon Inventory, an offline desktop app for tracking technical equipment: barcode-driven serial number entry, filtering, user accounts with badge or password sign-in, and Excel export. Built on .NET 10 and Avalonia UI, with data kept in a local SQLite database. The app makes no network calls of any kind, and its third-party libraries are listed in [THIRD-PARTY-NOTICES.txt](THIRD-PARTY-NOTICES.txt).

## Requirements

- Windows 10/11, 64-bit, or Linux, x86-64 (glibc-based distro). No separate .NET runtime install needed, the executable is self-contained.
- A display server on Linux (X11).

## Installing

Download the executable for your platform from the latest [Release](https://github.com/Blue-Heighliner/Beacon/releases/latest):

- **Windows** - download `BlueHeighliner.Beacon.exe` and run it.
- **Linux** - download `BlueHeighliner.Beacon`, mark it executable (`chmod +x BlueHeighliner.Beacon`), and run it.

## Documentation

| File | Covers |
| --- | --- |
| [Docs/Api.md](Docs/Api.md) | The service and view model contracts and how a screen flows through them |
| [Docs/Usage.md](Docs/Usage.md) | First run, everyday use, and where data lives |
| [Docs/Architecture.md](Docs/Architecture.md) | Why the app is built the way it is |
| [Docs/Project.md](Docs/Project.md) | This repository's scripts, publishing, and CI |
| [Docs/Components/](Docs/Components) | One file per complex internal component |
