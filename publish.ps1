param(
    [Parameter(Mandatory)]
    [ValidateSet('Debug','Release')]
    [System.String]$Target,
    
    [Parameter(Mandatory)]
    [System.String]$TargetPath,
    
    [Parameter(Mandatory)]
    [System.String]$TargetAssembly,

    [Parameter(Mandatory)]
    [System.String]$ValheimPath,

    [Parameter(Mandatory)]
    [System.String]$ProjectPath,
    
    [Parameter()]
    [System.String]$UnityAssembliesPath
)

# Make sure Get-Location is the script path
Push-Location -Path (Split-Path -Parent $MyInvocation.MyCommand.Path)

# Test some preliminaries
("$TargetPath",
 "$ValheimPath",
 "$(Get-Location)\libraries"
) | % {
    if (!(Test-Path "$_")) {Write-Error -ErrorAction Stop -Message "$_ folder is missing"}
}

# Main Script
Write-Host "Publishing for $Target from $TargetPath"

$name = "$TargetAssembly" -Replace('.dll')


if ($Target.Equals("Debug")) {
    Write-Host "Updating local installation in $ValheimPath"
      
    $plug = New-Item -Type Directory -Path "$ValheimPath\BepInEx\plugins\$name" -Force
    Write-Host "Copy $TargetAssembly to $plug"
    Copy-Item -Path "$TargetPath\$TargetAssembly" -Destination "$plug" -Force
    Write-Host "Copy dll's to $UnityAssembliesPath"
    Copy-Item -Path "$TargetPath\*.dll" -Destination "$UnityAssembliesPath" -Force 

    $mono = "$ValheimPath\MonoBleedingEdge\EmbedRuntime";
    Write-Host "Copy mono-2.0-bdwgc.dll to $mono"
    if (!(Test-Path -Path "$mono\mono-2.0-bdwgc.dll.orig")) {
        Copy-Item -Path "$mono\mono-2.0-bdwgc.dll" -Destination "$mono\mono-2.0-bdwgc.dll.orig" -Force
    }
    Copy-Item -Path "$(Get-Location)\libraries\Debug\mono-2.0-bdwgc.dll" -Destination "$mono" -Force

    $pdb = "$TargetPath\$name.pdb"
    if (Test-Path -Path "$pdb") {
        Write-Host "Copy Debug files for plugin $name"
        Copy-Item -Path "$pdb" -Destination "$plug" -Force
        Start-Process -FilePath "$(Get-Location)\libraries\Debug\pdb2mdb.exe" -ArgumentList "`"$plug\$TargetAssembly`""
    }

    # Set dnspy debugger env - after a relog in Windows mono runtime listens on port 56000 instead of 55555
    #$dnspy = '--debugger-agent=transport=dt_socket,server=y,address=127.0.0.1:56000,suspend=n,no-hide-debugger'
    #[Environment]::SetEnvironmentVariable('DNSPY_UNITY_DBG2',$dnspy,'User')
}

if($Target.Equals("Release")) {
    Write-Host "Packaging for ThunderStore"
    $Package="Package"
    $PackagePath=$ProjectPath+"\"+$Package

    Write-Host "$PackagePath\$TargetAssembly"
    Copy-Item -Path "$TargetPath\$TargetAssembly" -Destination "$PackagePath\plugins\$TargetAssembly"
    Copy-Item -Path "$PackagePath\README.md" -Destination "$ProjectPath\README.md"
    Compress-Archive -Path "$PackagePath\*" -DestinationPath "$TargetPath\$TargetAssembly.zip" -Force
 

    $output_dir = $ProjectPath + "bin\Release\"
    $resources_dir = $ProjectPath + "assets\"
    $mod_dll = $output_dir + $mod_name + ".dll"
    $target_dir = $ProjectPath + "output\"

    cd $ProjectPath

    # Get mod version from assembly version info
    $mod_version = (Get-Command $mod_dll).FileVersionInfo.FileVersion

    # Locations to put artifacts
    $artifact_path = $output_dir + "artifacts\"
    $nexus_path = $output_dir + "nexus\"
    $tsio_path = $output_dir + "ts_io\"
    $raw_path = $output_dir + "raw\"
    $raw_dir_path = $raw_path + $mod_name + "\"
    New-Item -ItemType Directory -Force -Path $artifact_path
    New-Item -ItemType Directory -Force -Path $nexus_path
    New-Item -ItemType Directory -Force -Path $tsio_path
    New-Item -ItemType Directory -Force -Path $raw_path
    New-Item -ItemType Directory -Force -Path $raw_dir_path

    echo "New-Item -ItemType Directory -Force -Path $raw_dir_path"

    ###################################
    ####### Raw DLL
    ###################################
    Remove-Item $raw_dir_path* -Recurse 
    Copy-Item $mod_dll $raw_dir_path
    Copy-Item -Path $resources_dir\* -Destination $raw_dir_path -Recurse -Force

    ###################################
    ####### Nexus Packaging
    ###################################
    $nexus_zip = $nexus_path + $name  + "-" + $mod_version + ".zip"
    cd $raw_path
    $zip_cmd_nexus = '& "C:\Program Files\7-Zip\7zG.exe" "a" ' + $nexus_zip + ' "."'
    Remove-Item  $nexus_zip
    echo $zip_cmd_nexus
    Invoke-Expression $zip_cmd_nexus 
    cd $project_dir

    ###################################
    ####### Thunderstore packaging
    ###################################
    # Create temp directory for TSIO package
    $tsio_tmp_directory = $output_dir + "ts_io_tmp\" + $mod_name + $mod_version + "\"
    New-Item -ItemType Directory -Force -Path $tsio_tmp_directory

    # Update Manifest to have correct version number
    $manifest = Get-Content $ProjectPath"resources\manifest.json" -raw | ConvertFrom-Json
    $manifest.version_number = $mod_version
    $manifest | ConvertTo-Json -depth 32| set-content $tsio_tmp_directory"manifest.json"

    # Copy README and icon into tmp directory
    Copy-Item $ProjectPath"README.md" $tsio_tmp_directory
    Add-Content $tsio_tmp_directory"README.md" -value "`r`n"
    Get-Content $ProjectPath"CHANGELOG.md" | Add-Content $tsio_tmp_directory"README.md"

    Copy-Item $ProjectPath"resources\icon.png" $tsio_tmp_directory

    # Copy mod dll into tmp file\plugins directory
    New-Item -ItemType Directory -Path $tsio_tmp_directory"files\plugins\" -Force
    Copy-Item $raw_dir_path $tsio_tmp_directory"files\plugins\" -Force -Recurse



    $tsio_zip = $tsio_path + $mod_name + "-" + $mod_version + "-tsio.zip"
    Remove-Item  $tsio_zip
    cd $tsio_tmp_directory
    $zip_cmd_tsio = '& "C:\Program Files\7-Zip\7zG.exe" "a" ' + $tsio_zip + ' "."'
    echo $zip_cmd_tsio
    Invoke-Expression $zip_cmd_tsio -Verbose

    sleep 1

    cd $ProjectPath
    Remove-Item  $tsio_tmp_directory -Recurse

    Copy-Item $nexus_zip $target_dir -Force
    Copy-Item $tsio_zip $target_dir -Force
    Copy-Item $mod_dll $target_dir -Force

    echo "
    === All done ==="

}

# Pop Location
Pop-Location