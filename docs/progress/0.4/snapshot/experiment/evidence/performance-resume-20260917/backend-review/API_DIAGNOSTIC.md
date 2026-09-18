> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../../../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Copied LocalApi diagnostic

`decorate_api.py` consumes a directory from `tests/profile_native_draw.py`
(optionally already decorated with Hub spans) and creates a **new** directory.
It changes only copied `native/NativeHost.cs` and copied `NativeDrawTiming.cs`.
Original files, the parent copy, authenticated transport, JSON locks, timeout,
poll backoff, exceptions, and returned objects remain unchanged. Exact anchors
and input hashes must match; stale/changed inputs and existing outputs refuse.

The generated timing file adds `api.events` and `api.dropped` to the existing
`native-draw-timing.json`. It stores no URLs, job identifiers, tokens, headers,
request/response content, or exception messages. Error rows contain exception
type only. The buffer is capped at 30000 rows; any dropped rows invalidate a
complete latency decomposition. File writing occurs only at the existing
post-check flush. Added instrumentation has overhead.

Every API event carries `command`, matching the outer `Api.Post` event, and
diagnostic `post`/`request` serials. The command id is bound to the **same worker
thread's explicit Api.Post or Api.GetRecovery call**, not whichever UI command
is globally active. Concurrent background HTTP calls are therefore excluded.
Request rows say `initial`, `poll`, or `outside-post` (explicit recovery GET).
Rows also carry start/end Stopwatch ticks, milliseconds, thread, and
`outcome=completed|error`. Post rows have poll/wait counts and requested wait
milliseconds. Request rows have observed request/response byte counts when
those stages completed.

| Span | Meaning / inclusion |
|---|---|
| Existing `Api.Post` | Outer inclusive transport call, unchanged parent timer |
| `Api.LocalPost` | Inclusive LocalApi.Post; initial request, parses, polls, waits |
| `Api.Request` | Inclusive one authenticated HttpWebRequest lifecycle |
| `Api.RequestEncode` | Json(body) plus UTF8 encoding; includes JsonSerialize |
| `Api.JsonSerialize` | Serializer-lock wait and serialization |
| `Api.RequestStream` | Request-stream acquisition |
| `Api.RequestWrite` | Writing already encoded request bytes |
| `Api.ResponseHeaders` | GetResponse wait, including server work and transport |
| `Api.ResponseRead` | Response stream CopyTo, including network/body read |
| `Api.ResponseArray` | MemoryStream.ToArray allocation/copy |
| `Api.ResponseDecode` | UTF8 decoding only; Request executes before timer |
| `Api.JsonParse` | Serializer-lock wait and DeserializeObject |
| `Api.ErrorRead` | Existing WebException response-body read, content omitted |
| `Api.PollWait` | Actual Thread.Sleep elapsed time; requested sum also recorded |

Parents are **inclusive**. Never add Request, LocalPost and outer Post together,
or JsonSerialize to its containing RequestEncode. Header time does not identify
server time independently. Poll count is the number of GET attempts including
a final success/error GET; wait count includes attempted sleeps. Unknown bytes
are omitted rather than written as zero. A disposal exception marks the parent
as error even if it had already prepared a return value.

Validation to date is source transformation and compile-only. Six focused Python
checks cover anchor drift, output ownership, source/copy changes, unchanged
protocol literals and errors, bounded/redacted data, and worker-thread command
correlation. They do not prove native HTTP/poll behavior at runtime. The earlier
060150 compile predates the thread-correlation tightening; use the v2 receipt.

After product promotion, in the experiment directory:

```powershell
python -B native_launch.py --mode g2 --build-only
python -B tests/profile_native_draw.py --manifest native-build/<identity>/build.json --output <new-base>
python -B evidence/performance-resume-20260917/backend-review/decorate_api.py <new-base-or-hub-copy> <new-api-copy>
python -B tests/run_native_draw_profile.py --copies <new-api-copy> --compile-only
```

Root alone may later remove `--compile-only` for the existing serial 5+5 native
diagnostic. This helper does not start that run. The runner revalidates original
and generated hashes before capture. An added decorator can compose onto these
copies before that run, but the resulting final source set must be compiled
and bound again. There is no p95, physical-input, GPU or package-acceptance claim.
