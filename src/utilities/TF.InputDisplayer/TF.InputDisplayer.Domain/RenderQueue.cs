namespace TF.InputDisplayer.Domain
{
    public static class RenderQueue
    {
        private static InputHistory _history;
        private static int _frame;
        private static bool _pending;

        public static void Request(InputHistory history, int frame)
        {
            _history = history;
            _frame = frame;
            _pending = true;
        }

        public static void Clear()
        {
            _history = null;
            _pending = false;
        }

        public static bool Consume(out InputHistory history, out int frame)
        {
            history = _history;
            frame = _frame;

            var pending = _pending;
            _pending = false;

            return pending && history != null;
        }
    }
}
