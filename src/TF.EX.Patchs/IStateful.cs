namespace TF.EX.Patchs
{
    public interface IStateful<S, T>
    {
        T GetState(S entity);
        void LoadState(T toLoad, S entity);
    }
}
