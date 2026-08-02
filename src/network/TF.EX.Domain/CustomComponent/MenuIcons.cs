using Monocle;

namespace TF.EX.Domain.CustomComponent
{
    public static class MenuIcons
    {
        private static Func<Subtexture> _online;

        public static void ConfigureOnline(Func<Subtexture> online)
        {
            _online = online;
        }

        public static Subtexture Online()
        {
            try
            {
                return _online?.Invoke() ?? TowerFall.TFGame.Atlas["versus/introArrow"];
            }
            catch
            {
                return TowerFall.TFGame.Atlas["versus/introArrow"];
            }
        }
    }
}
