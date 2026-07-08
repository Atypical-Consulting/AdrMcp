# Security Policy

## Supported Versions

AdrMcp is currently pre-1.0/early-stage software. Security fixes are applied to
the latest published `0.1.x` release. There is no long-term support (LTS)
branch at this time.

| Version | Supported          |
| ------- | ------------------ |
| 0.1.x   | :white_check_mark: |
| Older   | :x:                |

## Reporting a Vulnerability

**Please do not report security vulnerabilities through public GitHub
issues, discussions, or pull requests.**

Instead, report vulnerabilities privately using GitHub's private
vulnerability reporting feature:

1. Go to the [Security tab](https://github.com/phmatray/AdrMcp/security) of this repository.
2. Click **"Report a vulnerability"**.
3. Fill in as much detail as you can, including:
   - A description of the vulnerability and its potential impact
   - Steps to reproduce (a minimal `docs/adr/` layout that triggers the issue is ideal)
   - The AdrMcp version, .NET SDK version, and OS you tested on
   - Any relevant MCP tool call and parameters (e.g. `create_adr`, `update_adr`, `supersede_adr`) involved

If you are unable to use GitHub's private reporting for any reason, you may
contact the maintainer directly at
[phmatray@gmail.com](mailto:phmatray@gmail.com). Please do not include
vulnerability details in any public issue.

### Response Time

We aim to acknowledge new reports within **7 days** and to provide an
initial assessment (severity, affected versions, and a remediation plan or
timeline) within that same window. Timelines for a fix or patch release will
depend on severity and complexity, and we will keep the reporter updated
throughout the process.

## Known Risk: Filesystem Access

AdrMcp reads and writes Architectural Decision Records as Markdown files on the
local filesystem, under a configured ADR directory (by default `docs/adr/`).
The MCP tools operate on paths derived from this directory and from tool
parameters.

- **Write surface.** Authoring and lifecycle tools (`create_adr`,
  `update_adr`, `set_status`, `supersede_adr`, `link_adrs`, `render_index`)
  can create or modify files under the ADR directory. All mutating tools are
  **preview-by-default**: they report the intended change and only apply it
  when the caller explicitly confirms. Operators should still treat tool
  parameters that influence file paths (slugs, identifiers, output targets) as
  untrusted input and confine AdrMcp to a directory they control.
- **Read surface.** Navigation and intelligence tools (`list_adrs`, `get_adr`,
  `search_adrs`, `find_related_adrs`, `get_adr_graph`, and others) read files
  from the ADR directory. When pointing AdrMcp at a repository you do not
  control, treat its ADR contents as untrusted data.

## Known Risk: Code-Link Providers

AdrMcp can associate decision records with source code and flag potentially
stale decisions via provider-based checks (`ICodeLinkProvider`, used by tools
such as `find_stale_adrs`, `coverage_report`, and `suggest_adr_from_change`).
The default provider inspects files on disk relative to the configured
repository. When analyzing an untrusted repository, run AdrMcp in an isolated
or ephemeral environment (container, VM, or CI sandbox) and only point it at
directories and branches you trust.

If you find a way to escalate any of the above into a more severe issue (for
example, path traversal outside the designated ADR directory, or bypassing the
preview-by-default guarantee for writes), please report it privately as
described above.
