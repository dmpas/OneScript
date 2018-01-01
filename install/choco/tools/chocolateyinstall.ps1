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
  # 1.0.19 exe
  checksum      = '5250cd1a514c5f63014573f5ee33c041945db11ae15eb3024cad283cc1c51800'
  checksumType  = 'sha256'
  checksum64    = ''
  checksumType64= 'sha256'
}

Install-ChocolateyPackage @packageArgs
