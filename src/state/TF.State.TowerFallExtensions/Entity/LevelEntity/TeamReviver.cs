using Monocle;
using MonoMod.Utils;
using TF.State.Domain.Extensions;
using TF.State.Domain.Models.Entity.LevelEntity;
using TF.State.TowerFallExtensions.ComponentExtensions;

namespace TF.State.TowerFallExtensions.Entity.LevelEntity
{
    public static class TeamReviverExtensions
    {
        public static TeamReviver GetState(this TowerFall.TeamReviver entity)
        {
            var dyn = DynamicData.For(entity);
            var corpse = entity.Corpse;
            var canReviveAlarm = entity.GetComponent<Alarm>();

            return new TeamReviver
            {
                ActualDepth = dyn.Get<double>("actualDepth"),
                Position = entity.Position.ToModel(),
                CorpseActualDepth = corpse != null ? DynamicData.For(corpse).Get<double>("actualDepth") : -1,
                Mode = (int)dyn.Get<TowerFall.TeamReviver.Modes>("Mode"),
                IsReviving = dyn.Get<bool>("reviving"),
                ReviveCounter = dyn.Get<float>("reviveCounter"),
                CanRevive = dyn.Get<bool>("canRevive"),
                Reviver = dyn.Get<int>("reviver"),
                Finished = entity.Finished,
                LevitateCorpse = dyn.Get<bool>("levitateCorpse"),
                PlayerCanRevive = entity.PlayerCanRevive,
                AutoRevive = entity.AutoRevive,
                IsRevivingHitbox = entity.Collider == dyn.Get<TowerFall.WrapHitbox>("revivingHitbox"),
                CanReviveAlarm = canReviveAlarm != null ? canReviveAlarm.GetState() : null,
                TargetPosition = dyn.Get<Microsoft.Xna.Framework.Vector2>("targetPosition").ToModel(),
                Sine = dyn.Get<SineWave>("sine").GetState(),
                ReviveSequenceCounter = dyn.Get<float>("reviveSequenceCounter"),
                ArrowSine = dyn.Get<SineWave>("arrowSine").GetState(),
            };
        }

        public static void LoadState(this TowerFall.TeamReviver entity, TeamReviver toLoad)
        {
            var dyn = DynamicData.For(entity);
            dyn.Set("Scene", TowerFall.TFGame.Instance.Scene);

            entity.Added();

            dyn.Set("actualDepth", toLoad.ActualDepth);
            entity.Position = toLoad.Position.ToTFVector();
            dyn.Set("reviving", toLoad.IsReviving);
            dyn.Set("reviveCounter", toLoad.ReviveCounter);
            dyn.Set("canRevive", toLoad.CanRevive);
            dyn.Set("reviver", toLoad.Reviver);
            dyn.Set("Finished", toLoad.Finished);
            dyn.Set("levitateCorpse", toLoad.LevitateCorpse);
            dyn.Set("PlayerCanRevive", toLoad.PlayerCanRevive);
            entity.AutoRevive = toLoad.AutoRevive;

            dyn.Set("reviveSequenceCounter", toLoad.ReviveSequenceCounter);
            dyn.Set("targetPosition", toLoad.TargetPosition.ToTFVector());

            entity.DeleteAllComponents<Coroutine>();

            if (toLoad.Sine != null)
            {
                dyn.Get<SineWave>("sine").LoadState(toLoad.Sine);
            }

            if (toLoad.ArrowSine != null)
            {
                dyn.Get<SineWave>("arrowSine").LoadState(toLoad.ArrowSine);
            }

            entity.Collider = toLoad.IsRevivingHitbox
                ? dyn.Get<TowerFall.WrapHitbox>("revivingHitbox")
                : dyn.Get<TowerFall.WrapHitbox>("normalHitbox");

            var canReviveAlarm = entity.GetComponent<Alarm>();
            if (toLoad.CanReviveAlarm != null)
            {
                canReviveAlarm ??= Alarm.Set(entity, toLoad.CanReviveAlarm.Duration,
                    () => DynamicData.For(entity).Set("canRevive", true));

                canReviveAlarm.LoadState(toLoad.CanReviveAlarm);
            }
            else if (canReviveAlarm != null)
            {
                entity.Remove(canReviveAlarm);
            }
        }
    }
}
