$ErrorActionPreference = 'Stop';

$urltag = $version -replace '\.', '_'
$packageName = 'onescript'
$toolsDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$url = "http://oscript.io/downloads/$urltag/OneScript-$version-setup.exe"
$url64 = ''

$packageArgs = @{
  packageName   = $packageName
  unzipLocation = $toolsDir
  fileType      = 'exe'
  url           = $url
  url64bit      = $url64

  silentArgs    = "/verysilent /norestart /log=`"$($env:TEMP)\$($packageName).$($env:chocolateyPackageVersion).MsiInstall.log`""
  validExitCodes= @(0, 3010, 1641)

  registryUninstallerKey = 'onescript'
  # 1.0.20 exe
  checksum      = 'ee009cb6dda3ae69444d4793645cfe10491a192e1e5e16fb3b514b747f0ccabe'
  checksumType  = 'sha256'
  checksum64    = ''
  checksumType64= 'sha256'
}

Install-ChocolateyPackage @packageArgs
