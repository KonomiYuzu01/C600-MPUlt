> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# BrowserAct isolated setup diagnosis

2026-09-16. Scope: CLI/toolchain only; no Magic 600 Cell source or native UI changes.

## Result

Use `<user-home>/.cache/magic600-tooling/browser-act/bin/browser-act.exe` (1.4.2).
The original deep installation was retained. No global PATH, registry, security, account or browser changes were made.

## Evidence and diagnosis

- Deep `--version` returned 1.4.2, exit 0.
- Deep `get-skills core --skill-version 2.0.2` returned exit 1, `Error 230101: Invalid command arguments. Detail: DLL load failed while importing strategy_content_check: 文件名或扩展名太长。`
- Direct import using the deep environment reproduced the same ImportError through `browser_act_core → session_manager.execution → page_ready.strategy_content_check`.
- Installed the identical CLI and all 68 constrained distribution versions in a shorter project-specific cache, using the identical CPython 3.12.14 interpreter and existing uv 0.12.15.
- Failing module path length changed from 253 to 188 characters. Its SHA256 remained `79dac96a7d49709162d6d952e376db92d827827396b68cd73fe7d84615e0c6f1`.
- Short-path `--version`, complete core 2.0.2 retrieval, complete advanced retrieval and read-only `browser list` all exited 0.

This establishes a path-sensitive compiled-module loading problem, rather than a missing package or an account failure. The exact loader-internal derived path/threshold was not traced; 253 characters alone does not prove a specific MAX_PATH boundary.

## Current capability and authorization state

The CLI reports headed mode supported, API key not configured, no configured browsers and no active sessions. Read-only `browser list` independently returned `No browsers found.`

The fully read core and advanced guides document local Chrome, direct Chrome, page extraction, state, keyboard/pointer interaction and screenshots. These browser operations were not exercised here. Local Chrome does not require the API key described for stealth/managed-proxy features; no inference is made that existing user Chrome tabs are already attached. A created local environment starts blank unless a separate explicit profile import is performed.

No account login is required to finish this CLI repair. Any later browser creation/profile use must respect the applicable current user authorization and loaded BrowserAct instructions. Stealth/managed-proxy use needs the official login flow. Passwords and verification codes remain user-entered in official UI. No browser creation, auth login, profile scan/import, session open or native-window interaction occurred in this task.

## Reproduction

```powershell
& '<user-home>/.cache/magic600-tooling/browser-act/bin/browser-act.exe' --version
& '<user-home>/.cache/magic600-tooling/browser-act/bin/browser-act.exe' get-skills core --skill-version 2.0.2
& '<user-home>/.cache/magic600-tooling/browser-act/bin/browser-act.exe' browser list
```

Full outputs are in `browseract-core-2.0.2.txt`, `browseract-advanced.txt`, `browseract-browser-list.txt`; exact old/new package inventories and structured command results are alongside this report. Installation receipt is in the shorter tool environment. Removal of that isolated cache is the rollback; nothing global needs restoration.
