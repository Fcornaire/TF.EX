using System.Text;
using TF.EX.Domain.Interop;
using TF.EX.Domain.Ports;

namespace TF.EX.Domain.Services
{
    internal class SyncTestUtilsService : ISyncTestUtilsService
    {
        ICollection<(int frame, byte[] bytes)> frames_gameStates = new List<(int, byte[])>();

        public void AddFrame(int frame, byte[] state)
        {
            frames_gameStates.Add((frame, state));
        }

        public string Compare(int frame)
        {
            var msgBuilder = new StringBuilder();
            var gs = frames_gameStates.Where(x => x.frame == frame).ToList();

            for (var i = 0; i < gs.Count - 1; i++)
            {
                msgBuilder.Append(StateApi.Current.CompareStates(gs[i].bytes, gs[i + 1].bytes));
            }

            return msgBuilder.ToString();
        }

        public void Remove(int fromFrame)
        {
            frames_gameStates = frames_gameStates.Where(x => x.frame >= fromFrame).ToList();
        }

        public void Reset()
        {
            frames_gameStates = new List<(int, byte[])>();
        }
    }
}
