#! /bin/bash

if [ $# -ne 2 ]; then
    echo $0: usage: ./build.sh [project] [version] eg. ./build.sh Rebus 1.2
    exit 1
fi

project=$1
version=$2

command="dotnet msbuild '/p:Configuration=Release;TargetFrameworkIdentifier=.NETStandard;TargetFrameworkVersion=v1.3;DefineConstants=NETSTANDARD1_3;Version=$version' ../Rebus/$project.csproj"

echo $command
eval "$command"