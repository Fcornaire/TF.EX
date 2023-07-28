namespace TF.EX.TowerFallExtensions
{
    public static class SaveDataExtensions
    {
        public static void WithNetplayOptions(this TowerFall.SaveData self)
        {
            self.Options.DevConsole = true;
            self.Options.CanSkipReplays = true;
            self.Options.ReplayMode = TowerFall.Options.ReplayModes.UseGPU;
        }
    }
}
