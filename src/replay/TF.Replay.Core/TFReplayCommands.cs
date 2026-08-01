using FortRise;
using TF.Replay.Domain.Api;

namespace TF.Replay.Core
{
    internal class TFReplayCommands
    {
        private readonly TfReplayApi _api;

        public TFReplayCommands(TfReplayApi api)
        {
            _api = api;
        }

        public void Register(IModuleContext context)
        {
            context.Registry.Commands.RegisterCommands("replay-play", new CommandConfiguration
            {
                Callback = args =>
                {
                    if (args.Length == 0)
                    {
                        Monocle.Engine.Instance.Commands.Log("usage: replay-play <filename.tow>");
                        return;
                    }

                    var failure = _api.StartPlayback(args[0]);

                    if (failure != null)
                    {
                        Monocle.Engine.Instance.Commands.Log(failure);
                    }
                }
            });

            context.Registry.Commands.RegisterCommands("replay-stop", new CommandConfiguration
            {
                Callback = _ =>
                {
                    _api.StopPlayback();

                    if (_api.IsRecording)
                    {
                        Monocle.Engine.Instance.Commands.Log(_api.Export() ?? "nothing recorded");
                    }
                }
            });

            context.Registry.Commands.RegisterCommands("replay-seek", new CommandConfiguration
            {
                Callback = args =>
                {
                    if (args.Length == 0 || !int.TryParse(args[0], out var frame))
                    {
                        Monocle.Engine.Instance.Commands.Log("usage: replay-seek <frame>");
                        return;
                    }

                    Monocle.Engine.Instance.Commands.Log(_api.SeekTo(frame) ? $"seeked to {frame}" : "seek failed");
                }
            });
        }
    }
}
