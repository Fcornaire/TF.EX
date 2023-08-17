using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Monocle;
using TF.EX.Common.Extensions;
using TF.EX.Domain;
using TF.EX.Domain.Ports;
using TF.EX.Domain.Ports.TF;
using TowerFall;

namespace TF.EX.Core.RoundLogic
{
    //TODO: Refactor this class to a Netplay1v1RoundLogic
    [CustomRoundLogic("Netplay1v1DirectRoundLogic")]
    public class Netplay1v1DirectRoundLogic : CustomVersusRoundLogic
    {
        private RoundEndCounter roundEndCounter;

        private bool done;

        private bool wasFinalKill;

        private readonly INetplayManager _netplayManager;
        private readonly IInputService _inputInputService;
        private readonly IReplayService _replayService;
        private readonly ILogger _logger;

        public Netplay1v1DirectRoundLogic(Session session, bool canHaveMiasma) : base(session, true)
        {
            roundEndCounter = new RoundEndCounter(session);
            _netplayManager = ServiceCollections.ResolveNetplayManager();
            _inputInputService = ServiceCollections.ResolveInputService();
            _replayService = ServiceCollections.ResolveReplayService();
            _logger = ServiceCollections.ResolveLogger();
        }

        public static RoundLogicInfo Create()
        {
            return new RoundLogicInfo
            {
                Name = "Netplay Direct",
                Icon = TFEXModModule.Atlas["gameModes/netplay"],
                RoundType = RoundLogicType.FFA
            };
        }

        public override void OnLevelLoadFinish()
        {
            if (!_netplayManager.HaveFramesToReSimulate()) //Prevent adding a VersusStart on a rollback frame
            {
                base.OnLevelLoadFinish();

                if (!_netplayManager.IsInit())
                {
                    _logger.LogDebug<Netplay1v1QuickPlayRoundLogic>("Configuring and initializing current netplay session");
                    _netplayManager.Init(this);
                    _replayService.Initialize();

                    TowerFall.TFGame.ConsoleEnabled = false;
                }
                else
                {
                    base.Session.CurrentLevel.Add(new VersusStart(base.Session));
                    base.Players = SpawnPlayersFFA();
                }
            }
        }

        public override void OnRoundStart()
        {
            base.OnRoundStart();
            SpawnTreasureChestsVersus();
        }

        public override void OnUpdate()
        {
            SessionStats.TimePlayed += Engine.DeltaTicks;
            base.OnUpdate();
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

                if (_netplayManager.ShouldSwapPlayer())
                {
                    if (playerIndex == 0)
                    {
                        AddScore(_inputInputService.GetLocalPlayerInputIndex(), 1);
                    }
                    else
                    {
                        AddScore(_inputInputService.GetRemotePlayerInputIndex(), 1);
                    }
                }
                else
                {
                    AddScore(base.Session.CurrentLevel.Player.PlayerIndex, 1);
                }
            }

            InsertCrownEvent();
            base.Session.EndRound();
        }

        public override void OnPlayerDeath(Player player, PlayerCorpse corpse, int playerIndex, DeathCause deathType, Vector2 position, int killerIndex)
        {
            base.OnPlayerDeath(player, corpse, playerIndex, deathType, position, killerIndex);
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

                if (_netplayManager.ShouldSwapPlayer())
                {
                    if (num == 0)
                    {
                        num = _inputInputService.GetLocalPlayerInputIndex();
                    }
                    else
                    {
                        num = _inputInputService.GetRemotePlayerInputIndex();
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
