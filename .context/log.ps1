param(
    [Parameter(Mandatory = $true)]
    [string]$Message
)

$date = Get-Date -Format "yyyy-MM-dd"
$line = "[$date] $Message"
Add-Content -Path ".context/HISTORY.md" -Value $line
Write-Output $line
