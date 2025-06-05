# Kill any running Claude processes
Get-Process | Where-Object { $_.ProcessName -like "*claude*" } | Stop-Process -Force -ErrorAction SilentlyContinue

# Wait a moment to ensure processes are fully terminated
Start-Sleep -Seconds 2

# Launch Claude - using the default installation path
Start-Process "C:\Users\$env:USERNAME\AppData\Local\AnthropicClaude\claude.exe"