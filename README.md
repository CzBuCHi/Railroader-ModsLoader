# Railroader-ModsLoader [WIP]
Mod loader for game Railroader

Aim is to replace Railloader mod loader as its developement was rescently stopped.

Currently implemented:
- code injection to Assembly-CSharp.dll
- compiling plugin source code to dll and loading it int the game
    - idea here is to use mod source code instead of compiled DLL
    - then anybody (and ideally mod loader during compilation) can check if mod isnt doing anything funky ...
    
