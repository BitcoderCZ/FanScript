Param (
	[Parameter(Mandatory)]
	[string]$version
)

$profiles = 'win-x64', 'win-arm64', 'linux-x64', 'linux-arm64'

Function Publish-Proj
{
	Param (
		[Parameter(Mandatory)]
		[string]$DirName
	)

	Write-Host "Publishing $DirName"
	try
	{
		Push-Location "./$DirName"
		./publish.ps1 $profiles
	}
	finally
	{
		Pop-Location
	}
}

Publish-Proj 'FanScript.Cli'
Publish-Proj 'FanScript.LangServer'

$publishDir = 'Publish'

Write-Host 'Copying files'

New-Item -Path . -Name $publishDir -ItemType "directory" -Force > $null

foreach ($profile in $profiles) {
	New-Item -Path $publishDir -Name $profile -ItemType "directory" -Force
	$outDir = "./$publishDir/$profile"

	New-Item -Path $outDir -Name "Cli" -ItemType "directory" -Force > $null
	Copy-Item -Path "./FanScript.Cli/bin/Publish/$profile/*" -Destination "$outDir/Cli"

	New-Item -Path $outDir -Name "VSCodeExtension" -ItemType "directory" -Force > $null
	$extensionDir = "$outDir/VSCodeExtension"
	Copy-Item -Path "./FanScript.LangServer/bin/Publish/$profile/*" -Destination $extensionDir
	Copy-Item -Path "./VSCodeExtension/*" -Destination $extensionDir -Filter * -Exclude 'node_modules','FanScript.LangServer*' -Recurse -Force

	# for some fucking reason -Exclude doesn't work in subdirectories, so the client/node_modules dir has to be deleted
	$clientModulesDir = "$extensionDir/client/node_modules"
	if (Test-Path $clientModulesDir) {
		Remove-Item -Path $clientModulesDir -Recurse -Force
	}
}

Write-Host 'Compressing folders'

foreach ($profile in $profiles) {
	$fromDir = "./$publishDir/$profile/*"
	$toFile = "./$publishDir/FanScript_$($profile)_$version.zip"
	Write-Host "Comressing $fromDir to $toFile"
	
	Compress-Archive -Path $fromDir -CompressionLevel 'Optimal' -DestinationPath $toFile -Force
}

Write-Host 'Done'