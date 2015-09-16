Param(
  [Parameter(Mandatory=$True)]
  [string]$packagePath
)

Add-AppxPackage $packagePath -ForceApplicationShutdown