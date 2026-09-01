using HarmonyLib;
using TF.EX.Domain.Interop;
using TF.EX.Domain.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.Utils;
using System.Xml;
using TF.EX.Domain;
using TF.EX.Domain.Context;
using TF.EX.Domain.Externals;
using TowerFall;

namespace TF.EX.Patchs.Scene
{
    [HarmonyPatch(typeof(Level))]
    public class LevelPatch
    {
        private const int HUD_LAYER = 4;
        private static Level clearedWaitingFor;

        private static Random random = new Random();

        private static readonly Action<float> SetEngineTimeMult = StaticFloatSetter(nameof(Monocle.Engine.TimeMult));
        private static readonly Action<float> SetEngineDeltaTime = StaticFloatSetter(nameof(Monocle.Engine.DeltaTime));

        private static Action<float> StaticFloatSetter(string property)
        {
            var setter = AccessTools.PropertySetter(typeof(Monocle.Engine), property);

            return setter == null ? null : (Action<float>)setter.CreateDelegate(typeof(Action<float>));
        }

        [HarmonyPrefix]
        [HarmonyPatch("HandlePausing")]
        public static void Level_HandlePausing()
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();
            if (netplayManager.IsInit() || netplayManager.IsReplayMode() || netplayManager.IsTestMode())
            {
                ServiceCollections.ResolveInputService().EnsureEveryControllerSlot();
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor)]
        [HarmonyPatch([typeof(Session), typeof(XmlElement)])]
        public static void Level_ctor(Level __instance)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();
            var mode = TowerFall.MainMenu.VersusMatchSettings?.Mode;

