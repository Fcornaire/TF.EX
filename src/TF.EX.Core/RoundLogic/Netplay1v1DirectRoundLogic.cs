using TowerFall;

namespace TF.EX.Core.RoundLogic
{
    //TODO: Implement this
    [CustomRoundLogic("Netplay1v1DirectRoundLogic")]
    public class Netplay1v1DirectRoundLogic : CustomVersusRoundLogic
    {
        public Netplay1v1DirectRoundLogic(Session session, bool canHaveMiasma) : base(session, canHaveMiasma)
        {
        }

        public static RoundLogicInfo Create()
        {
            return new RoundLogicInfo
            {
                Name = "Netplay Direct",
                Icon = TFEXModModule.Atlas["gameModes/netplay"],
                RoundType = RoundLogicType.FFA
            };
        }
    }
}
