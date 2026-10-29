using TF.State.Domain.Models;

namespace TF.State.Domain.Extensions
{
    public static class ActionExtensions
    {
        public static List<string> ToNames(this List<Action> actions)
        {
            return [.. actions.Select(x =>
            {
                var name = x.Method.Name;

                //Handling delegates methods (250 windows , the other linux)
                return name switch
                {
                    "<.ctor>b__250_3" or "<.ctor>b__3" => Constants.INVENTORY_INVISIBLE_DELEGATE,
                    "<.ctor>b__250_2" or "<.ctor>b__2" => Constants.INVENTORY_SHIELD_DELEGATE,
                    _ => name,
                };
            }
            )];
        }
    }
}
