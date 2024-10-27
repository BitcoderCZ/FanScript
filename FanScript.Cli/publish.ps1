Param (
	[Parameter(Mandatory)]
	[string[]]$profiles
)

$publishDir = './bin/Publish'

foreach ($profile in $profiles) {
	Write-Host "Publishing $profile"
	if ($profile -eq 'framework-dependent') {
		dotnet publish -o "$publishDir/$profile/" --no-self-contained -c 'Release' /p:PublishSingleFile=false
	}
	else {
		dotnet publish -o "$publishDir/$profile/" --sc -c 'Release' -r $profile
	}
}

Write-Host "Published $($profiles.Count) profile(s)"