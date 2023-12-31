﻿using MonoMod.Utils;
using TF.EX.Domain.Ports;
using TF.EX.Domain.Ports.TF;
using TF.EX.TowerFallExtensions.Layer;
using TowerFall;

namespace TF.EX.Patchs.Layer
{
    internal class LayerPatch : IHookable
    {
        private readonly INetplayManager _netplayManager;
        private readonly ISessionService _sessionService;
        private readonly IHUDService _hudService;

        public LayerPatch(INetplayManager netplayManager, IHUDService hudService, ISessionService sessionService)
        {
            _netplayManager = netplayManager;
            _hudService = hudService;
            _sessionService = sessionService;
        }

        public void Load()
        {
            On.Monocle.Layer.UpdateEntityList += Layer_UpdateEntityList;
            On.Monocle.Layer.Remove_Entity += Layer_Remove_Entity;
        }

        public void Unload()
        {
            On.Monocle.Layer.UpdateEntityList -= Layer_UpdateEntityList;
            On.Monocle.Layer.Remove_Entity -= Layer_Remove_Entity;
        }

        private void Layer_Remove_Entity(On.Monocle.Layer.orig_Remove_Entity orig, Monocle.Layer self, Monocle.Entity entity)
        {
            orig(self, entity);

            if (entity is VersusStart)
            {
                var hud = _hudService.Get();
                _hudService.Update(new Domain.Models.State.Entity.HUD.HUD
                {
                    VersusStart = new Domain.Models.State.Entity.HUD.VersusStart(),
                    VersusRoundResults = new Domain.Models.State.Entity.HUD.VersusRoundResults
                    {
                        CoroutineState = hud.VersusRoundResults.CoroutineState
                    }
                });
            }

            if (entity is VersusRoundResults)
            {
                var hud = _hudService.Get();
                _hudService.Update(new Domain.Models.State.Entity.HUD.HUD
                {
                    VersusStart = new Domain.Models.State.Entity.HUD.VersusStart
                    {
                        CoroutineState = hud.VersusStart.CoroutineState,
                        TweenState = hud.VersusStart.TweenState
                    },
                    VersusRoundResults = new Domain.Models.State.Entity.HUD.VersusRoundResults()
                });
            }


            if (entity is Miasma)
            {
                var session = _sessionService.GetSession();
                session.Miasma = TF.EX.Domain.Models.State.Miasma.Default(); //FIX
            }
        }

        private void Layer_UpdateEntityList(On.Monocle.Layer.orig_UpdateEntityList orig, Monocle.Layer self)
        {
            if (self.IsGameplayLayer())
            {
                if (_netplayManager.IsRollbackFrame() && !(TFGame.Instance.Scene is TowerFall.LevelLoaderXML)) //Remove entities from the precedent frame (but on a level only)
                {
                    var dynLayer = DynamicData.For(self);
                    var toAdd = dynLayer.Get<List<Monocle.Entity>>("toAdd");
                    var toRemove = dynLayer.Get<HashSet<Monocle.Entity>>("toRemove");
                    var toRemoveCache = dynLayer.Get<HashSet<Monocle.Entity>>("toRemoveCache");

                    toAdd.Clear();
                    toRemove.Clear();
                    toRemoveCache.Clear();
                }
            }

            orig(self);
        }
    }
}
