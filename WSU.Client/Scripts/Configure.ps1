$t = Test-Path HKLM:\SOFTWARE\Policies\Microsoft\Windows\Appx

if ($t -ne "true")
{
	New-Item -Path HKLM:\SOFTWARE\Policies\Microsoft\Windows\Appx
}
Set-ItemProperty -Path HKLM:\SOFTWARE\Policies\Microsoft\Windows\Appx -Name AllowAllTrustedApps -Value 1