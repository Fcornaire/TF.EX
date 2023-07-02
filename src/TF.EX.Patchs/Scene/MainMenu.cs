using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using TF.EX.Domain;
using TF.EX.Domain.Ports;
using TowerFall;

namespace TF.EX.Patchs.Scene
{
    public class MainMenuPatch : IHookable
    {
        private readonly IMatchmakingService _matchmakingService;
        private readonly INetplayManager _netplayManager;
        private readonly IReplayService _replayService;

        private bool _hadAddedReplayButton = false;
        private OptionsButton _name;

        private BladeButton replayButton = null;
        private List<BladeButton> replays = new List<BladeButton>();

        public MainMenuPatch(IMatchmakingService matchmakingService, INetplayManager netplayManager, IReplayService replayService)
        {
            _matchmakingService = matchmakingService;
            _netplayManager = netplayManager;
            _replayService = replayService;
        }

        public void Load()
        {
            On.TowerFall.MainMenu.Update += MainMenu_Update;
            On.TowerFall.MainMenu.InitOptions += MainMenu_InitOptions;
            On.TowerFall.MainMenu.Render += MainMenu_Render;
            On.TowerFall.MainMenu.ctor += MainMenu_ctor;
        }

        public void Unload()
        {
            On.TowerFall.MainMenu.Update -= MainMenu_Update;
            On.TowerFall.MainMenu.InitOptions -= MainMenu_InitOptions;
            On.TowerFall.MainMenu.Render -= MainMenu_Render;
            On.TowerFall.MainMenu.ctor -= MainMenu_ctor;
        }

        private void MainMenu_ctor(On.TowerFall.MainMenu.orig_ctor orig, MainMenu self, MainMenu.MenuState state)
        {
            orig(self, state);

            TowerFall.TFGame.ConsoleEnabled = true;
            SaveData.Instance.Options.DevConsole = true;
        }

        private void MainMenu_Render(On.TowerFall.MainMenu.orig_Render orig, TowerFall.MainMenu self)
        {
            orig(self);

            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);


            if (self.State == TowerFall.MainMenu.MenuState.VersusOptions)
            {
                (_, var mode) = ServiceCollections.ResolveStateMachineService();

                if (TowerFall.MainMenu.VersusMatchSettings != null
                && mode == Domain.Models.Modes.Netplay1v1QuickPlay
                && _matchmakingService.IsConnectedToServer())
                {
                    Draw.OutlineTextCentered(TFGame.Font, $"{_matchmakingService.GetTotalAvailablePlayersInQuickPlayQueue()} PLAYERS", new Vector2(35f, 8f), Color.Aqua, 1f);
                }
            }

            Draw.SpriteBatch.End();
        }

        private void MainMenu_InitOptions(On.TowerFall.MainMenu.orig_InitOptions orig, TowerFall.MainMenu self, List<OptionsButton> buttons)
        {
            OptionsButton inputDelay = new OptionsButton("NETPLAY INPUT DELAY");
            inputDelay.SetCallbacks(inputDelay.InputDelayState, InputDelayRightCallback, InputDelayLeftCallback, null);
            buttons.Insert(0, inputDelay);

            _name = new OptionsButton("NETPLAY NAME");
            _name.SetCallbacks(_name.NameState, null, null, null);
            buttons.Insert(1, _name);

            orig(self, buttons);
        }

        public void InputDelayRightCallback()
        {
            var config = _netplayManager.GetConfig();
            config.InputDelay--;
            _netplayManager.UpdateConfig(config);
            _netplayManager.SaveConfig();
        }

        public void InputDelayLeftCallback()
        {
            var config = _netplayManager.GetConfig();
            config.InputDelay++;
            _netplayManager.UpdateConfig(config);
            _netplayManager.SaveConfig();
        }

        private void MainMenu_Update(On.TowerFall.MainMenu.orig_Update orig, TowerFall.MainMenu self)
        {
            if (_name != null && _name.State != _netplayManager.GetConfig().Name)
            {
                _name.State = _netplayManager.GetConfig().Name;
            }

            if (TowerFall.MainMenu.VersusMatchSettings != null && self.State == TowerFall.MainMenu.MenuState.VersusOptions)
            {
                if (TFGame.PlayerInputs[0] != null && TFGame.PlayerInputs[0].MenuBack)
                {
                    if (!_matchmakingService.HasRegisteredForQuickPlay())
                    {
                        self.State = TowerFall.MainMenu.MenuState.Main;
                    }
                    return;
                }
            }

            orig(self);
        }
    }

    public static class OptionsButtonExtensions
    {
        public static void InputDelayState(this OptionsButton optionsButton)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();
            optionsButton.State = netplayManager.GetConfig().InputDelay.ToString();
            optionsButton.CanLeft = netplayManager.GetConfig().InputDelay > 1;
            optionsButton.CanRight = netplayManager.GetConfig().InputDelay < 20;
        }

        public static void NameState(this OptionsButton optionsButton)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();
            optionsButton.State = netplayManager.GetConfig().Name.ToString();
        }
    }

}
