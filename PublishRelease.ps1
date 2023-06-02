New-Item -Path $PSScriptRoot -Name "publish" -ItemType "directory" -Force
$publish_dir = Join-Path $PSScriptRoot "publish"
$mod_build_dir = Join-Path $PSScriptRoot "build\CelesteTAS-EverestInterop\bin\Release\net48"
$studio_build_dir = Join-Path $PSScriptRoot "build\CelesteStudio\bin\Release\net48"
Copy-Item -Path (Join-Path $mod_build_dir "Dialog") -Destination $publish_dir -Recurse -Force
Copy-Item -Path (Join-Path $mod_build_dir "CelesteTAS-EverestInterop.dll") -Destination $publish_dir -Force
Copy-Item -Path (Join-Path $mod_build_dir "CelesteTAS-EverestInterop.pdb") -Destination $publish_dir -Force
Copy-Item -Path (Join-Path $mod_build_dir "everest.yaml") -Destination $publish_dir -Force
Copy-Item -Path (Join-Path $studio_build_dir "Celeste Studio.exe") -Destination $publish_dir -Force
Copy-Item -Path (Join-Path $studio_build_dir "Celeste Studio.pdb") -Destination $publish_dir -Force

Get-ChildItem -Path $publish_dir -Recurse | Compress-Archive -DestinationPath (Join-Path $publish_dir "CelesteTAS.zip") -Force
