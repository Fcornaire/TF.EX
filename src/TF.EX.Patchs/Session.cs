using Microsoft.Extensions.Logging;
using MonoMod.Utils;
using TF.EX.Common.Extensions;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Ports;
using TF.EX.Domain.Ports.TF;
using TF.EX.Patchs.Calc;
using TF.EX.TowerFallExtensions;
using TowerFall;

namespace TF.EX.Patchs
{
    public class SessionPatch : IHookable
    {
        private readonly ISessionService _sessionService;
        private readonly INetplayManager _netplayManager;
        private readonly IInputService _inputService;
        private readonly IRngService _rngService;
        private readonly ILogger _logger;

        public SessionPatch(ISessionService sessionService,
            INetplayManager netplayManager,
            IInputService inputService,
            IRngService rngService,
            ILogger logger)
        {
            _sessionService = sessionService;
            _netplayManager = netplayManager;
            _inputService = inputService;
            _rngService = rngService;
            _logger = logger;
        }

        public void Load()
        {
            On.TowerFall.Session.ctor += Session_ctor;
            On.TowerFall.Session.StartRound += Session_StartRound;
            On.TowerFall.Session.CreateResults += Session_CreateResults;
            On.TowerFall.Session.GetOldScore += Session_GetOldScore;
            On.TowerFall.Session.GetWinner += Session_GetWinner;
            On.TowerFall.Session.StartGame += Session_StartGame;
            On.TowerFall.Session.GotoNextRound += Session_GotoNextRound;
        }

        public void Unload()
        {
            On.TowerFall.Session.ctor -= Session_ctor;
            On.TowerFall.Session.StartRound -= Session_StartRound;
            On.TowerFall.Session.CreateResults -= Session_CreateResults;
            On.TowerFall.Session.GetOldScore -= Session_GetOldScore;
            On.TowerFall.Session.GetWinner -= Session_GetWinner;
            On.TowerFall.Session.StartGame -= Session_StartGame;
            On.TowerFall.Session.GotoNextRound -= Session_GotoNextRound;

        }

        /// <summary>
        /// Some hack since MatchResult is not tracked in the gamestate
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private void Session_GotoNextRound(On.TowerFall.Session.orig_GotoNextRound orig, Session self)
        {
            var mode = TowerFall.MainMenu.VersusMatchSettings.Mode.ToModel();

            if (mode.IsNetplay() && self.GetWinner() != -1)
            {
                _logger.LogDebug<Session>("Skipping GotoNextRound since game ended");

                var vsRoundResult = self.CurrentLevel.Get<VersusRoundResults>();
                var vsMatchResult = self.CurrentLevel.Get<VersusMatchResults>();

                if (vsRoundResult != null && vsMatchResult != null)
                {
                    _logger.LogDebug<Session>("Hack! Setting roundResults on matchResults");

                    vsRoundResult.MatchResults = vsMatchResult;
                    var dynMatchResult = DynamicData.For(vsMatchResult);
                    dynMatchResult.Set("roundResults", vsRoundResult);
                    self.CurrentLevel.Frozen = true;
                    ArcherData.Get(TFGame.Characters[self.GetWinner()], TFGame.AltSelect[self.GetWinner()]).PlayVictoryMusic();
                }
                return;
            }

            orig(self);
        }

        private void Session_StartGame(On.TowerFall.Session.orig_StartGame orig, Session self)
        {
            CalcPatch.Reset();
            _rngService.Reset();

            orig(self);
        }

        /// <summary>
        /// Some hack since MatchResult is not tracked in the gamestate
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private void Session_CreateResults(On.TowerFall.Session.orig_CreateResults orig, TowerFall.Session self)
        {
            var versusMatchResults = self.CurrentLevel.Get<VersusMatchResults>();

            if (versusMatchResults != null)
            {
                _logger.LogDebug<Session>("VersusMatchResults found, skipping CreateResults");
                versusMatchResults.TweenIn();

                ArcherData.Get(TFGame.Characters[self.GetWinner()], TFGame.AltSelect[self.GetWinner()]).PlayVictoryMusic();
                self.CurrentLevel.Frozen = true;
                return;
            }

            orig(self);
        }


        private int Session_GetWinner(On.TowerFall.Session.orig_GetWinner orig, TowerFall.Session self)
        {
            var originalWinner = orig(self);

            if (_netplayManager.IsInit() && _netplayManager.ShouldSwapPlayer())
            {
                if (originalWinner == 0)
                {
                    return _inputService.GetLocalPlayerInputIndex();
                }

                if (originalWinner == 1)
                {
                    return _inputService.GetRemotePlayerInputIndex();
                }
            }

            return originalWinner;
        }

        private int Session_GetOldScore(On.TowerFall.Session.orig_GetOldScore orig, TowerFall.Session self, int scoreIndex)
        {
            if ((_netplayManager.IsInit() || _netplayManager.IsTestMode()) && _netplayManager.ShouldSwapPlayer())
            {
                if (scoreIndex == 0)
                {
                    return orig(self, _inputService.GetLocalPlayerInputIndex());
                }

                if (scoreIndex == 1)
                {
                    return orig(self, _inputService.GetRemotePlayerInputIndex());

                }
            }
            return orig(self, scoreIndex);
        }

        private void Session_StartRound(On.TowerFall.Session.orig_StartRound orig, TowerFall.Session self)
        {
            orig(self);

            var session = _sessionService.GetSession();
            session.RoundStarted = true;
        }

        private void Session_ctor(On.TowerFall.Session.orig_ctor orig, TowerFall.Session self, TowerFall.MatchSettings settings)
        {
            orig(self, settings);
            //TODO: Methodes
            settings.Variants.DisableAll();
            settings.Variants.TournamentRules();

            self.MatchSettings = settings;
        }
    }
}
