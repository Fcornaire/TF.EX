﻿using FortRise;
using Monocle;
using MonoMod.Utils;
using System.Net;
using System.Net.Sockets;
using TF.EX.Common.Extensions;
using TF.EX.Domain;
using TF.EX.Domain.CustomComponent;
using TF.EX.Domain.Models;
using TF.EX.Domain.Ports;
using TowerFall;

namespace TF.EX.Core
{
    internal static class TFCommands
    {
        [Command("test")]
        public static void LaunchTestMode(string[] args)
        {
            var mode = args.Length > 0 ? ParseMode(args[0]) : TowerFall.Modes.LastManStanding;
            var startLevel = args.Length > 1 ? Math.Min(int.Parse(args[1]), 9) : 0;
            var map = args.Length > 2 ? Math.Min(int.Parse(args[2]), 14) : 0;
            var seed = args.Length > 3 ? int.Parse(args[3]) : 42;
            var checkDistance = args.Length > 4 ? Math.Min(int.Parse(args[4]), 7) : 2;

            var logger = ServiceCollections.ResolveLogger();
            logger.LogDebug<Commands>($"Launching test mode with mode: {mode}, startLevel: {startLevel}, seed: {seed}, checkDistance: {checkDistance}");

            var netplayManager = ServiceCollections.ResolveNetplayManager();
            var replayService = ServiceCollections.ResolveReplayService();
            var rngService = ServiceCollections.ResolveRngService();

            if (netplayManager.HasSetMode())
            {
                TFGame.Instance.Commands.Log("You can't launch test mode while being in another netplay mode");
                return;
            }

            netplayManager.SetTestMode(checkDistance);

            rngService.SetSeed(seed);
            replayService.Initialize();

            for (int i = 0; i < 4; i++)
            {
                TFGame.Players[i] = TFGame.PlayerInputs[i] != null;
            }

            StartGame(mode, netplayManager, map, startLevel);

            TFGame.Instance.Commands.Open = false;
        }

        [Command("local")]
        public static void LaunchLocalNetplay(string[] args)
        {
            var mode = args.Length > 0 ? ParseMode(args[0]) : TowerFall.Modes.LastManStanding;
            var addr = args.Length > 1 ? args[1] : "";
            var startLevel = args.Length > 2 ? Math.Min(int.Parse(args[2]), 9) : 0;
            var draw = args.Length > 3 ? ParseDraw(args[3]) : PlayerDraw.Player1;
            var seed = args.Length > 4 ? int.Parse(args[4]) : 42;


            if (string.IsNullOrEmpty(addr))
            {
                TFGame.Instance.Commands.Log("You must specify an address in your local network (assuming the local port 7000 is available)");
                return;
            }

            if (IPAddress.TryParse(addr, out var ip))
            {
                if (ip.AddressFamily != AddressFamily.InterNetwork)
                {
                    TFGame.Instance.Commands.Log("You must specify an IPv4 address");
                    return;
                }
            }
            else
            {
                TFGame.Instance.Commands.Log("You must specify a valid IPv4 address");
                return;
            }

            addr = $"{addr}:7000";

            var netplayManager = ServiceCollections.ResolveNetplayManager();
            var replayService = ServiceCollections.ResolveReplayService();
            var rngService = ServiceCollections.ResolveRngService();

            if (netplayManager.HasSetMode())
            {
                TFGame.Instance.Commands.Log("You can't launch local mode while being in another netplay mode");
                return;
            }

            netplayManager.SetLocalMode(addr, draw);

            rngService.SetSeed(seed);
            replayService.Initialize();

            StartGame(mode, netplayManager, startLevel);

            TFGame.Instance.Commands.Open = false;
        }

        [Command("replay")]
        public static void LaunchReplay(string[] args)
        {
            var replayName = args.Length > 0 ? args[0] : "";

            if (string.IsNullOrEmpty(replayName))
            {
                TFGame.Instance.Commands.Log("You must specify a replay name");
                return;
            }

            replayName = $"{replayName}.tow";

            var netplayManager = ServiceCollections.ResolveNetplayManager();
            var replayService = ServiceCollections.ResolveReplayService();
            Music.Stop();
            Sounds.ui_mapZoom.Play();

            Task.Run(async () =>
            {
                TFGame.Instance.Commands.Open = false;

                var loader = new Loader(true);
                Loader.Message = "LOADING REPLAY...";
                TFGame.Instance.Scene.Add(new Fader());
                TFGame.Instance.Scene.Add(loader);

                await replayService.LoadAndStart(replayName);

                netplayManager.SetReplayMode();

                TFGame.Instance.Scene.Remove(loader);
            });
        }

