namespace TF.EX.Patchs.Layer
{
    internal class ReplayViewerPatch : IHookable
    {
        public void Load()
        {
            On.TowerFall.ReplayViewer.Watch += ReplayViewer_Watch;
        }

        public void Unload()
        {
            On.TowerFall.ReplayViewer.Watch -= ReplayViewer_Watch;
        }

        /// <summary>
        /// Skip the replay viewer
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        /// <param name="recorder"></param>
        /// <param name="type"></param>
        /// <param name="onComplete"></param>
        private void ReplayViewer_Watch(On.TowerFall.ReplayViewer.orig_Watch orig, TowerFall.ReplayViewer self, TowerFall.ReplayRecorder recorder, TowerFall.ReplayViewer.ReplayType type, Action onComplete)
        {
            onComplete();
        }
    }
}
