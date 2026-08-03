using Microsoft.Xna.Framework;
using TF.EX.Domain;
using TowerFall;

namespace TF.EX.Patchs.Entity.MenuItem
{
    internal static class RollcallLayout
    {
        private static readonly Vector2 PING = new Vector2(0f, -66f);
        private static readonly Vector2 NAME = new Vector2(0f, 105f);
        private static readonly Vector2 TEAM = new Vector2(0f, 114f);

        private static readonly Vector2 WIDE_PING = new Vector2(-16f, -40f);
        private static readonly Vector2 WIDE_NAME = new Vector2(0f, 40f);
        private static readonly Vector2 WIDE_TEAM = new Vector2(0f, -20f);

        public static bool IsWide => ServiceCollections.ResolveWiderSetModApi()?.IsWide ?? false;

        public static Vector2 PingAt(RollcallElement element) => element.Position + (IsWide ? WIDE_PING : PING);

        public static Vector2 NameAt(RollcallElement element) => element.Position + (IsWide ? WIDE_NAME : NAME);

        public static Vector2 TeamAt(RollcallElement element) => element.Position + (IsWide ? WIDE_TEAM : TEAM);
    }
}