            if (__instance.ReplayRecorder != null
                && ((mode != null && mode.Value.IsNetplay()) || netplayManager.IsReplayMode() || netplayManager.IsTestMode()))
            {
                DynamicData.For(__instance).Set("ReplayRecorder", null);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        public static bool Level_Update__Prefix(Level __instance, out bool __state)
        {
            __state = IsAwaitingSynchronization();

            if (__state)
            {
                return false;
            }

            if (ExFlags.IsCaptureActive || ExFlags.HasFramesToReSimulate)
            {
                var actualDelta = (float)Monocle.Engine.Instance.TargetElapsedTime.TotalSeconds * TFGame.TimeRate;
                SetEngineTimeMult?.Invoke(actualDelta * 60f);
                SetEngineDeltaTime?.Invoke(actualDelta);
            }

            if (ExFlags.IsCaptureActive)
            {
                AddPlayersIndicators(__instance);
            }

            return true;
        }

        private static bool IsAwaitingSynchronization()
        {
            var mode = TowerFall.MainMenu.VersusMatchSettings?.Mode;

            if (mode == null || !mode.Value.IsNetplay())
            {
                return false;
            }

            var netplayManager = ServiceCollections.ResolveNetplayManager();

            return !netplayManager.IsSynchronized()
                && !netplayManager.IsReplayMode()
                && !netplayManager.IsTestMode()
                && !netplayManager.HasFailedInitialConnection()
                && !netplayManager.IsDisconnected();
        }

        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        public static void Level_Update__Postfix(Level __instance, bool __state)
        {
            if (__state)
            {
                return;
            }

            Domain.CustomComponent.Notification.FlushDeferred(__instance);

            var netplayManager = ServiceCollections.ResolveNetplayManager();

            ClearWaitingNotificationOnceSynchronized(__instance, netplayManager);

            netplayManager.SetIsRollbackFrame(false); //Mark the end of the First RBF

            if (ExFlags.IsCaptureActive)
            {
                //Moonglass shatter + DarkPortals sequence, TF.State owns the controllers and EX drives
                StateApi.Current.StepOwnedControllers();
            }

            var isStateDriven = ExFlags.IsCaptureActive
                || ExFlags.IsReplayMode
                || !string.IsNullOrEmpty(StateApi.Current.GetFrameDriver());

            if (isStateDriven)
            {
                UpdateLayersEntityList(__instance);
            }

            if (ExFlags.IsReplayMode)
            {
                netplayManager.UpdateFramesToReSimulate(0);
                StateApi.Current.SynchronizeSfx((int)__instance.FrameCounter, false);
            }
            else if (!ExFlags.HasFramesToReSimulate && netplayManager.IsSynchronized() && !netplayManager.IsDisconnected())
            {
                StateApi.Current.SynchronizeSfx(GGRSFFI.netplay_current_frame(), ExFlags.IsTestMode);
            }

            if (isStateDriven)
            {
                SkipLevelLoaderIfNeeded();
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("HandlePausing")]
        public static bool Level_HandlePausing(Level __instance)
        {
            if (ServiceCollections.ResolveNetplayManager().IsReplayMode())
            {
                return false;
            }

            var matchMakingService = ServiceCollections.ResolveMatchmakingService();

            var mode = TowerFall.MainMenu.VersusMatchSettings?.Mode;
            if (mode == null || !mode.Value.IsNetplay())
            {
                return true;
            }

            var lobby = matchMakingService.GetOwnLobby();
            if (lobby != null && (matchMakingService.IsLobbyReady() || !lobby.IsEmpty))
            {
                Domain.CustomComponent.LightPauseMenu.HandleStartPress(__instance);
                return false;
            }

            return true;
        }

        private static void ClearWaitingNotificationOnceSynchronized(Level level, Domain.Ports.INetplayManager netplayManager)
        {
            if (ReferenceEquals(clearedWaitingFor, level) || !netplayManager.IsSynchronized())
            {
                return;
            }

            clearedWaitingFor = level;

            Domain.CustomComponent.Notification.Clear(level, HUD_LAYER);
        }

        private static void UpdateLayersEntityList(Level level)
        {
            var gameplayLayer = level.GetGameplayLayer();
            var dynGameplayLayer = DynamicData.For(gameplayLayer);
            dynGameplayLayer.Invoke("UpdateEntityList");

            var versusStartLayer = level.GetHUDLayer();
            var dynVersusStartLayer = DynamicData.For(versusStartLayer);
            dynVersusStartLayer.Invoke("UpdateEntityList");
        }

        private static void AddPlayersIndicators(Level self)
        {
            var players = self[GameTags.Player].ToArray();

            float indicatorCounter = -1.0f;
            var nextDouble = -1.0f;

            foreach (TowerFall.Player player in players)
            {
                if (player.Indicator == null)
                {
                    if (nextDouble == -1.0f)
                    {
                        nextDouble = (float)random.NextDouble();
                    }

                    if (nextDouble <= 0.00025f)
                    {
                        if (indicatorCounter == -1.0f)
                        {
                            indicatorCounter = Monocle.Calc.Range(random, 500.0f, 200.0f);
                        }

                        var dynPlayer = DynamicData.For(player);
                        var indicator = new PlayerIndicator(new Vector2(0f, -8f), player.PlayerIndex, false);
                        var counter = DynamicData.For(DynamicData.For(indicator).Get<Counter>("showCounter"));
                        counter.Set("counter", indicatorCounter);

                        dynPlayer.Set("Indicator", indicator);

                        player.Add(indicator);
                    }
                }
            }
        }

        private static void RestorePauseMenuMusicVolume(Monocle.Scene oldScene)
        {
            var pauseMenu = (oldScene as Level)?.Get<PauseMenu>();

            if (pauseMenu != null)
            {
                Monocle.Music.MasterVolume = DynamicData.For(pauseMenu).Get<float>("oldMusicVolume");
            }
        }

        private static void SkipLevelLoaderIfNeeded()
        {
            var dynEngine = DynamicData.For(TowerFall.TFGame.Instance);
            var nextScene = dynEngine.Get<Monocle.Scene>("nextScene");
            if (nextScene is LevelLoaderXML)
            {
                RestorePauseMenuMusicVolume(dynEngine.Get<Monocle.Scene>("scene"));

                StateApi.Current.ClearSfx();
                dynEngine.Set("scene", dynEngine.Get<Monocle.Scene>("nextScene"));
                while (!(TFGame.Instance.Scene as LevelLoaderXML).Finished)
                {
                    TFGame.Instance.Scene.Update();
                }

                dynEngine.Set("scene", dynEngine.Get<Monocle.Scene>("nextScene"));
                TowerFall.TFGame.Instance.Scene.Begin();
                TowerFall.TFGame.Instance.Scene.Update();

                dynEngine.Set("FrameCounter", GGRSFFI.netplay_current_frame());
            }
        }
    }
}
