using System;

namespace TF.EX.Domain.Models.State.Player
{

    public enum PlayerStates : int
    {
        Normal,
        LedgeGrab,
        Ducking,
        Dodging,
        Dying,
        Frozen
    }

    public static class PlayerStateExtensions
    {
        public static PlayerStates ToModel(this TowerFall.Player.PlayerStates playerStates)
        {
            switch (playerStates)
            {
                case TowerFall.Player.PlayerStates.Normal: return PlayerStates.Normal;
                case TowerFall.Player.PlayerStates.LedgeGrab: return PlayerStates.LedgeGrab;
                case TowerFall.Player.PlayerStates.Frozen: return PlayerStates.Frozen;
                case TowerFall.Player.PlayerStates.Ducking: return PlayerStates.Ducking;
                case TowerFall.Player.PlayerStates.Dying: return PlayerStates.Dying;
                case TowerFall.Player.PlayerStates.Dodging: return PlayerStates.Dodging;
            }
            throw new NotImplementedException($"Can't create a PlayerState for unkwon value {playerStates}");
        }

        public static TowerFall.Player.PlayerStates ToTFModel(this PlayerStates playerStates)
        {
            switch (playerStates)
            {
                case PlayerStates.Normal: return TowerFall.Player.PlayerStates.Normal;
                case PlayerStates.LedgeGrab: return TowerFall.Player.PlayerStates.LedgeGrab;
                case PlayerStates.Frozen: return TowerFall.Player.PlayerStates.Frozen;
                case PlayerStates.Ducking: return TowerFall.Player.PlayerStates.Ducking;
                case PlayerStates.Dying: return TowerFall.Player.PlayerStates.Dying;
                case PlayerStates.Dodging: return TowerFall.Player.PlayerStates.Dodging;
            }
            throw new NotImplementedException($"Can't create a Twoerfall PlayerState for unkwon value {playerStates}");
        }

    }
}
