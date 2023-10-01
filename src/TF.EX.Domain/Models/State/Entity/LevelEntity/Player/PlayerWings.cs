namespace TF.EX.Domain.Models.State.Entity.LevelEntity.Player
{
    public class PlayerWings
    {
        public Sprite<string> Wings { get; set; }
        public bool IsGaining { get; set; }
        public float SpriteScaleTweenTimer { get; set; }
    }
}
