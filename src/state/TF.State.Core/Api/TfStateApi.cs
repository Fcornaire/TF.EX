using MessagePack;
using TF.State.Domain;
using TF.State.Domain.Context;
using TF.State.Domain.Models;
using TF.State.Domain.Ports;
using TF.State.Patchs.Calc;
using TF.State.TowerFallExtensions;
using TowerFall;

namespace TF.State.Core.Api
{
    public class TfStateApi : ITfStateApi
    {
        public int ApiVersion => 1;

        public string GetStateSchemaVersion() => "TF.State/1"; //Bump whenever the GameState layout or StateSerialization options change.

        public bool IsStateAvailable() => TFGame.Instance?.Scene is Level;

        public byte[] GetGameStateBytes()
        {
            if (TFGame.Instance?.Scene is not Level level)
            {
                return null;
            }

            return MessagePackSerializer.Serialize(level.GetState(), StateSerialization.Options);
        }

        public byte[] GetGameStateBytesForRecording()
        {
            if (TFGame.Instance?.Scene is not Level level)
            {
                return null;
            }

            return MessagePackSerializer.Serialize(level.GetState(), StateSerialization.Options);
        }

        public bool LoadGameStateBytes(byte[] state)
        {
            if (state == null || TFGame.Instance?.Scene is not Level level)
            {
                return false;
            }

            var gameState = MessagePackSerializer.Deserialize<GameState>(state, StateSerialization.Options);

            StateFlags.IsRestoring = true;
            try
            {
                level.LoadState(gameState);
            }
            finally
            {
                StateFlags.IsRestoring = false;
            }

            return true;
        }

        public string[] ClassifyStateDiff(byte[] liveState, byte[] recordedState) => TF.State.Domain.StateDiff.Classify(liveState, recordedState);

        public bool StateMatchesWithFrame(byte[] candidate, byte[] liveState, int frame) => TF.State.Domain.StateDiff.MatchesWithFrame(candidate, liveState, frame);

        public void ResetRngOverride() => TF.State.Patchs.Calc.CalcPatch.Reset();

        public void StepOwnedControllers()
        {
            if (TFGame.Instance?.Scene is not Level level)
            {
                return;
            }

            TF.State.TowerFallExtensions.Entity.LevelEntity.MoonGlassBlockExplodeController.StepAll(level);
            TF.State.Patchs.DarkPortalsVariantSequencePatch.Step(level);
        }

        public bool IsTrialsOver()
        {
            if (TFGame.Instance?.Scene is not Level level)
            {
                return false;
            }

            var control = level.Get<TowerFall.TrialsControl>();

            if (control == null)
            {
                return false;
            }

            var state = TF.State.TowerFallExtensions.Entity.HUD.TrialsControlExtensions.GetState(control);

            return state.Started && !state.CanEnd;
        }

        public long GetTrialsTime()
        {
            if (TFGame.Instance?.Scene is not Level trialsLevel)
            {
                return 0;
            }

            var control = trialsLevel.Get<TowerFall.TrialsControl>();

            return control == null
                ? 0
                : TF.State.TowerFallExtensions.Entity.HUD.TrialsControlExtensions.GetState(control).Time;
        }

        public string DumpEntities() => TFGame.Instance?.Scene is Level level
                ? TF.State.TowerFallExtensions.EntityDumper.Dump(level)
                : null;

        public void GenerateVersusLevels(TowerFall.MatchSettings matchSettings, int mapId, int startLevel)
        {
            if (matchSettings?.LevelSystem is not VersusLevelSystem levelSystem)
            {
                return;
            }

            var levels = levelSystem.OwnGenLevel(matchSettings, GameData.VersusTowers[mapId], null, TF.State.Domain.ServiceCollections.ResolveRngService());

            HarmonyLib.Traverse.Create(levelSystem).Field("levels").SetValue(levels);
            levelSystem.StartOnLevel(startLevel);
        }

        public void SetVersusLevels(TowerFall.MatchSettings matchSettings, IEnumerable<string> levelPaths)
        {
            var levels = levelPaths?.ToList();

            if (levels == null || levels.Count == 0)
            {
                TF.State.Domain.Context.ScenarioLevels.Clear();

                return;
            }

            TF.State.Domain.Context.ScenarioLevels.Set(levels);

            if (matchSettings?.LevelSystem is not VersusLevelSystem levelSystem)
            {
                return;
            }

            HarmonyLib.Traverse.Create(levelSystem).Field("levels").SetValue(levels.ToList());
        }

