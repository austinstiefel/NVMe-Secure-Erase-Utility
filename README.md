# NVMe Secure Erase Utility

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)

A lightweight, instant-launch Windows utility for fast SSD partition clearing and downloading bootable media for military-grade cryptographic NVMe erasure.

---

## ⚡ Core Features

*   **Instant-Launch Architecture:** Built in native C# to completely eliminate the background-extraction lag common in bundled Python executables. UAC elevation triggers the millisecond the application is launched.
*   **Windows Portable Wiper:** Natively integrates with Windows PowerShell to asynchronously scan for connected SSDs, strip partition tables, and unallocate space without freezing the UI.
*   **Bootable Media Downloader:** Utilizes a "Stub Downloader" pattern to keep the executable under 5MB. It fetches a 2GB+ bootable Linux ISO directly from GitHub Releases with a real-time progress bar.
