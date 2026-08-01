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
                var recordedMode = ServiceCollections.ResolveReplayService().GetLoadedReplayMode();

                if (recordedMode >= 0)
                {
                    return (TowerFall.Modes)recordedMode;
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

        public override void OnLevelLoadFinish()
        {
            base.OnLevelLoadFinish();

            if (netplayManager.IsReplayMode())
            {
                if (base.Session.RoundIndex > 0)
                {
                    SpawnRoundPlayers();
                }

                return;
            }

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
            SpawnRoundPlayersOnly();
        }

        public void SpawnRoundPlayersOnly()
        {
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

        private bool IsHeadHunters => mode == TowerFall.Modes.HeadHunters;


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

            if (IsHeadHunters)
            {
                HandleHeadHuntersUpdate();
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

        private void HandleHeadHuntersUpdate()
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
                AddScore(base.Session.CurrentLevel.Player.PlayerIndex, 1);
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

            if (IsHeadHunters)
            {
                HandleHeadHuntersPlayerDeath(corpse, playerIndex, killerIndex);
                return;
            }

            HandleLastManStandingPlayerDeath(player, corpse, playerIndex, deathType, position, killerIndex);
        }

        private void HandleHeadHuntersPlayerDeath(PlayerCorpse corpse, int playerIndex, int killerIndex)
        {
            if (killerIndex == playerIndex || killerIndex == -1)
            {
                AddScore(playerIndex, -1);
            }
            else
            {
                AddScore(killerIndex, 1);
            }

            var winner = base.Session.GetWinner();

            if (wasFinalKill && winner == -1)
            {
                wasFinalKill = false;
                base.Session.CurrentLevel.Ending = false;
                CancelFinalKill();
                roundEndCounter.Reset();
            }

            if (FFACheckForAllButOneDead())
            {
                base.Session.CurrentLevel.Ending = true;

                if (winner != -1 && !wasFinalKill)
                {
                    wasFinalKill = true;
                    FinalKill(corpse, winner);
                }

                return;
            }

            if (!wasFinalKill && winner != -1 && !OtherPlayerCouldWin(winner))
            {
                base.Session.CurrentLevel.Ending = true;
                wasFinalKill = true;
                FinalKill(corpse, winner);
            }
        }

        private bool OtherPlayerCouldWin(int playerIndex)
        {
            if (base.Session.Scores[playerIndex] < base.Session.MatchSettings.GoalScore
                || base.Session.GetScoreLead(playerIndex) <= 0)
            {
                return true;
            }

            var reachable = base.Session.GetHighestScore() - (base.Session.CurrentLevel.LivingPlayers - 1);

            for (int i = 0; i < 4; i++)
            {
                if (!TFGame.Players[i] || i == playerIndex)
                {
                    continue;
                }

                var other = base.Session.CurrentLevel.GetPlayer(i);

                if (other != null && !other.Dead && base.Session.Scores[i] >= reachable)
                {
                    return true;
                }
            }

            return false;
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
