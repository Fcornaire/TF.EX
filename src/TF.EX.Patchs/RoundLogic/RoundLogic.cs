using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models.State;
using TF.EX.Domain.Ports;
using TF.EX.Domain.Ports.TF;
using TF.EX.TowerFallExtensions;

namespace TF.EX.Patchs.RoundLogic
{
    public class RoundLogicPatch : IHookable
    {
        private readonly INetplayManager _netplayManager;
        private readonly ISessionService _sessionService;
        private readonly IRngService _rngService;
        private readonly IInputService _inputInputService;

        public RoundLogicPatch(INetplayManager netplayManager, ISessionService sessionService, IRngService rngService, IInputService inputInputService)
        {
            _netplayManager = netplayManager;
            _sessionService = sessionService;
            _rngService = rngService;
            _inputInputService = inputInputService;
        }

        public void Load()
        {
            On.TowerFall.RoundLogic.OnUpdate += RoundLogic_OnUpdate;
            On.TowerFall.RoundLogic.AddScore += RoundLogic_AddScore;
            On.TowerFall.RoundLogic.InsertCrownEvent += RoundLogic_InsertCrownEvent;
            On.TowerFall.RoundLogic.SpawnPlayersFFA += RoundLogic_SpawnPlayersFFA;
        }

        public void Unload()
        {
            On.TowerFall.RoundLogic.OnUpdate -= RoundLogic_OnUpdate;
            On.TowerFall.RoundLogic.AddScore -= RoundLogic_AddScore;
            On.TowerFall.RoundLogic.InsertCrownEvent -= RoundLogic_InsertCrownEvent;
            On.TowerFall.RoundLogic.SpawnPlayersFFA -= RoundLogic_SpawnPlayersFFA;
        }

        private int RoundLogic_SpawnPlayersFFA(On.TowerFall.RoundLogic.orig_SpawnPlayersFFA orig, TowerFall.RoundLogic self)
        {
            if (!_netplayManager.IsInit() && !_netplayManager.IsReplayMode())
            {
                return orig(self);
            }

            Vector2[] array = new Vector2[4];
            List<Vector2> xMLPositions = self.Session.CurrentLevel.GetXMLPositions("PlayerSpawn");

            _rngService.Get().ResetRandom();

            xMLPositions = CalcExtensions.OwnVectorShuffle(xMLPositions).ToList();
            int num;
            if (!self.Session.IsInOvertime)
            {
                num = TowerFall.TFGame.PlayerAmount;
            }
            else
            {
                int highestScore = self.Session.GetHighestScore();
                num = 0;
                for (int i = 0; i < 4; i++)
                {
                    if (TowerFall.TFGame.Players[i] && self.Session.Scores[i] == highestScore)
                    {
                        num++;
                    }
                }
            }

            int num2 = 0;
            var players = new List<TowerFall.Player>();
            for (int j = 0; j < 4; j++)
            {
                if (!self.Session.ShouldSpawn(j))
                {
                    continue;
                }

                if (num2 == 0 && num == 2 && xMLPositions[0].X != 160f)
                {
                    Vector2 vector = TowerFall.WrapMath.Opposite(xMLPositions[0]);
                    if (xMLPositions.Contains(vector))
                    {
                        xMLPositions[1] = vector;
                    }
                }

                int i = j;
                array[j] = xMLPositions[num2] + Vector2.UnitY * 2f;

                if (_netplayManager.ShouldSwapPlayer())
                {
                    i = j == 0 ? 1 : 0;
                    array[j] = xMLPositions.GetPositionByPlayerDraw(_netplayManager.ShouldSwapPlayer(), num2) + Vector2.UnitY * 2f;
                }

                TowerFall.Player entity = new TowerFall.Player(i, array[j], TowerFall.Allegiance.Neutral, TowerFall.Allegiance.Neutral, self.Session.GetPlayerInventory(j), self.Session.GetSpawnHatState(j), frozen: true, flash: true, indicator: true);

                players.Add(entity);

                num2++;
            }

            if (_netplayManager.ShouldSwapPlayer())
            {
                players.Reverse(); //TODO: Not true with more than 2 players
            }

            foreach (TowerFall.Player entity in players.ToArray())
            {
                self.Session.CurrentLevel.Add(entity);
            }

            return num;
        }

