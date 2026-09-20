using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.EX.Domain.Models.WebSocket;
using TowerFall;

namespace TF.EX.Domain.CustomComponent
{
    public class SpectatorSeriesTally : Entity
    {
        private const int HUD_LAYER = 4;
        private const float TOP = 3f;
        private const float CENTER_X = 160f;

        private static readonly Color Gold = Calc.HexToColor("FFDC6B");
        private static readonly Vector2 Centered = new Vector2(0.5f, 0f);

        private SpectatorSeriesTally() : base(HUD_LAYER)
        {
            Position = Vector2.Zero;
            Depth = -2000000;
        }

        public static void Create(Level level)
        {
            var layer = level.Layers.Single(entry => entry.Key == HUD_LAYER).Value;

            if (layer.Entities.Any(entity => entity is SpectatorSeriesTally))
            {
                return;
            }

            var tally = new SpectatorSeriesTally();

            DynamicData.For(tally).Set("Scene", level);
            layer.Entities.Add(tally);
        }

        public override void Render()
        {
            base.Render();

            var lobby = ServiceCollections.ResolveMatchmakingService().GetOwnLobby();
            var series = lobby.Series;

            if (lobby.IsEmpty || series == null || series.Sides.Count < 2)
            {
                return;
            }

            var offset = ServiceCollections.ResolveWiderSetModApi()?.UIXOffset ?? 0f;
            var text = $"BEST OF {series.BestOf}   {NameOf(0, series, lobby)} {series.Wins(0)} - {series.Wins(1)} {NameOf(1, series, lobby)}   GAME {series.Games.Count + 1}";

            Draw.OutlineTextJustify(TFGame.Font, text, new Vector2(CENTER_X - offset, TOP), Gold, Color.Black, Centered);
        }

        private static string NameOf(int side, Series series, Lobby lobby)
        {
            if (series.Sides[side].Count > 1)
            {
                return side == 0 ? "BLUE" : "RED";
            }

            var seat = series.Sides[side].FirstOrDefault();

            return lobby.Players.FirstOrDefault(pl => pl.Seat == seat)?.Name ?? $"P{seat + 1}";
        }
    }
}
