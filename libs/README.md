Compile-time reference assemblies for **FortRise 5.3.0**, all libs here are stripped

| File                                            | Needed for                                  |
| ----------------------------------------------- | ------------------------------------------- |
| `TowerFall.Patch.dll`                           | the game itself (FortRise-patched assembly) |
| `FNA.dll`                                       | `Vector2`, `Rectangle`, XNA types           |
| `MonoMod.Utils.dll`                             | errors without it                           |
| `Microsoft.Extensions.Logging.Abstractions.dll` | `ILogger`, injected by FortRise             |
| `0Harmony.dll`                                  | `context.Harmony.PatchAll`                  |

# NEVER PUSH THE ORIGINAL VERSION OF STRIPPED LIBS

**All of these are IL-stripped**:

[BepInEx.AssemblyPublicizer](https://github.com/BepInEx/BepInEx.AssemblyPublicizer):

```sh
dotnet tool install -g BepInEx.AssemblyPublicizer.Cli

for f in TowerFall.Patch FNA MonoMod.Utils Microsoft.Extensions.Logging.Abstractions 0Harmony; do
    assembly-publicizer "<FortRise>/$f.dll" --strip --target Fields -o "libs/$f.dll"
done
```
