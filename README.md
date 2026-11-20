<img width="1831" height="1154" alt="NVMe Secure Erase Utility" src="https://github.com/user-attachments/assets/f5db6356-d4d6-4e3c-a244-1039c902a250" />


[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)

A high-performance, WPF-based hardware destruction utility for securely wiping and unallocating NVMe and SSD drives.

---

## ⚡ Core Features

*   **Drive Discovery:** Automatically scans and lists physical SSD/NVMe drives via safe PowerShell process execution.
*   **Secure Wipe:** Executes cryptographic sanitize commands to permanently erase data and unallocate drive space.
*   **Download ISO:** You can download the ISO for flashing to a Bootable USB directly from the windows launcher.

## 🛠️ Requirements

To compile or modify the source code, you will need:
* **Visual Studio 2026** (or newer)
* **.NET 10.0 SDK**
* Windows 10 / 11

## 🚀 Installation & Usage

**For Standard Users:**
Do not download the source code. Go to the [Releases](../../releases) tab on the right side of the GitHub page and download the latest compiled `NVMe Secure Erase.exe`. 

**For Developers:**
1. Clone the repository.
2. Open `NVMe_Secure_Erase.slnx` in Visual Studio 2026.
3. Set the build dropdown configuration at the top to **Release**.
4. Click **Build > Rebuild Solution**.
5. The compiled executable will be located in your `bin\Release\net10.0-windows\` folder.
