using TF.EX.Domain.Models;
using TF.EX.Domain.Services.StateMachine;

namespace TF.EX.Domain.Extensions
{
    public static class ModeExtensions
    {
        public static Type ToType(this Modes mode)
        {
            switch (mode)
            {
                case Modes.Netplay1v1Direct:
                    return typeof(Netplay1V1DirectStateMachine);
                case Modes.Netplay1v1QuickPlay:
                    return typeof(Netplay1V1QuickPlayStateMachine);
                default:
                    return typeof(DefaultNetplayStateMachine);
            }
        }
    }
}
