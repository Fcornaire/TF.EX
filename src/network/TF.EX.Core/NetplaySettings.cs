using System;
using FortRise;
using Microsoft.Extensions.Logging;
using TF.EX.Domain;
using TF.EX.Domain.Models;

namespace TF.EX
{
    public class NetplaySettings : ModuleSettings
    {
        private const int MinInputDelay = 0;
        private const int MaxInputDelay = 10;
        private const int MaxNameLength = 10;

        public override void Create(ISettingsCreate settings)
        {
            var meta = TryGetMeta();

            settings.CreateNumber("INPUT DELAY", meta?.InputDelay ?? 2,
                value => Update(m => m.InputDelay = value),
                MinInputDelay, MaxInputDelay);

            settings.CreateInput("NETPLAY NAME", meta?.Name ?? "PLAYER",
                value =>
                {
                    var name = (value ?? "").ToUpperInvariant();

                    Update(m => m.Name = name.Substring(0, System.Math.Min(name.Length, MaxNameLength)));
                },
                InputBehavior.None);
        }

        private static NetplayMeta TryGetMeta()
        {
            try
            {
                return ServiceCollections.ResolveNetplayManager()?.GetNetplayMeta();
            }
            catch (Exception e)
            {
                ServiceCollections.ResolveLogger()?.LogError(e, "Could not read the netplay config");
                return null;
            }
        }

        private static void Update(Action<NetplayMeta> change)
        {
            try
            {
                var netplayManager = ServiceCollections.ResolveNetplayManager();
                var meta = netplayManager.GetNetplayMeta();

                change(meta);

                netplayManager.UpdateMeta(meta);
                netplayManager.SaveConfig();
            }
            catch (Exception e)
            {
                ServiceCollections.ResolveLogger()?.LogError(e, "Could not save the netplay config");
            }
        }
    }
}
