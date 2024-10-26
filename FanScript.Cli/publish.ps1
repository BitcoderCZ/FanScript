Param (
	[Parameter(Mandatory)]
	[string]$version
)

$profiles = 'win-x64', 'win-arm64', 'linux-x64', 'linux-arm64'
$publishDir = './bin/Publish'

foreach ($profile in $profiles) {
	Write-Host "Publishing $profile"
	dotnet publish -o "$publishDir/$profile/" --sc -c 'Release' -r $profile
}

Write-Host "Published $($profiles.Count) profile(s)"

New-Item -Path $publishDir -Name "Compressed" -ItemType "directory" -Force

$currentDir = Split-Path -Path (Get-Location) -Leaf

foreach ($profile in $profiles) {
	$fromDir = "$publishDir/$profile/*"
	$toFile = "$publishDir/Compressed/$($currentDir)_$($profile)_$version.zip"
	Write-Host "Comressing $fromDir to $toFile"
	
	Compress-Archive -Path $fromDir -CompressionLevel 'Optimal' -DestinationPath $toFile -Force
}

Write-Host "Compressed builds into archives"
Write-Host "Done"