        [Command("online")]
        public static void LaunchServerNetplay(string[] args)
        {
            var mode = args.Length > 0 ? ParseMode(args[0]) : TowerFall.Modes.LastManStanding;
            var roomUrl = args.Length > 1 ? args[1] : "";
            var seed = args.Length > 2 ? int.Parse(args[2]) : 42;

            if (string.IsNullOrEmpty(roomUrl))
            {
                TFGame.Instance.Commands.Log("You must specify a room url (Server + roomId)");
                return;
            }

            var netplayManager = ServiceCollections.ResolveNetplayManager();
            var replayService = ServiceCollections.ResolveReplayService();
            var rngService = ServiceCollections.ResolveRngService();


            if (netplayManager.HasSetMode())
            {
                TFGame.Instance.Commands.Log("You can't launch online mode while being in another netplay mode");
                return;
            }

            rngService.SetSeed(seed);
            netplayManager.SetServerMode(roomUrl);
            replayService.Initialize();

            StartGame(mode, netplayManager, 0);

            TFGame.Instance.Commands.Open = false;
        }

        [Command("menu")]
        public static void SwitchToMenu(string[] args)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();

            netplayManager.ResetMode();

            TFGame.Instance.Scene = new MainMenu(MainMenu.MenuState.Main);

            TFGame.Instance.Commands.Open = false;
        }

        [Command("vs")]
        public static void LaunchVersus(string[] args)
        {
            var mode = args.Length > 0 ? ParseMode(args[0]) : TowerFall.Modes.LastManStanding;
            var startLevel = args.Length > 1 ? Math.Min(int.Parse(args[1]), 9) : 0;
            var map = args.Length > 2 ? Math.Min(int.Parse(args[2]), 14) : 0;
            var seed = args.Length > 3 ? int.Parse(args[3]) : 42;

            var logger = ServiceCollections.ResolveLogger();
            logger.LogDebug<Commands>($"Launching versus mode with mode: {mode}, startLevel: {startLevel}, seed: {seed}");

            var replayService = ServiceCollections.ResolveReplayService();
            var rngService = ServiceCollections.ResolveRngService();

            rngService.SetSeed(seed);
            replayService.Initialize();

            for (int i = 0; i < 4; i++)
            {
                TFGame.Players[i] = TFGame.PlayerInputs[i] != null;
            }

            StartGame(mode, null, map, startLevel);

            TFGame.Instance.Commands.Open = false;
        }

        private static TowerFall.Modes ParseMode(string arg)
        {
            switch (arg.ToUpper())
            {
                case "LMS": return TowerFall.Modes.LastManStanding;
                case "HH": return TowerFall.Modes.HeadHunters;
                case "TDM": return TowerFall.Modes.TeamDeathmatch;
                default: return TowerFall.Modes.LastManStanding;
            }
        }

        private static PlayerDraw ParseDraw(string arg)
        {
            switch (arg)
            {
                case "P1": return PlayerDraw.Player1;
                case "P2": return PlayerDraw.Player2;
                default: return PlayerDraw.Player1;
            }
        }

        private static void StartGame(TowerFall.Modes mode, INetplayManager netplayManager = null, int map = 0, int startLevel = 0)
        {
            for (int i = 0; i < 4; i++)
            {
                TFGame.Players[i] = TFGame.PlayerInputs[i] != null;
            }

            MatchSettings matchSettings = MatchSettings.GetDefaultVersus();
            matchSettings.LevelSystem = GameData.VersusTowers[map].GetLevelSystem();
            matchSettings.Mode = mode;

            var levels = (matchSettings.LevelSystem as VersusLevelSystem).OwnGenLevel(matchSettings, GameData.VersusTowers[map], null, ServiceCollections.ResolveRngService());
            var dynVersusLevelSystem = DynamicData.For(matchSettings.LevelSystem);
            dynVersusLevelSystem.Set("levels", levels);
            (matchSettings.LevelSystem as VersusLevelSystem).StartOnLevel(startLevel);

            var session = new Session(matchSettings);

            if (netplayManager != null)
            {
                netplayManager.Init(session.RoundLogic);
            }

            session.StartGame();
        }
    }
}
