using Microsoft.Xna.Framework;
using Monocle;

namespace TF.EX.TowerFallExtensions.Entity.LevelEntity
{
    public static class MiasmaSequenceController
    {
        public const int ModeNormal = 0;
        public const int ModeAmaranthBoss = 1;
        public const int ModeCataclysmBoss = 2;

        public const int DirUnset = 0;

        public struct Result
        {
            public float Percent;
            public float SideWeight;
            public bool Collidable;
            public bool NervesOfSteelCheck;
        }

        public static bool ShouldRollAmaranthDir(int mode, float ticks, int currentDir)
        {
            return mode == ModeAmaranthBoss && currentDir == DirUnset && ticks >= 180f;
        }

        public static Result Evaluate(int mode, float ticks, int dir)
        {
            switch (mode)
            {
                case ModeAmaranthBoss:
                    return EvaluateAmaranth(ticks, dir);
                case ModeCataclysmBoss:
                    return EvaluateCataclysm(ticks);
                default:
                    return EvaluateNormal(ticks);
            }
        }

        private static Result EvaluateNormal(float ticks)
        {
            float percent;
            if (ticks < 540f)
            {
                percent = MathHelper.Lerp(-0.1f, 0.33f, Ease.CubeOut(ticks / 540f));
            }
            else if (ticks < 720f)
            {
                percent = MathHelper.Lerp(0.33f, 0.15f, Ease.CubeOut((ticks - 540f) / 180f));
            }
            else if (ticks < 1380f)
            {
                percent = MathHelper.Lerp(0.15f, 1.05f, Ease.CubeInOut((ticks - 720f) / 660f));
            }
            else if (ticks < 1620f)
            {
                percent = MathHelper.Lerp(1.05f, -0.1f, Ease.CubeIn((ticks - 1380f) / 240f));
            }
            else
            {
                percent = -0.1f;
            }

            return new Result
            {
                Percent = percent,
                SideWeight = 0f,
                Collidable = ticks >= 120f,
                NervesOfSteelCheck = ticks >= 720f,
            };
        }

        private static Result EvaluateCataclysm(float ticks)
        {
            float percent = ticks < 180f
                ? MathHelper.Lerp(-0.1f, 0.2f, Ease.CubeOut(ticks / 180f))
                : 0.2f;

            return new Result
            {
                Percent = percent,
                SideWeight = 0f,
                Collidable = ticks >= 120f,
                NervesOfSteelCheck = false,
            };
        }

        private static Result EvaluateAmaranth(float ticks, int dir)
        {
            float percent = ticks < 180f
                ? MathHelper.Lerp(-0.1f, 0.25f, Ease.CubeOut(ticks / 180f))
                : 0.25f;

            float sideWeight = 0f;
            if (ticks >= 180f && dir != DirUnset)
            {
                float u = (ticks - 180f) % 960f;
                float peak = 0.25f * dir;
                if (u < 240f)
                {
                    sideWeight = MathHelper.Lerp(0f, peak, Ease.CubeOut(u / 240f));
                }
                else if (u < 720f)
                {
                    sideWeight = MathHelper.Lerp(peak, -peak, Ease.CubeInOut((u - 240f) / 480f));
                }
                else
                {
                    sideWeight = MathHelper.Lerp(-peak, 0f, Ease.CubeIn((u - 720f) / 240f));
                }
            }

            return new Result
            {
                Percent = percent,
                SideWeight = sideWeight,
                Collidable = ticks >= 120f,
                NervesOfSteelCheck = false,
            };
        }

        public static void EvaluateDissipate(float ticks, float startPercent, float startSideWeight, out float percent, out float sideWeight, out bool shouldRemove)
        {
            float duration = System.Math.Max(20f, (int)(60f * startPercent));
            float eased = Ease.CubeInOut(MathHelper.Clamp(ticks / duration, 0f, 1f));
            percent = MathHelper.Lerp(startPercent, -0.1f, eased);
            sideWeight = MathHelper.Lerp(startSideWeight, 0f, eased);
            shouldRemove = ticks >= 100f;
        }
    }
}
