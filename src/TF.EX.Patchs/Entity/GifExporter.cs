using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System.Collections;
using TF.EX.Domain.Ports;
using TowerFall;

namespace TF.EX.Patchs.Entity
{
    public class GifExporterPatch : IHookable
    {
        private readonly INetplayManager netplayManager;

        public GifExporterPatch(INetplayManager netplayManager)
        {
            this.netplayManager = netplayManager;
        }

        public void Load()
        {
            On.TowerFall.GifExporter.ExportGIF += GifExporter_ExportGIF;
            On.TowerFall.GifExporter.Render += GifExporter_Render;
        }

        public void Unload()
        {
            On.TowerFall.GifExporter.ExportGIF -= GifExporter_ExportGIF;
            On.TowerFall.GifExporter.Render -= GifExporter_Render;
        }

        private void GifExporter_Render(On.TowerFall.GifExporter.orig_Render orig, GifExporter self)
        {
            if (netplayManager.IsDisconnected() && self.Scene is Level && self.Finished) //We are exporting a disconnected online game
            {
                MenuPanel.DrawPanel(100f, 142f, 120f, 60f);
                Draw.TextCentered(TFGame.Font, "SUCCESSFULLY EXPORTED", new Vector2(160f, 160f), Color.White);
                Draw.TextCentered(TFGame.Font, "LAST FRAMES FOR DEBUG", new Vector2(160f, 168f), Color.White);
                Draw.TextureCentered(TFGame.PlayerInputs[0].ConfirmIcon, new Vector2(160f, 182f), Color.White);
                return;
            }

            orig(self);
        }

        private IEnumerator GifExporter_ExportGIF(On.TowerFall.GifExporter.orig_ExportGIF orig, TowerFall.GifExporter self, string filename)
        {
            if (netplayManager.IsDisconnected() && self.Scene is Level) //We are exporting a disconnected online game
            {
                var dynExporter = DynamicData.For(self);
                var filePath = dynExporter.Get<string>("FilePath");

                GifExportOptions.Quality = 300;
                GifExportOptions.FrameRate = 60;
                GifExportOptions.Scale = 2;

                return orig(self, filePath);
            }

            return orig(self, filename);
        }
    }
}
