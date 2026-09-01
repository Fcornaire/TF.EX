using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using TF.State.Domain;

namespace TF.State.Patchs
{
    //For cross platform purpose
    // Math.Sin/Cos/Atan2/Pow will probably diverge Windows x Linuw, so every call reachable from tracked gameplay state is retargeted to DeterministicMath
    [HarmonyPatch]
    internal static class DeterministicMathPatch
    {
        private static readonly Dictionary<MethodInfo, MethodInfo> Replacements = new()
        {
            { typeof(Math).GetMethod(nameof(Math.Sin)), typeof(DeterministicMath).GetMethod(nameof(DeterministicMath.Sin)) },
            { typeof(Math).GetMethod(nameof(Math.Cos)), typeof(DeterministicMath).GetMethod(nameof(DeterministicMath.Cos)) },
            { typeof(Math).GetMethod(nameof(Math.Atan2)), typeof(DeterministicMath).GetMethod(nameof(DeterministicMath.Atan2)) },
            { typeof(Math).GetMethod(nameof(Math.Pow)), typeof(DeterministicMath).GetMethod(nameof(DeterministicMath.Pow)) },
        };

        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Monocle.Calc), nameof(Monocle.Calc.AngleToVector));
            yield return AccessTools.Method(typeof(Monocle.Calc), nameof(Monocle.Calc.Angle), new[] { typeof(Vector2), typeof(Vector2) });
            yield return AccessTools.Method(typeof(Monocle.Calc), nameof(Monocle.Calc.Angle), new[] { typeof(Vector2) });

            yield return AccessTools.Method(typeof(Monocle.SineWave), "Update");
            yield return AccessTools.Method(typeof(Monocle.SineWave), nameof(Monocle.SineWave.ValueOffset));
            yield return AccessTools.Method(typeof(Monocle.SineWave), nameof(Monocle.SineWave.StartUp));
            yield return AccessTools.Method(typeof(Monocle.SineWave), nameof(Monocle.SineWave.StartDown));
            yield return AccessTools.Method(typeof(Monocle.Wiggler), "Update");
            yield return AccessTools.Method(typeof(Monocle.Particle), "Update");

            yield return AccessTools.Method(typeof(TowerFall.Arrow), "ArrowUpdate");
            yield return AccessTools.Method(typeof(TowerFall.Player), "DodgingUpdate");

            yield return AccessTools.Method(typeof(TowerFall.LevelRandomGeometry), "GenerateData");
            yield return AccessTools.Method(typeof(TowerFall.LevelRandomItems), "AddItems");
            yield return AccessTools.Method(typeof(TowerFall.LevelRandomBGDetails), "GenCataclysm"); // FortRise renames the vanilla GenerateTileData body to GenCataclysm

            yield return Monocle.Ease.ExpoIn.Method;
            yield return Monocle.Ease.SineIn.Method;
            yield return Monocle.Ease.SineOut.Method;
            yield return Monocle.Ease.SineInOut.Method;
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (instruction.operand is MethodInfo callee && Replacements.TryGetValue(callee, out var replacement))
                {
                    instruction.operand = replacement;
                }
                yield return instruction;
            }
        }
    }
}
