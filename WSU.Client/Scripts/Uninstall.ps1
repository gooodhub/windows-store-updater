Param(
  [Parameter(Mandatory=$True)]
  [string]$packageName
)

Get-AppxPackage -Name $packageName | Remove-AppxPackage