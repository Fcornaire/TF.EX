namespace TF.InputDisplayer.Domain
{
    public static class Rounds
    {
        private static bool _pending;

        public static void Mark() => _pending = true;

        public static void Consume(InputHistory history, int frame)
        {
            if (!_pending || history == null)
            {
                return;
            }

            _pending = false;
            history.MarkBoundary(frame);
        }
    }
}
