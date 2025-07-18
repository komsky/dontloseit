param(
    [string]$Destination = 'C:\\inetpub\\wwwroot\\fleamarket',
    [string]$SiteName = 'fleamarket',
    [string]$AppPoolName = 'fleamarket'
)

Import-Module WebAdministration

function Stop-AppPoolSafely {
    param([string]$Name)
    try {
        if ((Get-WebAppPoolState -Name $Name).Value -eq 'Started') {
            Write-Host "Stopping app pool '$Name'..."
            Stop-WebAppPool -Name $Name -ErrorAction Stop
        } else {
            Write-Host "App pool '$Name' is already stopped."
        }
    } catch {
        Write-Warning "Failed to stop app pool '$Name'. Continuing..."
    }
}

function Start-AppPoolSafely {
    param([string]$Name)
    try {
        if ((Get-WebAppPoolState -Name $Name).Value -ne 'Started') {
            Write-Host "Starting app pool '$Name'..."
            Start-WebAppPool -Name $Name -ErrorAction Stop
        } else {
            Write-Host "App pool '$Name' is already running."
        }
    } catch {
        Write-Warning "Failed to start app pool '$Name'."
        throw
    }
}

function Start-SiteSafely {
    param([string]$Name)
    try {
        if ((Get-WebsiteState -Name $Name).Value -ne 'Started') {
            Write-Host "Starting website '$Name'..."
            Start-Website -Name $Name -ErrorAction Stop
        } else {
            Write-Host "Website '$Name' is already running."
        }
    } catch {
        Write-Warning "Failed to start website '$Name'."
        throw
    }
}

function Copy-PublishedFiles {
    param(
        [string]$Source = 'publish',
        [string]$Destination
    )
    try {
        Write-Host 'Copying published files...'
        robocopy $Source $Destination /MIR /NFL /NDL | Out-Null
    } catch {
        Write-Warning 'File copy failed. A file may be locked by another process.'
        throw
    }
}

Stop-AppPoolSafely $AppPoolName
Copy-PublishedFiles -Destination $Destination
Start-AppPoolSafely $AppPoolName
Start-SiteSafely $SiteName
