# MSIX Packaging and Microsoft Store Publishing 📦

This document details how to package **TodayWallpaper** into an MSIX bundle and publish it through the Microsoft Partner Center without requiring the Visual Studio GUI.

---

## 🛠️ Prerequisites

- **Build Tools**: Install [Build Tools for Visual Studio](https://visualstudio.microsoft.com/downloads/) with the **.NET desktop build tools** workload.
- **Microsoft Developer Account**: An active account in [Windows Partner Center](https://partner.microsoft.com/).
- **Code Signing Certificate**: For local testing, a self-signed certificate can be used; for the store, Microsoft signs the package upon ingestion.

---

## 🚀 Building the MSIX Package via .NET CLI

Run the following command from the root of the solution to build and package the WPF application:

```powershell
dotnet publish TodayWallpaper.App/TodayWallpaper.App.csproj `
  -c Release `
  -r win-x64 `
  --self-contained false `
  -p:GenerateAppxPackageOnBuild=true `
  -p:AppxPackageDir="./AppPackages/"
```

The generated package (`.msix` or `.msixbundle`) will be placed in the `./AppPackages/` directory.

---

## ⚙️ Manifest Configuration (`Package.appxmanifest`)

Key identity and capability fields required in the manifest:

```xml
<Identity Name="com.todaywallpaper.app"
          Publisher="CN=YourPublisherId"
          Version="1.0.0.0" />

<Properties>
  <DisplayName>Today Wallpaper</DisplayName>
  <PublisherDisplayName>Your Name or Organization</PublisherDisplayName>
  <Logo>Assets\StoreLogo.png</Logo>
</Properties>

<Capabilities>
  <!-- Internet access for Open-Meteo queries and IP geolocation -->
  <Capability Name="internetClient" />
</Capabilities>
```

---

## 🖼️ Store Visual Assets Requirements

| Asset | Dimensions (px) | Purpose |
| :--- | :--- | :--- |
| **StoreLogo.png** | 50 × 50 | Search result and listing icon |
| **Square150x150Logo.png** | 150 × 150 | Medium tile in Start Menu |
| **Square44x44Logo.png** | 44 × 44 | Small icon in taskbar and lists |
| **Wide310x150Logo.png** | 310 × 150 | Wide tile in Start Menu |
| **Screenshots** | 1920 × 1080 (minimum 4) | Promotional store listing screenshots |

---

## 🔒 Privacy Policy

The Microsoft Store requires a public Privacy Policy URL because the application utilizes network access:

1. **Location Data**: Clarify that geolocation is inferred anonymously via IP address and is neither logged nor shared with third parties.
2. **Weather Queries**: Approximate latitude/longitude coordinates are sent directly to Open-Meteo's public API without user identification.
3. **Local Storage**: Settings and wallpaper history remain strictly on the local machine (`%APPDATA%`).
