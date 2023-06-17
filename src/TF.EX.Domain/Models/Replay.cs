using System.Collections.Generic;
using TF.EX.Domain.Models.State;

namespace TF.EX.Domain.Models
{
    public class Replay
    {
        public Info Informations { get; set; }

        public List<Record> Record { get; set; } = new List<Record>();

        public List<Record> Desynchs { get; set; } = new List<Record>();
    }

    public class Info
    {
        public int Id { get; set; }
        public PlayerDraw PlayerDraw { get; set; }

    }

    public class Record
    {
        public List<Input> Inputs { get; set; }

        public GameState GameState { get; set; }
    }

}