        private void RoundLogic_InsertCrownEvent(On.TowerFall.RoundLogic.orig_InsertCrownEvent orig, TowerFall.RoundLogic self)
        {
            bool[] array = new bool[self.Session.Scores.Length];

            if ((_netplayManager.IsInit() || _netplayManager.IsTestMode()) && _netplayManager.ShouldSwapPlayer())
            {
                for (int i = 0; i < array.Length; i++)
                {
                    if (i == 0)
                    {
                        array[_inputInputService.GetLocalPlayerInputIndex()] = self.Session.TeamHasCrown(i);
                    }

                    if (i == 1)
                    {
                        array[_inputInputService.GetRemotePlayerInputIndex()] = self.Session.TeamHasCrown(i);
                    }
                }
            }
            else
            {
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = self.Session.TeamHasCrown(i);
                }
            }

            self.Events.Add(new TowerFall.CrownChangeEvent(array));
        }

        private void RoundLogic_AddScore(On.TowerFall.RoundLogic.orig_AddScore orig, TowerFall.RoundLogic self, int scoreIndex, int add)
        {
            orig(self, scoreIndex, add);

            if ((_netplayManager.IsInit() || _netplayManager.IsTestMode()) && _netplayManager.ShouldSwapPlayer())
            {
                var evt = self.Events.Find(e => e is TowerFall.GainPointEvent && (e as TowerFall.GainPointEvent).ScoreIndex == scoreIndex);
                self.Events.Remove(evt);

                if (scoreIndex == 0)
                {
                    self.Events.Add(new TowerFall.GainPointEvent(_inputInputService.GetLocalPlayerInputIndex()));
                }

                if (scoreIndex == 1)
                {
                    self.Events.Add(new TowerFall.GainPointEvent(_inputInputService.GetRemotePlayerInputIndex()));
                }
            }
        }

        private void RoundLogic_OnUpdate(On.TowerFall.RoundLogic.orig_OnUpdate orig, TowerFall.RoundLogic self)
        {
            if (_netplayManager.IsInit())
            {
                var session = _sessionService.GetSession();
                LoadState(self, session);
            }

            orig(self);

            var dynamicRounlogic = DynamicData.For(self);
            var miasma = dynamicRounlogic.Get<TowerFall.Miasma>("miasma");

            if (_netplayManager.HaveFramesToReSimulate() && self.Session.CurrentLevel.Get<TowerFall.Miasma>() == null && miasma != null)
            {
                if (_netplayManager.IsRollbackFrame()) //We might be in the first RBF
                {
                    var dynMiasma = DynamicData.For(miasma);
                    dynMiasma.Set("Scene", self.Session.CurrentLevel);

                    self.Session.CurrentLevel.GetGameplayLayer().Entities.Add(miasma); //We manually add/tag the miasma
                    miasma.Added();
                    dynMiasma.Set("actualDepth", Constants.MIASMA_CUSTOM_DEPTH); //Setting the custom depth for sorting layer later
                }
            }
        }


        //TODO: Move to level LoadState method
        private void LoadState(TowerFall.RoundLogic self, TF.EX.Domain.Models.State.Session toLoad)
        {
            var dynamicRounlogic = DynamicData.For(self);
            var miasmaCounter = dynamicRounlogic.Get<Counter>("miasmaCounter");



            if (miasmaCounter.Value > 0)
            {
                dynamicRounlogic.Set("miasma", null);
            }
            else
            {
                dynamicRounlogic.Set("miasma", self.Session.CurrentLevel.Get<TowerFall.Miasma>());
            }
        }
    }
}
