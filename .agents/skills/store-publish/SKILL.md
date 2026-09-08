---
name: store-publish
description: >-
  Skill for building the MSIX package and publishing TodayWallpaper to the
  Microsoft Store. Activate when preparing a release or working on packaging.
---

# Skill: Store Publish

## Responsibility

This skill covers:
1. Building the MSIX package without Visual Studio IDE.
2. Preparing assets and metadata for the Microsoft Store listing.
3. Submitting via Partner Center.

---

## 1. Prerequisites

Install **Build Tools for Visual Studio** (free, no IDE):
- Download: https://visualstudio.microsoft.com/downloads/ → "Build Tools for Visual Studio"
- Select workload: **.NET desktop build tools**

---

## 2. MSIX via dotnet publish

For a WPF project with MSIX configured, run:

```powershell
dotnet publish TodayWallpaper.App/TodayWallpaper.App.csproj `
  -f net8.0-windows `
  -c Release `
  -p:RuntimeIdentifierOverride=win-x64 `
  -p:GenerateAppxPackageOnBuild=true `
  -p:AppxPackageDir="./AppPackages/"
```

The `.msix` file will be in `./AppPackages/`.

---

## 3. Required .csproj settings for MSIX

```xml
<PropertyGroup>
  <OutputType>WinExe</OutputType>
  <TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
  <UseWPF>true</UseWPF>
  <WindowsPackageType>MSIX</WindowsPackageType>
  <ApplicationManifest>app.manifest</ApplicationManifest>
</PropertyGroup>
```

---

## 4. Package.appxmanifest key fields

```xml
<Identity Name="com.todaywallpaper.app"
          Publisher="CN=YourPublisherName"
          Version="1.0.0.0" />

<Properties>
  <DisplayName>Today Wallpaper</DisplayName>
  <PublisherDisplayName>YourName</PublisherDisplayName>
  <Logo>Assets\StoreLogo.png</Logo>
</Properties>

<Capabilities>
  <Capability Name="internetClient" />
  <!-- Location only if using GPS mode -->
  <!-- <DeviceCapability Name="location" /> -->
</Capabilities>
```

---

## 5. Required Store Assets

| Asset                  | Size      |
| ---------------------- | --------- |
| StoreLogo.png          | 50x50     |
| Square150x150Logo.png  | 150x150   |
| Square44x44Logo.png    | 44x44     |
| Wide310x150Logo.png    | 310x150   |
| Store screenshots      | Min 1, recommended 4-10 |

---

## 6. Privacy Policy

Required because the app accesses the internet (weather API + IP geolocation).
A simple GitHub Pages site or Notion page is sufficient.

Must state:
- What data is collected (IP address for geolocation — not stored externally).
- No personal data is sold or shared.
- Weather data fetched anonymously.

---

## 7. Partner Center Submission Steps

1. Go to https://partner.microsoft.com and log in.
2. Create new app → Reserve name "Today Wallpaper".
3. Start a new submission:
   - Upload the `.msix` package.
   - Add Store listing (description, screenshots, keywords).
   - Set age rating: **PEGI 3 / Everyone**.
   - Add Privacy Policy URL.
   - Set pricing: Free (or chosen model).
4. Submit for certification (typically 3-5 business days).
5. Microsoft automatically signs the package — no code signing certificate needed.

---

## 8. Versioning Convention

Follow semantic versioning in the MSIX manifest:
- `Major.Minor.Build.Revision` (e.g., `1.0.0.0`)
- Increment `Build` for each Store submission.
- Increment `Minor` for feature releases.
- The Store requires each new submission to have a higher version number.
