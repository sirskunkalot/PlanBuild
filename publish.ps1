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
    
    [Parameter(Mandatory)]
    [System.String]$SolutionPath
    
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

# name without .dll
$name = "$TargetAssembly" -Replace('.dll')

# Create the mdb file
$pdb = "$TargetPath\$name.pdb"
if (Test-Path -Path "$pdb") {
    Write-Host "Create mdb file for plugin $name"
    Invoke-Expression "& `"$(Get-Location)\libraries\Debug\pdb2mdb.exe`" `"$TargetPath\$TargetAssembly`""
}

if ($Target.Equals("Debug")) {
    Write-Host "Updating local installation in $ValheimPath"
    
    $plug = New-Item -Type Directory -Path "$ValheimPath\BepInEx\plugins\$name" -Force
    Write-Host "Copy $TargetAssembly to $plug"
    Copy-Item -Path "$TargetPath\$name.dll" -Destination "$plug" -Force
    Copy-Item -Path "$TargetPath\$name.pdb" -Destination "$plug" -Force
    Copy-Item -Path "$TargetPath\$name.dll.mdb" -Destination "$plug" -Force
    
    $assets = "$ProjectPath\assets"
    if (Test-Path -Path "$assets") {
        Write-Host "Copy assets to $plug"
        Copy-Item -Path "$assets\*" -Destination "$plug" -Recurse -Force -ErrorAction SilentlyContinue
    }

    $dedi = "$ValheimPath\..\Valheim dedicated server"
    if (Test-Path -Path "$dedi") {
      if (Get-Process -Name 'valheim_server' -ErrorAction Ignore) {
        Write-Host "Dedicated server is running, plugin will not be updated"
      }
      else {
        $dediplug = New-Item -Type Directory -Path "$dedi\BepInEx\plugins\$name" -Force
        Write-Host "Copy $TargetAssembly to $dediplug"
        Copy-Item -Path "$TargetPath\$name.dll" -Destination "$dediplug" -Force -ErrorAction SilentlyContinue
        Copy-Item -Path "$TargetPath\$name.pdb" -Destination "$dediplug" -Force -ErrorAction SilentlyContinue
        Copy-Item -Path "$TargetPath\$name.dll.mdb" -Destination "$dediplug" -Force -ErrorAction SilentlyContinue
        
        if (Test-Path -Path "$assets") {
            Write-Host "Copy assets to $dediplug"
            Copy-Item -Path "$assets\*" -Destination "$dediplug" -Recurse -Force -ErrorAction SilentlyContinue
        }
      }
    }
}

if($Target.Equals("Release")) {
    Write-Host "Packaging for Release"

    $output_dir = $ProjectPath + "\bin\Release"
    $resources_dir = $ProjectPath + "\assets"
    $mod_dll = $output_dir + "\" + $name + ".dll"
    $target_dir = $SolutionPath + "\distribute"

    cd $ProjectPath

    # Get mod version from assembly version info
    $mod_version = (Get-Command $mod_dll).FileVersionInfo.FileVersion

    # Locations to put artifacts
    $artifact_path = $output_dir + "\artifact"
    $nexus_path = $output_dir + "\nexus"
    $tsio_path = $output_dir + "\ts_io"
    $raw_path = $output_dir + "\raw"
    $raw_dir_path = $raw_path + "\" + $name
    echo "Raw dir path" + $raw_dir_path
    New-Item -ItemType Directory -Force -Path $artifact_path
    New-Item -ItemType Directory -Force -Path $nexus_path
    New-Item -ItemType Directory -Force -Path $tsio_path
    New-Item -ItemType Directory -Force -Path $raw_path
    Remove-Item $raw_path\* -Recurse 
    New-Item -ItemType Directory -Force -Path $raw_dir_path
     
    ###################################
    ####### Raw DLL and assets
    ###################################
    
    Copy-Item $mod_dll "$raw_dir_path\"
    Copy-Item -Path "$resources_dir\*" -Destination "$raw_dir_path\" -Recurse -Force
    Copy-Item "$SolutionPath\README.md" "$raw_dir_path\" -Force
    Copy-Item "$SolutionPath\CHANGELOG.md" "$raw_dir_path\" -Force
    
    ###################################
    ####### Plain packaging
    ###################################
    
    $raw_zip = "$artifact_path\$name-$mod_version.zip"
    cd "$raw_path"
    $zip_cmd_raw = '& "C:\Program Files\7-Zip\7zG.exe" "a" ' + $raw_zip + ' "."'
    Remove-Item "$raw_zip"
    echo $zip_cmd_raw
    Invoke-Expression $zip_cmd_raw

    ###################################
    ####### Nexus packaging
    ###################################

    $nexus_zip = "$nexus_path\$name-$mod_version-nexus.zip"
    cd "$raw_path"
    $zip_cmd_nexus = '& "C:\Program Files\7-Zip\7zG.exe" "a" ' + $nexus_zip + ' "."'
    Remove-Item "$nexus_zip"
    echo $zip_cmd_nexus
    Invoke-Expression $zip_cmd_nexus 
    cd $ProjectPath

    ###################################
    ####### Thunderstore packaging
    ###################################
    # Create temp directory for TSIO package
    $tsio_tmp_directory = "$output_dir\ts_io_tmp\$name$mod_version"
    New-Item -ItemType Directory -Force -Path "$tsio_tmp_directory"

    # Copy mandatory thunder files to tmp directory
    Copy-Item "$SolutionPath\README.md" "$tsio_tmp_directory"
    #Add-Content "$tsio_tmp_directory\README.md" -value "`r`n"
 
    Copy-Item "$SolutionPath\icon.png" "$tsio_tmp_directory"

    Copy-Item "$SolutionPath\manifest.json" "$tsio_tmp_directory"

    # Copy mod dll into tmp plugins directory
    New-Item -ItemType Directory -Path "$tsio_tmp_directory\plugins\" -Force
    Copy-Item "$raw_dir_path" "$tsio_tmp_directory\plugins\" -Force -Recurse

    $tsio_zip = "$tsio_path\$name-$mod_version-tsio.zip"
    Remove-Item "$tsio_zip"
    cd "$tsio_tmp_directory"
    $zip_cmd_tsio = '& "C:\Program Files\7-Zip\7zG.exe" "a" ' + $tsio_zip + ' "."'
    echo $zip_cmd_tsio
    Invoke-Expression $zip_cmd_tsio -Verbose

    sleep 1

    cd $ProjectPath
    Remove-Item -Path "$tsio_tmp_directory"

    New-Item -ItemType Directory -Force -Path "$target_dir"
    Copy-Item "$nexus_zip" "$target_dir" -Force
    Copy-Item "$tsio_zip" "$target_dir" -Force
    Copy-Item "$raw_zip" "$target_dir" -Force
    Copy-Item "$mod_dll" "$target_dir" -Force

    echo "
    === All done ==="
}

# Pop Location
Pop-Location