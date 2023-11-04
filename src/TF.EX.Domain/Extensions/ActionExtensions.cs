using TF.EX.Domain.Models.State;

namespace TF.EX.Domain.Extensions
{
    public static class ActionExtensions
    {
        public static List<string> ToNames(this List<Action> actions)
        {
            return actions.Select(x =>
            {
                var name = x.Method.Name;
                switch (name)
                {
                    //Handling delegates methods, will crash if TF ever get updated
                    case "<.ctor>b__250_3":
                        return Constants.INVENTORY_INVISIBLE_DELEGATE;
                    case "<.ctor>b__250_2":
                        return Constants.INVENTORY_SHIELD_DELEGATE;
                    default:
                        return name;
                }
            }
            ).ToList();
        }
    }
}