        public string CompareStates(byte[] stateA, byte[] stateB) => TF.State.Domain.StateDiff.Compare(stateA, stateB);

        public string[] DescribePlayers(byte[] state) => TF.State.Domain.StateDiff.DescribePlayers(state);

        public string DescribeState(byte[] state, int maxDepth) => TF.State.Domain.StateDescriber.Describe(state, maxDepth);

        public int GetIntroCoroutineState(byte[] state) => TF.State.Domain.StateDiff.GetIntroCoroutineState(state);

        public bool IsRoundStarted(byte[] state) => TF.State.Domain.StateDiff.IsRoundStarted(state);

        public bool IsRoundResultsShown(byte[] state) => TF.State.Domain.StateDiff.IsRoundResultsShown(state);

        public int RestoreGameStateBytes(byte[] state)
        {
            if (state == null || TFGame.Instance?.Scene is not Level level)
            {
                return -1;
            }

            var gameState = MessagePackSerializer.Deserialize<GameState>(state, StateSerialization.Options);

            StateFlags.IsRestoring = true;
            try
            {
                level.LoadState(gameState);
            }
            finally
            {
                StateFlags.IsRestoring = false;
            }

            return gameState.Frame;
        }

        public byte[] CaptureGameState()
        {
            if (TFGame.Instance?.Scene is not Level level)
            {
                return null;
            }

            return MessagePackSerializer.Serialize(level.GetState(), StateSerialization.Options);
        }

        public int GetFrameOf(byte[] state)
        {
            if (state == null)
            {
                return -1;
            }

            return MessagePackSerializer.Deserialize<GameState>(state, StateSerialization.Options).Frame;
        }

        public void SetFrameDriver(string ownerModName) => StateFlags.FrameDriverOwner = ownerModName;

        public string GetFrameDriver() => StateFlags.FrameDriverOwner;

        public void SetDriverFlags(int currentFrame, bool isCaptureActive, bool isTestMode,
                                   bool isReplayMode, bool isRollbackFrame, double framesToReSimulate)
        {
            StateFlags.CurrentFrame = currentFrame;
            StateFlags.IsCaptureActive = isCaptureActive;
            StateFlags.IsTestMode = isTestMode;
            StateFlags.IsReplayMode = isReplayMode;
            StateFlags.IsRollbackFrame = isRollbackFrame;
            StateFlags.FramesToReSimulate = framesToReSimulate;
        }

        public void SetSfxCapture(bool active) => StateFlags.IsSfxCaptureActive = active;

        public void SetRestoring(bool value) => StateFlags.IsRestoring = value;

        public void SetCurrentFrame(int frame) => StateFlags.CurrentFrame = frame;

        public int GetCurrentFrame() => StateFlags.CurrentFrame;

        public void SynchronizeSfx(int currentFrame, bool isTestMode) => ServiceCollections.ResolveSFXService().Synchronize(currentFrame, isTestMode);

        public void ClearSfx() => ServiceCollections.ResolveSFXService().Clear();

        public void ResetSfx() => ServiceCollections.ResolveSFXService().Reset();

        public void ResetSession() => ServiceCollections.ResolveSessionService().Reset();

        public void SetSessionRoundStarted(bool value) => ServiceCollections.ResolveSessionService().GetSession().RoundStarted = value;

        public void SetSeed(int seed) => ServiceCollections.ResolveRngService().SetSeed(seed);

        public int GetSeed() => ServiceCollections.ResolveRngService().GetSeed();

        public void ResetRng() => ServiceCollections.ResolveRngService().Reset();

        public void RegisterRng() => CalcPatch.RegisterRng();

        public void UnregisterRng() => CalcPatch.UnregisterRng();

        public void ResetRound() => ServiceCollections.ResetState();

        public void ResetMatch() => ServiceCollections.ResolveStateContext().Reset();

        public void PurgeCache() => ServiceCollections.PurgeCache();

        public void RegisterStateEvents(string modName, string key, Func<byte[]> onSaveState, Action<byte[]> onLoadState) => ServiceCollections.ResolveAPIManager().RegisterStateEvents(modName, key, onSaveState, onLoadState);

        public void UnregisterStateEvents(string modName, string key) => ServiceCollections.ResolveAPIManager().UnregisterStateEvents(modName, key);


        public bool HasStateEvents(string id) => ServiceCollections.ResolveAPIManager().HasStateEvents(id);

        public bool IsTestMode() => StateFlags.IsTestMode;

    }
}
