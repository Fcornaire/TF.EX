namespace TF.EX.Domain.Extensions
{
    public static class ListExtensions
    {
        public static void Copy<T>(this List<T> current, List<T> copy)
        {
            foreach (var item in copy)
            {
                current.Add(item);
            }
        }
    }
}
