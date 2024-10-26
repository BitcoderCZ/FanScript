Param (
	[Parameter(Mandatory)]
	[string[]]$profiles
)

$publishDir = './bin/Publish'

foreach ($profile in $profiles) {
	Write-Host "Publishing $profile"
	dotnet publish -o "$publishDir/$profile/" --sc -c 'Release' -r $profile
}

Write-Host "Published $($profiles.Count) profile(s)"