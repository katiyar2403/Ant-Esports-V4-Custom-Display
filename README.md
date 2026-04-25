# Ant Esports V4 Custom Display

A lightweight, clean, and fully customizable tray application for the **Ant Esports V4 Digital CPU Air Cooler**.

This tool completely replaces the official bloated software with a modern system tray app.


## ✨ Features

- **Multiple Display Modes**:
  - CPU Temperature Only
  - GPU Temperature Only
  - CPU Usage Only
  - GPU Usage Only
  - Cycle All (Temp + Usage)
  - **Cycle CPU ↔ GPU Temperature** (New)

- Celsius ↔ Fahrenheit support (except in full cycle mode)
- Adjustable cycle interval (1 to 5 seconds)
- Runs silently in System Tray
- Very low resource usage
- No background bloat

## Installation

1. Download the latest release from [Releases](https://github.com/katiyar2403/Ant-Esports-V4-Custom-Display/releases)
2. Extract the zip file
3. **Right-click** `AntV4Display.exe` → **Run as administrator** (Required for HWiNFO registry access)

## How to Use

1. Run the application as Administrator
2. Right-click the tray icon (shield icon) to open the menu
3. Choose your preferred display mode and settings

## Requirements

- Windows 10 / 11 (64-bit)
- [HWiNFO64](https://www.hwinfo.com/) running in the background with **"Report value in Gadget"** enabled for:
  - CPU Temperature
  - GPU Temperature
  - CPU Usage
  - GPU Usage

## Notes

- The app **must** be run as Administrator for registry access.
- In "Cycle All" mode, temperature unit is forced to Celsius (hardware limitation).
- All settings are remembered during the session.

## Contributing

Feel free to open issues or pull requests!

## License

Free for personal use.

---
