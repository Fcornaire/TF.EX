namespace TF.EX.Domain.Extensions
{
    public static class ListExtensions
    {
        private static void Copy<T>(this List<T> current, List<T> copy)
        {
            foreach (var item in copy)
            {
                current.Add(item);
            }
        }

        public static void CopyInt(this List<int> current, List<int> copy)
        {
            current.Copy(copy);
        }

        public static void CopyFloat(this List<float> current, List<float> copy)
        {
            current.Copy(copy);
        }
    }
}
