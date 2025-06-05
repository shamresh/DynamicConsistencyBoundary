# Kill any existing server processes
Get-Process | Where-Object { $_.ProcessName -like "*DynamicConsistencyBoundary*" } | Stop-Process -Force -ErrorAction SilentlyContinue

# Wait a moment to ensure processes are fully terminated
Start-Sleep -Seconds 2

# Start the server process
$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = ".\MCPServer\bin\Debug\net8.0\DynamicConsistencyBoundary.MCPServer.exe"
$psi.UseShellExecute = $false
$psi.RedirectStandardInput = $true
$psi.RedirectStandardOutput = $true
$psi.RedirectStandardError = $true
$psi.CreateNoWindow = $true

$process = [System.Diagnostics.Process]::Start($psi)

# Function to send a JSON-RPC request
function Send-JsonRpcRequest {
    param(
        [Parameter(Mandatory=$true)]
        $Process,
        [Parameter(Mandatory=$true)]
        [hashtable]$Request
    )
    
    # Convert to JSON with no formatting (single line)
    $json = $Request | ConvertTo-Json -Compress -Depth 10
    Write-Host "Sending: $json" -ForegroundColor Yellow
    
    # Send the JSON as a single line
    $Process.StandardInput.WriteLine($json)
    $Process.StandardInput.Flush()
    
    # Read the response
    $response = $Process.StandardOutput.ReadLine()
    Write-Host "Received: $response" -ForegroundColor Green
    
    return $response
}

try {
    # Test 1: Initialize
    Write-Host "`n=== Testing Initialize ===" -ForegroundColor Cyan
    $initRequest = @{
        jsonrpc = "2.0"
        id = 1
        method = "initialize"
        params = @{
            protocolVersion = "2024-11-05"
            capabilities = @{}
            clientInfo = @{
                name = "test"
                version = "1.0.0"
            }
        }
    }
    
    $initResponse = Send-JsonRpcRequest -Process $process -Request $initRequest
    
    # Test 2: List tools
    Write-Host "`n=== Testing Tools List ===" -ForegroundColor Cyan
    $toolsRequest = @{
        jsonrpc = "2.0"
        id = 2
        method = "tools/list"
    }
    
    $toolsResponse = Send-JsonRpcRequest -Process $process -Request $toolsRequest
    
    # Test 3: Call get_current_position tool
    Write-Host "`n=== Testing Tool Call ===" -ForegroundColor Cyan
    $callRequest = @{
        jsonrpc = "2.0"
        id = 3
        method = "tools/call"
        params = @{
            name = "get_current_position"
            arguments = @{}
        }
    }
    
    $callResponse = Send-JsonRpcRequest -Process $process -Request $callRequest
    
    # Test 4: Invalid request (for testing error handling)
    Write-Host "`n=== Testing Invalid Request ===" -ForegroundColor Cyan
    $invalidRequest = @{
        jsonrpc = "2.0"
        id = 4
        method = "nonexistent_method"
    }
    
    $invalidResponse = Send-JsonRpcRequest -Process $process -Request $invalidRequest
    
} catch {
    Write-Host "Error: $_" -ForegroundColor Red
} finally {
    # Clean up
    if (-not $process.HasExited) {
        $process.Kill()
    }
    $process.Dispose()
}

Write-Host "`n=== Test Complete ===" -ForegroundColor Cyan