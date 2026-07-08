# Releasing

Releases are automated with [release-please](https://github.com/googleapis/release-please) —
driven by [Conventional Commits](https://www.conventionalcommits.org/), not manual tags.

## How it works

1. Every push to `dev` runs the **release-please** workflow, which keeps an open **release PR**
   up to date: it computes the next version from the `feat:` / `fix:` / `feat!:` commits since the
   last release, and bumps `Directory.Build.props` (the `x-release-please-version` markers) and
   `CHANGELOG.md`.
2. **Merge the release PR** to ship. That tags `vX.Y.Z`, creates the GitHub Release, and — in the
   same workflow run, gated on `release_created` — packs and publishes:
   - stamps `.mcp/server.json` + `mcpb/manifest.json` to the version, packs `AdrMcp`, verifies the
     embedded manifest, builds the `.mcpb` bundle, attaches both to the Release;
   - pushes the package to **NuGet.org**;
   - publishes the manifest to the **MCP Registry** (GitHub OIDC);
   - builds and pushes a multi-arch image to **`ghcr.io/atypical-consulting/adr-mcp`**.

`workflow_dispatch` runs the release-please job as a dry run (no release is created, so the
publish/docker jobs are skipped).

## Version bumps (pre-1.0)

`bump-minor-pre-major` is on, so before `1.0.0`: `feat:` → minor, `fix:`/`chore:` → patch,
`feat!:` / `BREAKING CHANGE` → minor. Write commit subjects accordingly.

## One-time setup

| Secret | Used by | Purpose |
| --- | --- | --- |
| `NUGET_API_KEY` | release-please `publish` job | Push the package to NuGet.org (already an org secret) |

GHCR uses the built-in `GITHUB_TOKEN`; the MCP Registry uses GitHub OIDC — no extra secrets.
The NuGet API key must be scoped to allow pushing the **`AdrMcp`** package (a glob/owner key, or one
that includes `AdrMcp`). release-please must be allowed to open PRs: org/repo setting
**Actions → General → Allow GitHub Actions to create and approve pull requests**.

## Install channels

```bash
# .NET global tool
dotnet tool install -g AdrMcp

# on-demand via dnx (no install; requires the .NET 10 SDK)
dnx AdrMcp --yes

# Docker
docker run --rm -i -v "$PWD:/workspace" ghcr.io/atypical-consulting/adr-mcp --adr-root /workspace/docs/adr
```
