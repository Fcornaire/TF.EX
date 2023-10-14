# About Modded variant

EX has the ability to run custom variant as long as the mod is made compatible with EX.

It's your responsability to make sure the mod is netplay compatible (You should ask author)

# Making a custom variant compatible (For developper)

This guide will help you implement your modded variant for netplay session.

You can also look at this [PR](https://github.com/FortRise/ExampleFortRiseMod/pull/1) which make Jester Hat variant compatible with EX

## Prerequisites

There are 2 rules of thumb for making your mod compatbile:

- Your custom variant act deterministically. That's mean for a X frame, applying the same input result to the same Y frame, every time with no exception.

- Your are only saving/loading the custom part of your variant, that's mean you shouldn't try for example to save the players position because it's already made by EX. You should only focus of the things that are specific to your mod

## Context

Altought i recommend to have at least a basic knowledge of how rollback netcode work (there are some great explanation/video on the web), it's not mandatory for making the mod compatible.

EX already manage the work of saving/loading important piece of information (State), it now only need from your mod what should be saved/loaded.

It expose an interop API to be able to let your mod interact with it

```C#
public static Action<FortModule, string, Func<string>, Action<string>> RegisterVariantStateEvents;
```

This function take 4 parameters

- A FortModule(aka your core mod) used fo identification
- A string that is the name of the modded variant
- A function that return a string (SaveState)
- A function that take a string (LoadState)

Let's digest the latter 2 since there are the more important

- The SaveState delegate: a function that will return as string a serialized version of the custom variant state.

That mean it's your responsability to know what your custom variant add to the game.

- The LoadState delegate: a function that take a serialized version of the mod state and expect you to load the it

## Implementation

### Import the mod

This is straight forward. Create a class like this one

```C#
    [ModImportName("TF.EX.API")]
    public static class TfExAPIModImports
    {
        public static Action<FortModule, string, Func<string>, Action<string>> RegisterVariantStateEvents;

        static TfExAPIModImports()
        {
            typeof(TfExAPIModImports).ModInterop();
        }
    }
```

`[ModImportName("TF.EX.API")]` is for MonoMod to be able to find EX mod

`static TfExAPIModImports()` is just the constructor that will automatically make MonoMod find EX mod

### Register the custom Save/Load delegate

Call the precedent delegate with something like this

```C#
TfExAPIModImports.RegisterVariantStateEvents(this, "customVariantName", OnSaveState, OnLoadState);
```

with OnSaveState being your save state delegate and OnLoadState being your load state delegate

Do note the register function should be call after all mods finished loading, so not a the mod loading.
For now, you can do it using the FortRise event `FortRise.RiseCore.Events.OnPreInitialize`

## Test

You can test by launching EX on test mode which is a special mode that trigger a rollback every check_distance frame and check if the state on each frame are the same (equality by checksum)

For example, with a check_distance at 2, the game will rollback every 2 frame

If there is a checksum mismatch, ther will be an exception thrown that will show why the mismatch (DeepEqual.dll is needed for that!)

You can launch EX test mode by launching Towerfall, open the console by pressing ² and paste a command like

`test LMS 1 2 3 4 JESTERS_HAT`

with

- test : the mode we are lauching
- LMS : Last man standing
- 1 : the level where we should start
- 2 : the map where we should play
- 3 : the seed (for RNG) we want to apply
- 4 : the check distance , 2 to 7
- JESTERS_HAT: the name of the variant we want to test, note that this name is the title name of the variant with space replaced by underscore
