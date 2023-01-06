#!/bin/bash

SCRIPT=$(echo 'Flight Computer' | sed 's/\s/_/g')

PATTERN="using Sandbox\\.Game\\.EntityComponents;\\nusing Sandbox\\.ModAPI\\.Ingame;\\nusing Sandbox\\.ModAPI\\.Interfaces;\\nusing SpaceEngineers\\.Game\\.ModAPI\\.Ingame;\\nusing System\\.Collections\\.Generic;\\nusing System\\.Collections;\\nusing System\\.Linq;\\nusing System\\.Text;\\nusing System;\\nusing VRage\\.Collections;\\nusing VRage\\.Game\\.Components;\\nusing VRage\\.Game\\.GUI\\.TextPanel;\\nusing VRage\\.Game\\.ModAPI\\.Ingame\\.Utilities;\\nusing VRage\\.Game\\.ModAPI\\.Ingame;\\nusing VRage\\.Game\\.ObjectBuilders\\.Definitions;\\nusing VRage\\.Game;\\nusing VRage;\\nusing VRageMath;\\nusing System\\.Collections\\.Immutable;\\n\\nnamespace IngameScript {\\n    partial class Program: MyGridProgram {"

mkdir $SPACE_ENGINEERS_SCRIPT_DIR/$SCRIPT

sed -z "s/$PATTERN//g" Program.cs |\
sed -z 's/}\n}\n$//g' |\
DOTNET_ROLL_FORWARD=LatestMajor csmin >\
$SPACE_ENGINEERS_SCRIPT_DIR/$SCRIPT/Script.cs
