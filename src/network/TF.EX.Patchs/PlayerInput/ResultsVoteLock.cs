namespace TF.EX.Patchs.PlayerInput
{
    internal static class ResultsVoteLock
    {
        public static bool IsActive()
        {
            return Entity.PauseMenuPatch.IsVotePending();
        }
    }
}
