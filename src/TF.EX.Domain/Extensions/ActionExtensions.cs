namespace TF.EX.Domain.Extensions
{
    public static class ActionExtensions
    {
        public static List<string> ToNames(this List<Action> actions)
        {
            return actions.Select(x => x.Method.Name).ToList();
        }
    }
}
