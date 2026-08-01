# Overview

EX exposes an interop API for other mods. It is small, because most of what a mod needs is not about
netplay itself:

```C#
/// <summary>
/// Determine if the game is currently playing online.
///
/// <para>Might be useful if a mod want to trigger some action</para>
/// </summary>
public static bool IsPlayingOnline;
```

Imported the usual way:

```C#
    [ModImportName("TF.EX.API")]
    public static class TfExAPIModImports
    {
        public static Func<bool> IsPlayingOnline;

        static TfExAPIModImports()
        {
            typeof(TfExAPIModImports).ModInterop();
        }
    }
```

# Making your mod netplay compatible

**That moved to [TF.State](src/state/README.md).**
