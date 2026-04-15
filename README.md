# hass-link

[![Build](https://github.com/gstevenson/hass-link/actions/workflows/build.yml/badge.svg)](https://github.com/gstevenson/hass-link/actions/workflows/build.yml)
[![Latest release](https://img.shields.io/github/v/release/gstevenson/hass-link)](https://github.com/gstevenson/hass-link/releases/latest)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

A Windows system tray app that publishes device metrics to [Home Assistant](https://www.home-assistant.io/) via MQTT. Sensors appear automatically in Home Assistant without any manual configuration.

## Requirements

- Windows 10 or 11 (64-bit)
- A running MQTT broker (e.g. [Mosquitto](https://mosquitto.org/)) accessible from your Home Assistant instance
- The [MQTT integration](https://www.home-assistant.io/integrations/mqtt/) enabled in Home Assistant

## Installation

1. Download the latest `hass-link-setup-x.x.x.exe` from the [Releases](https://github.com/gstevenson/hass-link/releases) page
2. Run the installer — Windows will prompt for administrator access, which is required for CPU and GPU temperature sensors
3. hass-link starts automatically after installation and adds a tray icon to the taskbar

## First run

On first launch, the Settings window opens automatically. Enter your MQTT broker details and click **Save**.

If you need to open Settings later, right-click the tray icon and choose **Settings...**.

## Configuration

### Connection tab

| Field | Description |
|---|---|
| MQTT Host | IP address or hostname of your MQTT broker |
| Port | Default is `1883`, or `8883` for TLS |
| Username / Password | Leave blank if your broker has no authentication |
| Use TLS/SSL | Enable for encrypted connections |
| Base Topic | Root topic for state messages. Default: `hass-link` |

Use the **Test Connection** button to verify your broker details before saving.

### Sensors tab

Enable or disable individual sensors. All sensors are enabled by default except Battery (which is only relevant for laptops) and GPU Temperature.

| Sensor | What it reports |
|---|---|
| CPU Usage | Overall processor load (%) |
| RAM Usage | Memory used (%, GB used, GB total) |
| Disk Usage | Free and used space per drive |
| Network Throughput | Upload and download speed per network adapter (MB/s) |
| Active Window | Title of the currently focused window |
| System Uptime | Hours since last boot |
| Battery | Charge level, charging state, AC power (laptops only) |
| CPU Temperature | CPU package temperature (°C) |
| GPU Temperature / Load | GPU temperature (°C) and load (%) |

> **Note:** CPU and GPU temperature sensors require the app to run as administrator. If you installed via the installer this is handled automatically. If you see no temperature readings, check that the app has admin privileges.

### General tab

| Field | Description |
|---|---|
| Device Name | How this machine appears in Home Assistant. Defaults to the Windows hostname. Use a unique name if you run hass-link on multiple machines. |
| Publish every (s) | How often sensor values are sent to your broker. Minimum 5 seconds. |
| Start with Windows | Launch hass-link automatically when you log in. |

## Home Assistant

No configuration is needed in Home Assistant. When hass-link connects to your broker it publishes [MQTT Discovery](https://www.home-assistant.io/integrations/mqtt/#mqtt-discovery) messages, and your sensors will appear automatically under a single device named after your machine.

To find them: **Settings → Devices & Services → MQTT → Devices** — look for the device name you configured.

## Tray icon

| Icon | Meaning |
|---|---|
| Green dot | Connected to MQTT broker |
| Grey dot | Disconnected or connecting |

Right-clicking the tray icon gives access to Settings, About, and Exit.
