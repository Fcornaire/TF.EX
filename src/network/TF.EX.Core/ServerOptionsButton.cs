using FortRise;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.EX.Domain.Models;
using TowerFall;

namespace TF.EX
{
    //FortRise CreateInput has no reset, custom implem
    internal class ServerOptionsButton : OptionsButton
    {
        private readonly NetplaySettings settings;
        private bool showingGuides;

        public ServerOptionsButton(NetplaySettings settings, string description) : base("SERVER")
        {
            this.settings = settings;
            DynamicData.For(this).Set("fortrise_description", description);

            SetCallbacks(() => State = Display(NetplayPreferences.Server), null, null, () =>
            {
                var uiInput = new UIInputText(this, value =>
                {
                    Selected = true;
                    SetServer(value);
                }, new Vector2(0, 240 * 0.5f), -1, NetplayPreferences.Server);
                uiInput.LayerIndex = 0;

                MainMenu.Add(uiInput);
                Selected = false;
                return true;
            });
        }

        public override void Update()
        {
            base.Update();

            if (Selected && MainMenu != null)
            {
                if (!showingGuides)
                {
                    MainMenu.ButtonGuideA.SetDetails(MenuButtonGuide.ButtonModes.Alt, "RESET");
                    MainMenu.ButtonGuideB.SetDetails(MenuButtonGuide.ButtonModes.Alt2, "LOCAL");
                    showingGuides = true;
                }

                if (MenuInput.Alt)
                {
                    SetServer(NetplayPreferences.OfficialServer);
                    Sounds.ui_subclickOn.Play();
                }
                else if (MenuInput.Alt2)
                {
                    SetServer(NetplayPreferences.LocalServer);
                    Sounds.ui_subclickOn.Play();
                }
            }
            else if (showingGuides)
            {
                ClearGuides();
            }
        }

        public override void Removed()
        {
            if (showingGuides)
            {
                ClearGuides();
            }
            base.Removed();
        }

        private void SetServer(string value)
        {
            settings.Server = value;
            settings.Apply();
            State = Display(NetplayPreferences.Server);
        }

        private void ClearGuides()
        {
            if (MainMenu != null)
            {
                MainMenu.ButtonGuideA.Clear();
                MainMenu.ButtonGuideB.Clear();
            }
            showingGuides = false;
        }

        private static string Display(string server)
        {
            if (server == NetplayPreferences.OfficialServer)
            {
                return "OFFICIAL";
            }

            return NetplayPreferences.IsOfficial(server) ? "LOCAL" : "CUSTOM";
        }
    }
}
