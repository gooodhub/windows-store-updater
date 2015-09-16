Param(
  [Parameter(Mandatory=$True)]
  [string]$certPath
)

certutil.exe -addstore Root $certPath