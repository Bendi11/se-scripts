#!/bin/bash

SCRIPT=$(echo $(NAME) | sed 's/\s/_/g')

mkdir -p $SPACE_ENGINEERS_SCRIPT_DIR/$SCRIPT

echo "" > ./final
for file in ./*.cs
do
    sed -zE 's/using (\w|\.)+;|partial class Program: MyGridProgram \{|}\n}\n$|namespace IngameScript \{//g' $file >> ./final
done


cat ./final |\
    DOTNET_ROLL_FORWARD=LatestMajor csmin >\
    $SPACE_ENGINEERS_SCRIPT_DIR/$SCRIPT/Script.cs
