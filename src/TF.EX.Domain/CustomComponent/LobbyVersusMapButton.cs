using Microsoft.Xna.Framework;
using Monocle;
using TF.EX.Domain.Models.State;
using TF.EX.Domain.Utils;
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
            UpdateMapIcon();
            UpdateTitle();
            UpdateSide();
        }

        public override void Update()
        {
            base.Update();

            if (base.Selected)
            {
                var limit = Constants.NETPLAY_SAFE_MAP.Count();

                if (MenuInput.Right)
                {
                    if (ownLobby.GameData.MapId + 1 >= limit)
                    {
                        MapButton.PlayTowerSound(MapButton.TowerType.Cataclysm);
                        return;
                    }

                    ownLobby.GameData.MapId = Math.Min(limit - 1, ownLobby.GameData.MapId + 1);
                    MapButton.PlayTowerSound(new TowerMapData(GameData.VersusTowers[ownLobby.GameData.MapId]).IconTile);
                }
                else if (MenuInput.Left)
                {
                    if (ownLobby.GameData.MapId - 1 < -1)
                    {
                        MapButton.PlayTowerSound(MapButton.TowerType.Cataclysm);
                        return;
                    }

                    ownLobby.GameData.MapId = Math.Max(-1, ownLobby.GameData.MapId - 1);

                    if (ownLobby.GameData.MapId == -1)
                    {
                        MapButton.PlayTowerSound(MapButton.TowerType.Random);
                    }
                    else
                    {
                        MapButton.PlayTowerSound(new TowerMapData(GameData.VersusTowers[ownLobby.GameData.MapId]).IconTile);
                    }
                }
            }

            UpdateMapIcon();
            UpdateTitle();
            UpdateSide();
        }

        private void UpdateSide()
        {
            DrawRight = ownLobby.GameData.MapId < Math.Min(Constants.NETPLAY_SAFE_MAP.Count() - 1, ownLobby.GameData.MapId + 1);
            DrawLeft = ownLobby.GameData.MapId > Math.Max(-1, ownLobby.GameData.MapId - 1);
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
            var imgs = ownLobby.GameData.MapId == -1 ? MapButton.InitRandomVersusGraphics() : VersusGraphics.GetVersusGraphics(ownLobby.GameData.MapId);

            if (block != null)
            {
                block.RemoveSelf();
            }

            if (icon != null)
            {
                icon.RemoveSelf();
            }

            block = imgs[0];
            icon = imgs[1];

            Add(block);
            Add(icon);
        }

        private void UpdateTitle()
        {
            var towerName = ownLobby.GameData.MapId == -1 ? "RANDOM" : GameData.VersusTowers[ownLobby.GameData.MapId].Theme.Name;

            if (_title != null)
            {
                _title.RemoveSelf();
            }

            _title = new OutlineText(TFGame.Font, towerName)
            {
                Scale = Vector2.One * 2f,
                Color = base.DrawColor,
                OutlineColor = Color.Black
            };

            _title.Position.Y += 20;

            Add(_title);
        }
    }
}
