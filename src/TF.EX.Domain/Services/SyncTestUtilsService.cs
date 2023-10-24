using DeepEqual.Syntax;
using System.Text;
using TF.EX.Domain.Models.State;
using TF.EX.Domain.Ports;

namespace TF.EX.Domain.Services
{
    internal class SyncTestUtilsService : ISyncTestUtilsService
    {
        ICollection<(int, GameState)> frames_gameStates = new List<(int, GameState)>();

        public void AddFrame(int frame, GameState gs)
        {
            frames_gameStates.Add((frame, gs));
        }

        public string Compare(int frame)
        {
            StringBuilder msgBuilder = new StringBuilder();

            var gs = frames_gameStates.Where(x => x.Item1 == frame).ToList();

            for (var i = 0; i < gs.Count - 1; i++)
            {
                try
                {
                    gs[i].Item2.ShouldDeepEqual(gs[i + 1].Item2);
                }
                catch (DeepEqual.Syntax.DeepEqualException e)
                {
                    msgBuilder.Append($"{msgBuilder}Diff at frame {frame} : {e.Message} \n");
                }
            }

            return msgBuilder.ToString();
        }

        public void Remove(int fromFrame)
        {
            frames_gameStates = frames_gameStates.Where(x => x.Item1 >= fromFrame).ToList();
        }
    }
}
