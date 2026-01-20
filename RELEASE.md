# Releasing ScreenshotUploader

This repo uses GitHub Actions to build and publish Windows release artifacts.

## Versioning + tags

The release workflow triggers on pushed tags:

- **Production**: `vX.Y.Z` (example: `v1.2.3`)
- **Staging / prerelease**: `staging-vX.Y.Z` (example: `staging-v1.2.3`)

## Create a release (recommended: via GitHub Actions)

1. Ensure `master` is up to date locally.
2. Create a tag.

```powershell
# Production release
git tag v1.2.3

# OR: staging prerelease
git tag staging-v1.2.3
```

3. Push the tag.

```powershell
git push origin v1.2.3
# or
git push origin staging-v1.2.3
```

4. Wait for the workflow run to finish.
5. Download the asset from the GitHub Release:
   - **File name**: `ScreenshotUploader-<version>-win-x64.zip`
   - **Contents**: the published Windows build (`dotnet publish` output)

## What the workflow does

The workflow file is:

- `.github/workflows/deploy.yml`

High level:

- Parses the tag to determine **environment** and **version**
- Generates release notes from commits since the previous same-prefix tag
- Builds on `windows-latest` using:
  - `dotnet publish -c Release -r win-x64 --self-contained false`
- Zips the `publish/` output into:
  - `ScreenshotUploader-<version>-win-x64.zip`
- Creates/updates a GitHub Release:
  - `staging-v*` tags are marked as **pre-release**

## Build a release locally (no GitHub Release)

From the repo root:

```powershell
dotnet restore .\ScreenshotUploader.csproj
dotnet publish .\ScreenshotUploader.csproj -c Release -r win-x64 --self-contained false -o publish

# Zip it (optional)
Compress-Archive -Path .\publish\* -DestinationPath .\ScreenshotUploader-win-x64.zip -Force
```

Notes:

- This is a **framework-dependent** build (requires the .NET 8 Desktop Runtime installed on the target machine).
- If you want a self-contained build instead, use:

```powershell
dotnet publish .\ScreenshotUploader.csproj -c Release -r win-x64 --self-contained true -o publish
```

## Sentry DSN (kept out of git)

Sentry is configured via environment variable at runtime:

- `SENTRY_DSN` (required to enable Sentry)
- `SENTRY_DEBUG=true` (optional)

Example:

```powershell
$env:SENTRY_DSN="https://..."
$env:SENTRY_DEBUG="true"
```

## Linting the workflow locally

- `yamllint`:

```powershell
python -m yamllint .github/workflows/deploy.yml
```

- `actionlint` (after installing via `winget`, restart your terminal so it’s on PATH):

```powershell
actionlint .github/workflows/deploy.yml
```

