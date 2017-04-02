$ErrorActionPreference = 'Stop';

$urltag = $version -replace '\.', '_'
$packageName = 'onescript'
$toolsDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$url = "http://oscript.io/downloads/$urltag/msi"
$url64 = ''

$packageArgs = @{
  packageName   = $packageName
  unzipLocation = $toolsDir
  fileType      = 'msi'
  url           = $url
  url64bit      = $url64

  silentArgs    = "/qn /norestart /l*v `"$($env:TEMP)\$($packageName).$($env:chocolateyPackageVersion).MsiInstall.log`""
  validExitCodes= @(0, 3010, 1641)

  registryUninstallerKey = 'onescript'
  # 1.0.16 msi
  checksum      = '2ce3b42857c9bb79ae6f63c25b805aa933f2bc8425074996e3491eec4c6750a9'
  checksumType  = 'sha256'
  checksum64    = ''
  checksumType64= 'sha256'
}

Install-ChocolateyPackage @packageArgs
