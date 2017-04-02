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
  # 1.0.15 msi
  checksum      = 'f5d7f3c1d33b9d73ce328f5e8bcf25b8f534913ff7361f1774f3edb709a03895'
  checksumType  = 'sha256'
  checksum64    = ''
  checksumType64= 'sha256'
}

Install-ChocolateyPackage @packageArgs
