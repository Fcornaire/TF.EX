using Monocle;

namespace TF.EX.Domain.Utils
{
    public class TutorialSlide
    {
        public string TextA { get; set; } = "";
        public string TextB { get; set; } = "";
        public int Dir { get; set; }
        public Func<Subtexture> Image { get; set; }
    }

    public static class Tutorial
    {
        private static Action _markSeen;

        public static bool IsPending { get; private set; }

        public static IReadOnlyList<TutorialSlide> Slides { get; private set; } = [];

        public static void Configure(IReadOnlyList<TutorialSlide> slides, bool isPending, Action markSeen)
        {
            Slides = slides;
            IsPending = isPending;
            _markSeen = markSeen;
        }

        public static void MarkSeen()
        {
            if (!IsPending)
            {
                return;
            }

            IsPending = false;
            _markSeen?.Invoke();
        }
    }
}
