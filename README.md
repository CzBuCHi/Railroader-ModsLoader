# Railroader-ModsLoader [WIP]

Mod loader for game **Railroader**

Aim is to replace **Railloader** mod loader as its developement was rescently stopped.

NOTE: Not compatible with **Railloader** as Railloader is canceling call to `LogManager::Awake` method where this manager injects its code ...

## Currently implemented

-   code injection to Assembly-CSharp.dll

-   compiling plugin source code to DLL and loading it int the game

    -   idea here is to use mod source code instead of compiled DLL:
        then anybody (and ideally mod manager during compilation) can check if mod is not doing anything not game-related ...

    -   currently code has limited set of references, i have 2 choices how to deal with them:
        -   reference everything from `Railroader_Data\\Managed\\*.dll` and `Mods\\*\\*.dll` (not good)
        -   add references to `Definition.json` where plugin author would need to list all needed references (better)

-   concept of 'marker' interfaces (see bellow)

### Marker interfaces

when plugin implements marker interface manager will inject extra function call to the end of `OnIsEnabledChanged` method

note: if method does not exists on plugin, manager will create one

```cs
protected override void OnIsEnabledChanged() {
    base.OnIsEnabledChanged();
    // plugin code here
    // injected call goes here
}
```

-   **IHarmonyPlugin**: manager will call harmony PatchAll / UnpatchAll when PluginBase::IsEnabled changes
-   **ITopRightButtonPlugin** manager will create / destroy icon button in top-right corner of screen when PluginBase::IsEnabled changes
