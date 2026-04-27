param(
    [string]$BaseUrl = "https://jaywalker-eaten-squishier.ngrok-free.dev/",
    [int]$DurationSeconds = 45,
    [int]$IntervalSeconds = 8,
    [switch]$DisconnectAtEnd
)

$ErrorActionPreference = "Stop"

if (-not $BaseUrl.EndsWith("/")) {
    $BaseUrl = "$BaseUrl/"
}

$baseUri = [Uri]$BaseUrl
$headers = @{}
if ($baseUri.Host.EndsWith(".ngrok-free.dev", [StringComparison]::OrdinalIgnoreCase)) {
    $headers["ngrok-skip-browser-warning"] = "true"
}

$devices = @(
    @{
        DeviceId = "demo-smoke-pixel6-api34-a"
        ClientInstanceId = "demo-smoke-session-a"
        UserCode = "demo-a"
        UserDisplayName = "Demo Pixel 6 A"
        DeviceModel = "Google Pixel 6 API 34 A"
    },
    @{
        DeviceId = "demo-smoke-pixel6-api34-b"
        ClientInstanceId = "demo-smoke-session-b"
        UserCode = "demo-b"
        UserDisplayName = "Demo Pixel 6 B"
        DeviceModel = "Google Pixel 6 API 34 B"
    }
)

function Invoke-DemoApi {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [string]$Method = "GET",
        [object]$Body = $null
    )

    $uri = [Uri]::new($baseUri, $Path)
    if ($null -eq $Body) {
        return Invoke-RestMethod -Uri $uri -Method $Method -Headers $headers -TimeoutSec 30
    }

    $json = $Body | ConvertTo-Json -Depth 6
    return Invoke-RestMethod -Uri $uri -Method $Method -Headers $headers -ContentType "application/json" -Body $json -TimeoutSec 30
}

function Send-DemoHeartbeat {
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$Device
    )

    $payload = @{
        deviceId = $Device.DeviceId
        clientInstanceId = $Device.ClientInstanceId
        userCode = $Device.UserCode
        userDisplayName = $Device.UserDisplayName
        userEmail = ""
        devicePlatform = "Android"
        deviceModel = $Device.DeviceModel
        appVersion = "1.0.0-demo"
        sentAtUtc = (Get-Date).ToUniversalTime().ToString("o")
    }

    Invoke-DemoApi -Path "api/analytics/active-devices/heartbeat" -Method "POST" -Body $payload | Out-Null
}

function Disconnect-DemoDevice {
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$Device
    )

    $payload = @{
        deviceId = $Device.DeviceId
        clientInstanceId = $Device.ClientInstanceId
        disconnectedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
    }

    Invoke-DemoApi -Path "api/analytics/active-devices/disconnect" -Method "POST" -Body $payload | Out-Null
}

foreach ($device in $devices) {
    try {
        Disconnect-DemoDevice -Device $device
    }
    catch {
        Write-Verbose "Could not clear previous demo session for $($device.DeviceId): $($_.Exception.Message)"
    }
}

Start-Sleep -Milliseconds 500

$startedAt = Get-Date
$endAt = $startedAt.AddSeconds($DurationSeconds)
$round = 0

do {
    $round++

    foreach ($device in $devices) {
        Send-DemoHeartbeat -Device $device
    }

    $stats = Invoke-DemoApi -Path "api/analytics/active-devices"
    $activeDevices = @($stats.devices)
    $activeDemoDevices = @(
        $activeDevices | Where-Object {
            $device = $_
            $devices | Where-Object {
                $_.DeviceId -eq $device.deviceId -and
                $_.ClientInstanceId -eq $device.clientInstanceId
            }
        }
    )

    [PSCustomObject]@{
        Round = $round
        ActiveDeviceCount = $stats.activeDeviceCount
        DemoDeviceCount = $activeDemoDevices.Count
        ExpectedDemoDeviceCount = 2
        GeneratedAtUtc = $stats.generatedAtUtc
    }

    if ($activeDemoDevices.Count -ne 2) {
        throw "Smoke test failed: expected 2 active demo devices, got $($activeDemoDevices.Count)."
    }

    if ((Get-Date).AddSeconds($IntervalSeconds) -lt $endAt) {
        Start-Sleep -Seconds $IntervalSeconds
    }
    else {
        break
    }
}
while ((Get-Date) -lt $endAt)

if ($DisconnectAtEnd) {
    foreach ($device in $devices) {
        Disconnect-DemoDevice -Device $device
    }

    $stats = Invoke-DemoApi -Path "api/analytics/active-devices"
    [PSCustomObject]@{
        Round = "cleanup"
        ActiveDeviceCount = $stats.activeDeviceCount
        DemoDeviceCount = 0
        ExpectedDemoDeviceCount = 0
        GeneratedAtUtc = $stats.generatedAtUtc
    }
}
