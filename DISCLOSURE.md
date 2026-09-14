# Third-party dependency and licence disclosure

Every third-party component in this repository, with the licence **verified against the package
metadata itself** (NuGet catalog `licenseExpression`, npm registry, or the LICENSE file shipped
inside the package) rather than quoted from documentation or memory.

Verified on 2026-09-14 against the versions actually referenced by this solution.

Nothing here charges per deployment, per seat or per client application. There is no component
licensed from the original author on an ongoing basis, no licence key, no private package feed
and no service that the application calls at runtime.

---

## Runtime dependencies — shipped in the deployed application

| Package | Version | Licence | Notes |
|---|---|---|---|
| Microsoft.AspNetCore.App | 8.0 (shared framework) | MIT | Part of the .NET platform |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 8.0.11 | MIT | |
| Microsoft.EntityFrameworkCore.Relational | 8.0.11 | MIT | |
| Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore | 8.0.11 | MIT | Powers `/health` |
| Npgsql.EntityFrameworkCore.PostgreSQL | 8.0.10 | PostgreSQL | BSD-style, permissive |
| Npgsql | 8.0.5 (transitive) | PostgreSQL | BSD-style, permissive |
| ClosedXML | 0.104.2 | MIT | XLSX export |

## Build-time / tooling dependencies — not shipped

| Package | Version | Licence | Notes |
|---|---|---|---|
| Microsoft.EntityFrameworkCore.Design | 8.0.11 | MIT | `dotnet ef` only; `PrivateAssets` |

## Test-only dependencies — not shipped

| Package | Version | Licence | Notes |
|---|---|---|---|
| xunit | 2.4.2 | Apache-2.0 | |
| xunit.runner.visualstudio | 2.4.5 | MIT | |
| Xunit.SkippableFact | 1.4.13 | MS-PL | Microsoft Public Licence, permissive |
| Microsoft.EntityFrameworkCore.InMemory | 8.0.11 | MIT | |
| coverlet.collector | 6.0.0 | MIT | |
| Microsoft.NET.Test.Sdk | 17.6.0 | MICROSOFT .NET LIBRARY LICENSE | See the warning below |
| FluentAssertions | 6.12.2 | Apache-2.0 | **See the warning below — do not upgrade** |

## Client-side libraries — vendored into `wwwroot/lib`, never loaded from a CDN

| Library | Version | Licence | Notes |
|---|---|---|---|
| htmx | 1.9.12 | 0BSD | Zero-clause BSD: public-domain equivalent, attribution not even required |
| jQuery | 3.6.0 | MIT | Used only by the validation scripts |
| jQuery Validation | bundled | MIT | |
| jQuery Validation Unobtrusive | bundled | MIT | The minified file carries a stale Apache-2.0 header; the `LICENSE.txt` shipped with the package is MIT (.NET Foundation). The LICENSE file governs |

All client-side libraries are committed to the repository. The application makes no request to any
external host at runtime, which is also what allows it to run under a strict Content-Security-Policy.

## Fonts and assets

No commercial font, icon set or image is used. The icons are hand-drawn inline SVG paths in
`Servicos/Icones.cs` and the stylesheet is written from scratch — there is no Bootstrap, no icon
font and no theme with its own licence.

---

## Warnings for whoever maintains this next

**1. Do not upgrade FluentAssertions past version 7.**

Version 6.12.2 (pinned here) and the 7.x line are Apache-2.0 and free. **Version 8 and later
changed to a commercial licence** requiring payment for non-open-source use. A routine "update
all NuGet packages" would silently pull it into a codebase that by then may have been copied into
several client applications.

Recommended fix before that can happen: replace it with **AwesomeAssertions** (Apache-2.0, a
drop-in fork with the same API) or **Shouldly** (BSD-3-Clause). Both verified free.

**2. Microsoft.NET.Test.Sdk uses a proprietary Microsoft licence, not MIT.**

It is free to use and redistribute and it is a test-only package that never reaches the deployed
application, so it does not affect commercial reuse of the product. It is listed here only for
completeness, because it is the one package in the solution without an open-source SPDX licence.

**3. If a PDF library is added later, check the licence before choosing.**

The obvious candidate, QuestPDF, is dual-licensed and free only below a revenue threshold, which
creates exactly the per-client cost this project is meant to avoid. **PDFsharp / MigraDoc 6.2.4
is MIT** and was verified as the safe choice for this purpose.

**4. How to check a licence properly.**

Do not trust documentation or a README badge. Read it from the package feed:

```bash
# NuGet
cat=$(curl -s "https://api.nuget.org/v3/registration5-semver1/<package>/<version>.json" \
      | python3 -c "import sys,json;print(json.load(sys.stdin)['catalogEntry'])")
curl -s "$cat" | python3 -c "import sys,json;d=json.load(sys.stdin);print(d.get('licenseExpression') or d.get('licenseFile'))"

# npm
curl -s "https://registry.npmjs.org/<package>/<version>" | python3 -c "import sys,json;print(json.load(sys.stdin)['license'])"
```

A package that reports `licenseFile` instead of a `licenseExpression` deserves a read: permissive
licences are normally expressed as an SPDX string, so the substitution is itself a signal.

Check the **latest major version** too, not only the one being pinned — that is precisely how the
FluentAssertions change would otherwise arrive unnoticed.
