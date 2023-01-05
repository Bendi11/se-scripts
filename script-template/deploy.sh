#!/bin/bash

SCRIPT=@NAME@

SCRIPT_DIR=@SCRIPT_DIR@


mkdir $SCRIPT_DIR/$SCRIPT

xargs -a pattern.txt -i sed -z 's/{}//g' Program.cs |\
sed -z 's/}\n}\n$//g' |\
DOTNET_ROLL_FORWARD=LatestMajor csmin >\
$SCRIPT_DIR/$SCRIPT/Script.cs
