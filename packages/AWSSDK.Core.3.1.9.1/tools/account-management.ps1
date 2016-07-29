
function RegisterProfile()
{
	$dllpath = "..\lib\net35\AWSSDK.Core.dll"
	$sdkassembly = [System.Reflection.Assembly]::LoadFrom($dllpath)

	$completed = $FALSE
	do
	{
		Write-Host "1) Add/Update new profile credentials"
		Write-Host "2) List registered profiles"
        Write-Host "3) Remove profile credentials"
		Write-Host "4) Exit"

		Write-Host ""
		$choose = Read-Host "Choose an option"

		If ($choose -eq "1")
		{
			$profileName = Read-Host "Profile name: "
			$accessKey = Read-Host "Access key: "
			$secretKey = Read-Host "Secret key: "
			[Amazon.Util.ProfileManager]::RegisterProfile($profileName, $accessKey, $secretKey)
		}
		ElseIf($choose -eq "2")
		{
			Write-Host ""

			$profiles = [Amazon.Util.ProfileManager]::ListProfileNames() | sort
			foreach($profile in $profiles)
			{
				Write-Host "*" $profile
			}
			Write-Host ""
		}
        ElseIf($choose -eq "3")
        {
			Write-Host ""

			$i = 1
			$profiles = [Amazon.Util.ProfileManager]::ListProfileNames() | sort
			foreach($profile in $profiles)
			{
				Write-Host $i")" $profile
				$i++
			}
			Write-Host ""
            $pick = Read-Host "Select a profile to unregister"
            [Amazon.Util.ProfileManager]::UnregisterProfile($profiles[$pick - 1])
        }
		ElseIf($choose -eq "4")
		{
			$completed = $TRUE
		}
		Else
		{
			Write-Host ""
			Write-Host "Unknown choose"
			Write-Host ""
		}
	}while($completed -ne $TRUE)
}

RegisterProfile