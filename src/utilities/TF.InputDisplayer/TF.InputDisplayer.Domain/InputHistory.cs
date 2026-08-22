using TF.InputDisplayer.Domain.Models;

namespace TF.InputDisplayer.Domain
{
    public sealed class InputHistory
    {
        public const int MaxSeats = 4;
        public const int RowCapacity = 32;

        private const int NoData = -1;

        private int[] _data = [];
        private int _frameCount;
        private int _seatCount;
        private int _lastPushed = -1;

        private readonly List<int> _boundaries = [];

        private readonly InputRow[][] _rows = new InputRow[MaxSeats][];
        private readonly int[] _rowCount = new int[MaxSeats];
        private readonly int[] _rowFrame = new int[MaxSeats];

        public InputHistory()
        {
            for (int seat = 0; seat < MaxSeats; seat++)
            {
                _rows[seat] = new InputRow[RowCapacity];
            }

            Begin(0);
        }

        public bool HasFrames => _frameCount > 0;

        public void Begin(int seatCount)
        {
            _seatCount = Math.Clamp(seatCount, 0, MaxSeats);
            _frameCount = 0;
            _lastPushed = -1;
            _data = Array.Empty<int>();
            _boundaries.Clear();

            for (int seat = 0; seat < MaxSeats; seat++)
            {
                _rowCount[seat] = 0;
                _rowFrame[seat] = int.MinValue;
            }
        }

        public void Push(int frame, int seat, int packed)
        {
            if (frame < 0 || seat < 0 || seat >= _seatCount)
            {
                return;
            }

            Grow(frame + 1);

            if (frame > _lastPushed)
            {
                Seed(frame);
                _lastPushed = frame;
            }

            _data[frame * _seatCount + seat] = packed;

            InvalidateAt(frame);
        }

        public void MarkBoundary(int frame)
        {
            if (frame < 0)
            {
                return;
            }

            var at = _boundaries.BinarySearch(frame);

            if (at >= 0)
            {
                return;
            }

            _boundaries.Insert(~at, frame);

            for (int seat = 0; seat < MaxSeats; seat++)
            {
                _rowFrame[seat] = int.MinValue;
                _rowCount[seat] = 0;
            }
        }

        private int FloorFor(int frame)
        {
            var at = _boundaries.BinarySearch(frame);
            var index = at >= 0 ? at : ~at - 1;

            return index >= 0 ? _boundaries[index] : 0;
        }

        public int Rows(int seat, int frame, InputRow[] into, int maxRows)
        {
            if (seat < 0 || seat >= _seatCount || into == null)
            {
                return 0;
            }

            var rows = _rows[seat];

            if (_rowFrame[seat] != frame)
            {
                if (frame == _rowFrame[seat] + 1 && _rowCount[seat] > 0)
                {
                    Extend(seat, frame);
                }
                else
                {
                    _rowCount[seat] = Scan(seat, frame, rows);
                }

                _rowFrame[seat] = frame;
            }

            var count = Math.Min(_rowCount[seat], Math.Min(maxRows, into.Length));
            Array.Copy(rows, into, count);

            return count;
        }

        private int At(int seat, int frame)
        {
            if (frame < 0 || frame >= _frameCount)
            {
                return NoData;
            }

            return _data[frame * _seatCount + seat];
        }

        private int Scan(int seat, int frame, InputRow[] rows)
        {
            var count = 0;
            var floor = FloorFor(frame);

            for (int f = Math.Min(frame, _frameCount - 1); f >= floor; f--)
            {
                var value = At(seat, f);

                if (value == NoData)
                {
                    break;
                }

                if (count > 0 && rows[count - 1].Packed == value)
                {
                    rows[count - 1].Frames++;
                    continue;
                }

                if (count == rows.Length)
                {
                    break;
                }

                rows[count].Packed = value;
                rows[count].Frames = 1;
                count++;
            }

            return count;
        }

        private void Extend(int seat, int frame)
        {
            var value = At(seat, frame);

            if (value == NoData)
            {
                _rowCount[seat] = Scan(seat, frame, _rows[seat]);
                return;
            }

            var rows = _rows[seat];

            if (rows[0].Packed == value)
            {
                rows[0].Frames++;
                return;
            }

            var keep = Math.Min(_rowCount[seat], rows.Length - 1);
            Array.Copy(rows, 0, rows, 1, keep);

            rows[0].Packed = value;
            rows[0].Frames = 1;
            _rowCount[seat] = keep + 1;
        }

        private void Seed(int frame)
        {
            if (_lastPushed < 0)
            {
                return;
            }

            var from = _lastPushed * _seatCount;

            for (int f = _lastPushed + 1; f <= frame; f++)
            {
                Array.Copy(_data, from, _data, f * _seatCount, _seatCount);
            }
        }

        private void InvalidateAt(int frame)
        {
            for (int seat = 0; seat < MaxSeats; seat++)
            {
                if (_rowFrame[seat] >= frame)
                {
                    _rowFrame[seat] = int.MinValue;
                    _rowCount[seat] = 0;
                }
            }
        }

        private void Grow(int frames)
        {
            if (frames <= _frameCount)
            {
                return;
            }

            var needed = frames * _seatCount;

            if (needed > _data.Length)
            {
                var capacity = Math.Max(needed, Math.Max(_data.Length * 2, 256 * _seatCount));
                Array.Resize(ref _data, capacity);
            }

            Array.Fill(_data, NoData, _frameCount * _seatCount, needed - _frameCount * _seatCount);
            _frameCount = frames;
        }
    }
}
