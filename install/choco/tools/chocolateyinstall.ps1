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
  checksum      = '3a3e13423f171b26e7123218c1b1481d2ece3aa788b68f3ff6f75493f12df95c'
  checksumType  = 'sha256'
  checksum64    = ''
  checksumType64= 'sha256'
}

Install-ChocolateyPackage @packageArgs
