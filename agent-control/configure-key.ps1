[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$magic600KeyFile = Join-Path $PSScriptRoot '.env'
if ($PSScriptRoot -match '(?i)(^|[\\/])OneDrive(?:[ -][^\\/]*)?([\\/]|$)') {
    throw 'Run configure-key.ps1 from the actual source agent-control folder outside OneDrive. See README.md for the external LOCALAPPDATA option.'
}
$magic600Secret = Read-Host 'Paste your newly created OpenAI Platform API key (hidden)' -AsSecureString
$magic600SecretPtr = [IntPtr]::Zero
try {
    $magic600SecretPtr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($magic600Secret)
    $magic600PlainKey = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($magic600SecretPtr)
    if ([string]::IsNullOrWhiteSpace($magic600PlainKey) -or $magic600PlainKey -match '[\s"'']') {
        throw 'Invalid empty or whitespace-containing key.'
    }
    $magic600Lines = if (Test-Path -LiteralPath $magic600KeyFile) {
        [IO.File]::ReadAllLines($magic600KeyFile) | Where-Object { $_ -notmatch '^\s*OPENAI_API_KEY\s*=' }
    } else {
        [IO.File]::ReadAllLines((Join-Path $PSScriptRoot '.env.example')) | Where-Object { $_ -notmatch '^\s*OPENAI_API_KEY\s*=' }
    }
    $magic600Text = (@($magic600Lines) + ('OPENAI_API_KEY=' + $magic600PlainKey)) -join [Environment]::NewLine
    [IO.File]::WriteAllText($magic600KeyFile, $magic600Text + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
    Write-Host 'Saved the key to the local git-ignored .env. The key was not printed. Next: node src/cli.mjs doctor'
} catch {
    throw 'Key configuration failed. Check that the pasted key is nonempty and this local directory is writable. No key was printed.'
} finally {
    if ($magic600SecretPtr -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($magic600SecretPtr) }
    Remove-Variable magic600PlainKey, magic600Text -ErrorAction SilentlyContinue
    $magic600Secret.Dispose()
}
