using FortRise;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Monocle;
using TF.EX.Domain;
using TF.EX.Domain.Ports;
using TF.EX.Domain.Ports.TF;
using TowerFall;

namespace TF.EX.Core.RoundLogic
{
    public class NetplayVersusMode : IVersusGameMode
    {
        public string Name => "Netplay";
        public Color NameColor => Color.Yellow;
        public ISubtextureEntry Icon => TFEXModModule.InternetIcon;

        public bool IsTeamMode => ResolveMode() == TowerFall.Modes.TeamDeathmatch;

        public static TowerFall.Modes ResolveMode()
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();

            if (netplayManager.IsReplayMode())
            {
                var replay = ServiceCollections.ResolveReplayService().GetReplay();

                if (replay != null)
                {
                    return (TowerFall.Modes)(int)replay.Informations.Mode;
                }
            }

            var lobby = ServiceCollections.ResolveMatchmakingService().GetOwnLobby();

            return lobby.IsEmpty
                ? TowerFall.Modes.LastManStanding
                : (TowerFall.Modes)lobby.GameData.Mode;
        }

        public TowerFall.RoundLogic OnCreateRoundLogic(Session session)
        {
            return new NetplayRoundLogic(session);
        }

        public void OnStartGame(Session session)
        {
        }

        public int GetMinimumPlayers(MatchSettings matchSettings)
        {
            return 0;
        }
    }

    public class NetplayRoundLogic : TowerFall.RoundLogic, IRoundPlayerSpawner
    {
        private readonly INetplayManager netplayManager;
        private readonly IInputService inputInputService;
        private readonly IReplayService replayService;
        private readonly IMatchmakingService matchmakingService;
        private readonly ILogger logger;
        private TowerFall.Modes mode;

        private RoundEndCounter roundEndCounter;

        private bool done;

        private bool wasFinalKill;

        public NetplayRoundLogic(Session session) : base(session, true)
        {
            roundEndCounter = new RoundEndCounter(session);
            netplayManager = ServiceCollections.ResolveNetplayManager();
            inputInputService = ServiceCollections.ResolveInputService();
            replayService = ServiceCollections.ResolveReplayService();
            matchmakingService = ServiceCollections.ResolveMatchmakingService();
            logger = ServiceCollections.ResolveLogger();
            mode = NetplayVersusMode.ResolveMode();
        }

        //public static RoundLogicInfo Create()
        //{
        //    return new RoundLogicInfo
        //    {
        //        Name = "Netplay",
        //        Icon = TFEXModModule.Atlas["gameModes/netplay"],
        //        RoundType = RoundLogicType.FFA,
        //    };
        //}

        public override void OnLevelLoadFinish()
        {
            base.OnLevelLoadFinish();

            if (!netplayManager.IsInit())
            {
                logger.LogInformation("Configuring and initializing current netplay session");
                netplayManager.Init(this);

                var lobby = matchmakingService.GetOwnLobby();
                replayService.Initialize(lobby.GameData, lobby.Mods);

                TowerFall.TFGame.ConsoleEnabled = false;
            }
            else
            {
                SpawnRoundPlayers();
            }
        }

        public void SpawnRoundPlayers()
        {
            base.Session.CurrentLevel.Add(new VersusStart(base.Session));

            if (IsTeamMode)
            {
                SpawnPlayersTeams();
                base.Players = TFGame.PlayerAmount;
            }
            else
            {
                base.Players = SpawnPlayersFFA();
            }
        }

        private bool IsTeamMode => mode == TowerFall.Modes.TeamDeathmatch;


        public override void OnRoundStart()
        {
            base.OnRoundStart();
            SpawnTreasureChestsVersus();
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            if (IsTeamMode)
            {
                HandleTeamDeathmatchUpdate();
                return;
            }

            HandleLastManStandingUpdate();
        }

        private void HandleTeamDeathmatchUpdate()
        {
            SessionStats.TimePlayed += Engine.DeltaTicks;

            if (!base.RoundStarted || done || !base.Session.CurrentLevel.Ending || !base.Session.CurrentLevel.CanEnd)
            {
                return;
            }

            if (!roundEndCounter.Finished)
            {
                roundEndCounter.Update();
                return;
            }

            done = true;

            if (base.Session.CurrentLevel.Players.Count > 0)
            {
                AddScore((int)(base.Session.CurrentLevel.Players[0] as Player).Allegiance, 1);
            }

            InsertCrownEvent();
            base.Session.EndRound();
        }

        private void HandleLastManStandingUpdate()
        {
            SessionStats.TimePlayed += Engine.DeltaTicks;
            if (!base.RoundStarted || done || !base.Session.CurrentLevel.Ending || !base.Session.CurrentLevel.CanEnd)
            {
                return;
            }

            if (!roundEndCounter.Finished)
            {
                roundEndCounter.Update();
                return;
            }

            done = true;
            if (base.Session.CurrentLevel.Players.Count == 1)
            {
                var playerIndex = base.Session.CurrentLevel.Player.PlayerIndex;

                //if (netplayManager.ShouldSwapPlayer())
                //{
                //    if (playerIndex == 0)
                //    {
                //        logger.LogInformation($"Adding score to local player {inputInputService.GetLocalPlayerInputIndex()}");
                //        AddScore(inputInputService.GetLocalPlayerInputIndex(), 1);
                //    }
                //    else
                //    {
                //        logger.LogInformation($"Adding score to remote player {inputInputService.GetRemotePlayerInputIndex()}");
                //        AddScore(inputInputService.GetRemotePlayerInputIndex(), 1);
                //    }
                //}
                //else
                //{
                AddScore(base.Session.CurrentLevel.Player.PlayerIndex, 1);
                //}

            }
            InsertCrownEvent();
            base.Session.EndRound();
        }

        public override void OnPlayerDeath(Player player, PlayerCorpse corpse, int playerIndex, DeathCause deathType, Vector2 position, int killerIndex)
        {
            base.OnPlayerDeath(player, corpse, playerIndex, deathType, position, killerIndex);

            if (IsTeamMode)
            {
                HandleTeamDeathmatchPlayerDeath(corpse);
                return;
            }

            HandleLastManStandingPlayerDeath(player, corpse, playerIndex, deathType, position, killerIndex);
        }

        private void HandleTeamDeathmatchPlayerDeath(PlayerCorpse corpse)
        {
            if (wasFinalKill && base.Session.CurrentLevel.LivingPlayers == 0)
            {
                wasFinalKill = false;
                CancelFinalKill();
                return;
            }

            if (!TeamCheckForRoundOver(out var surviving))
            {
                return;
            }

            base.Session.CurrentLevel.Ending = true;

            if (surviving != Allegiance.Neutral && base.Session.Scores[(int)surviving] >= base.Session.MatchSettings.GoalScore - 1)
            {
                wasFinalKill = true;
                FinalKillTeams(corpse, surviving);
            }
        }

        public void HandleLastManStandingPlayerDeath(Player player, PlayerCorpse corpse, int playerIndex, DeathCause deathType, Vector2 position, int killerIndex)
        {
            if (wasFinalKill && base.Session.CurrentLevel.LivingPlayers == 0)
            {
                CancelFinalKill();
            }
            else
            {
                if (!FFACheckForAllButOneDead())
                {
                    return;
                }

                int num = -1;
                foreach (Player item in base.Session.CurrentLevel[GameTags.Player])
                {
                    if (!item.Dead)
                    {
                        num = item.PlayerIndex;
                        break;
                    }
                }

                //if (netplayManager.ShouldSwapPlayer())
                //{
                //    if (num == 0)
                //    {
                //        num = inputInputService.GetLocalPlayerInputIndex();
                //    }
                //    else
                //    {
                //        num = inputInputService.GetRemotePlayerInputIndex();
                //    }
                //}

                base.Session.CurrentLevel.Ending = true;
                if (num != -1 && base.Session.Scores[num] >= base.Session.MatchSettings.GoalScore - 1)
                {
                    wasFinalKill = true;
                    FinalKill(corpse, num);
                }
            }
        }
    }
}
