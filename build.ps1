#This build assumes the following directory structure
#
#  \               - This is where the project build code lives
#  \build          - This folder is created if it is missing and contains output of the build
#  \src            - This folder contains the source code or solutions you want to build
#
Properties {
    $build_dir = Split-Path $psake.build_script_file    
    $build_artifacts_dir = "$build_dir\build"
    $solution_dir = "$build_dir\src"
}

FormatTaskName (("-"*25) + "[{0}]" + ("-"*25))

Task Default -depends Build 

Task Build -depends Clean {
    Set-Location "$solution_dir"
    Write-Host "Creating BuildArtifacts" -ForegroundColor Green
    Exec { dotnet restore }
    Exec { dotnet test }
    Exec { dotnet pack --configuration Release --output $build_artifacts_dir } 
}

Task Clean {
    Write-Host "Creating BuildArtifacts directory" -ForegroundColor Green
    if (Test-Path $build_artifacts_dir) {
        rd $build_artifacts_dir -rec -force | out-null
    }
    
    mkdir $build_artifacts_dir | out-null
}