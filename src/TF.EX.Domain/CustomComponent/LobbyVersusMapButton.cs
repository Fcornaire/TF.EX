using Microsoft.Xna.Framework;
using Monocle;
using TF.EX.Domain.Models.State;
using TowerFall;

namespace TF.EX.Domain.CustomComponent
{
    public class LobbyVersusMapButton : LobbyBorderButton
    {
        private Image block;

        private Image icon;

        private OutlineText _title;

        public LobbyVersusMapButton(Vector2 position, Vector2 tweenFrom) : base(position, tweenFrom, 200, 30)
        {
            var imgs = MapButton.InitVersusGraphics(ownLobby.GameData.MapId);

            block = imgs[0];
            icon = imgs[1];

            Add(block);
            Add(icon);

            var tower = GameData.VersusTowers[ownLobby.GameData.MapId];

            _title = new OutlineText(TFGame.Font, tower.Theme.Name)
            {
                Scale = Vector2.One * 2f,
                Color = base.DrawColor,
                OutlineColor = Color.Black
            };

            _title.Position.Y += 20;

            Add(_title);
        }

        public override void Update()
        {
            base.Update();

            if (base.Selected)
            {
                if (MenuInput.Right)
                {
                    if (ownLobby.GameData.MapId + 1 >= Constants.NETPLAY_MAP_LIMIT)
                    {
                        MapButton.PlayTowerSound(MapButton.TowerType.Cataclysm);
                        return;
                    }

                    ownLobby.GameData.MapId = Math.Min(Constants.NETPLAY_MAP_LIMIT - 1, ownLobby.GameData.MapId + 1);
                    MapButton.PlayTowerSound(new TowerMapData(GameData.VersusTowers[ownLobby.GameData.MapId]).IconTile);
                }
                else if (MenuInput.Left)
                {
                    if (ownLobby.GameData.MapId - 1 < 0)
                    {
                        MapButton.PlayTowerSound(MapButton.TowerType.Cataclysm);
                        return;
                    }

                    ownLobby.GameData.MapId = Math.Max(0, ownLobby.GameData.MapId - 1);
                    MapButton.PlayTowerSound(new TowerMapData(GameData.VersusTowers[ownLobby.GameData.MapId]).IconTile);
                }
            }

            UpdateMapIcon();
            UpdateTitle();
            UpdateSide();
        }

        private void UpdateSide()
        {
            DrawRight = ownLobby.GameData.MapId < Math.Min(Constants.NETPLAY_MAP_LIMIT - 1, ownLobby.GameData.MapId + 1);
            DrawLeft = ownLobby.GameData.MapId > Math.Max(0, ownLobby.GameData.MapId - 1);
        }

        public override void Removed()
        {
            base.Removed();
            RemoveComponents();
        }

        private void RemoveComponents()
        {
            if (block != null)
            {
                block.RemoveSelf();
                Remove(block);
            }

            if (icon != null)
            {
                icon.RemoveSelf();
                Remove(icon);
            }

            if (_title != null)
            {
                _title.RemoveSelf();
                Remove(_title);
            }
        }

        private void UpdateMapIcon()
        {
            var imgs = MapButton.InitVersusGraphics(ownLobby.GameData.MapId);

            block.RemoveSelf();
            icon.RemoveSelf();

            block = imgs[0];
            icon = imgs[1];

            Add(block);
            Add(icon);
        }

        private void UpdateTitle()
        {
            var tower = GameData.VersusTowers[ownLobby.GameData.MapId];

            _title.RemoveSelf();

            _title = new OutlineText(TFGame.Font, tower.Theme.Name);
            _title.Scale = Vector2.One * 2f;
            _title.Color = base.DrawColor;
            _title.OutlineColor = Color.Black;

            _title.Position.Y += 20;

            Add(_title);
        }
    }
}
