#!/usr/bin/env qsh

// Current Directory
// Script Directory

// env.home_dir, env.script_dir, env["a"]

// operator/ override
// var publish_path = env.HomeDir / "bin/QuickSC.Shell";

@{
    dotnet publish "../QuickSC.Shell/QuickSC.Shell.csproj" --self-contained false -o "../QuickSC.Shell/publish" /p:PublishSingleFile=true -r linux-x64
    rm ../QuickSC.Shell/publish/qsh
    cp ../QuickSC.Shell/publish/QuickSC.Shell ../QuickSC.Shell/publish/qsh
}
