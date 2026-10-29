using Microsoft.Xna.Framework;
using Monocle;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models;
using TowerFall;

namespace TF.EX.Domain.CustomComponent
{
    public class LocalPlayersPanel : Entity
    {
        private const float PANEL_X = 24f;
        private const float PANEL_Y = 150f;
        private const float HINTS_RIGHT_X = 316f;
        private const float SLOT_SPACING = 28f;
        private const float LABEL_OFFSET = 12f;
        private const float ICON_OFFSET = 12f;
        private const float HINT_LINE_OFFSET = 6f;
        private const float GAP = 3f;
        private const string JOIN_HINT = "JOIN";
        private const string SWAP_HINT = "SWAP";
        private const string QUIT_HINT = "QUIT";

        private static readonly Color GuestColor = Calc.HexToColor("FFDC6B");

        private static LocalPlayersPanel current;

        private readonly MainMenu menu;
        private float playersAlpha;
        private float hintsAlpha;

        private LocalPlayersPanel(MainMenu menu) : base(new Vector2(PANEL_X, PANEL_Y))
        {
            this.menu = menu;
        }

        public static void Ensure(MainMenu menu)
        {
            if (current?.menu == menu)
            {
                return;
            }

            current = new LocalPlayersPanel(menu);
            menu.Add(current);
        }

        public static bool CanJoin(MainMenu menu)
        {
            return menu.State.ToDomainModel() == MenuState.NetplaySelect && menu.CanAct;
        }

        public override void Update()
        {
            base.Update();

            var inputService = ServiceCollections.ResolveInputService();
            var isNetplayScreen = menu.State.ToDomainModel().IsNetplayScreen() && ServiceCollections.ResolveMatchmakingService().GetOwnLobby().IsEmpty;
            var canJoin = isNetplayScreen && CanJoin(menu);
            var hasGuests = inputService.GetGuestInputs().Count > 0;
            var hasJoinable = canJoin && inputService.GetJoinableDevices().Any();

            playersAlpha = CalcApproach(playersAlpha, isNetplayScreen && (hasGuests || hasJoinable));
            hintsAlpha = CalcApproach(hintsAlpha, canJoin && (hasGuests || hasJoinable));
        }

        private static float CalcApproach(float alpha, bool isShown)
        {
            return Calc.Approach(alpha, isShown ? 1f : 0f, 0.08f * Engine.TimeMult);
        }

        public static void UpdateGuests(Ports.TF.IInputService inputService)
        {
            foreach (var guest in inputService.GetGuestInputs().ToList())
            {
                if (!guest.Attached || guest.MenuBack)
                {
                    inputService.RemoveGuestInput(guest);
                    Sounds.ui_clickBack.Play();
                }
            }

            var claiming = inputService.GetJoinableDevices().FirstOrDefault(input => input.MenuAlt2);

            if (claiming != null)
            {
                inputService.ClaimLocalInput(claiming);
                Sounds.ui_click.Play();
                return;
            }

            if (inputService.GetLocalPlayerCount() < Constants.MAX_LOCAL_PLAYERS)
            {
                var joining = inputService.GetJoinableDevices().FirstOrDefault(input => input.MenuStart);

                if (joining != null && inputService.TryAddGuestInput(joining))
                {
                    Sounds.ui_click.Play();
                }
            }
        }

        public override void Render()
        {
            var inputService = ServiceCollections.ResolveInputService();
            var guests = inputService.GetGuestInputs();

            if (playersAlpha > 0f)
            {
                RenderPlayers(inputService.GetLocalInput(), guests);
            }

            if (hintsAlpha > 0f)
            {
                RenderHints(inputService.GetJoinableDevices().ToList(), guests);
            }
        }

        private void RenderPlayers(PlayerInput owner, IReadOnlyList<PlayerInput> guests)
        {
            var y = Position.Y;

            DrawPlayer(1, owner?.Icon, Color.White, y);

            for (int i = 0; i < guests.Count; i++)
            {
                y += SLOT_SPACING;
                DrawPlayer(i + 2, guests[i].Icon, GuestColor, y);
            }
        }

        private void DrawPlayer(int number, Subtexture icon, Color color, float y)
        {
            Draw.OutlineTextCentered(TFGame.Font, $"P{number}", new Vector2(Position.X - LABEL_OFFSET, y), color * playersAlpha, Color.Black * playersAlpha);

            if (icon != null)
            {
                Draw.TextureCentered(icon, new Vector2(Position.X + ICON_OFFSET, y), Color.White * playersAlpha);
            }
        }

        private void RenderHints(List<PlayerInput> joinable, IReadOnlyList<PlayerInput> guests)
        {
            var y = Position.Y;

            if (joinable.Count > 0)
            {
                DrawHint("P1", DistinctTextures(joinable.Select(input => input.Alt2Icon)), SWAP_HINT, Color.Gray, y);
            }

            for (int seat = 2; seat <= Constants.MAX_LOCAL_PLAYERS; seat++)
            {
                y += SLOT_SPACING;

                var guestIndex = seat - 2;

                if (guestIndex < guests.Count)
                {
                    DrawHint($"P{seat}", [guests[guestIndex].BackIcon], QUIT_HINT, GuestColor, y);
                    continue;
                }

                if (guestIndex == guests.Count && joinable.Count > 0)
                {
                    DrawHint($"P{seat}", DistinctTextures(joinable.Select(input => input.StartIcon)), JOIN_HINT, Color.Gray, y);
                }
            }
        }

        private static List<Subtexture> DistinctTextures(IEnumerable<Subtexture> icons)
        {
            return [.. icons.Where(icon => icon != null).Distinct()];
        }

        private void DrawHint(string label, IReadOnlyList<Subtexture> icons, string action, Color color, float y)
        {
            var text = $"{label} {action}";
            var textWidth = TFGame.Font.MeasureString(text).X;
            Draw.OutlineTextCentered(TFGame.Font, text, new Vector2(HINTS_RIGHT_X - textWidth / 2f, y - HINT_LINE_OFFSET), color * hintsAlpha, Color.Black * hintsAlpha);

            var x = HINTS_RIGHT_X - icons.Sum(icon => icon.Width + GAP) + GAP;

            foreach (var icon in icons)
            {
                Draw.TextureCentered(icon, new Vector2(x + icon.Width / 2f, y + HINT_LINE_OFFSET), Color.White * hintsAlpha);
                x += icon.Width + GAP;
            }
        }
    }
}
