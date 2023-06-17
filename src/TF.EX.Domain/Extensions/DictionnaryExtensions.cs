namespace TF.EX.Domain.Extensions
{
    public static class DictionnaryExtensions
    {
        public static void Copy(this Dictionary<int, double> current, Dictionary<int, double> copy)
        {
            foreach (var depth_actualDepth in copy)
            {
                current.Add(depth_actualDepth.Key, depth_actualDepth.Value);
            }
        }
    }
}
