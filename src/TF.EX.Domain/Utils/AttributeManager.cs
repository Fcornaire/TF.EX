namespace TF.EX.Domain.Utils
{
    //TODO: Drop this
    public class AttributeManager<T>
    {
        private List<T> _items;
        private Func<T> _defaultItemFactory;

        public AttributeManager(Func<T> defaultItemFactory, int defaultSize = 0)
        {
            _items = new List<T>();
            _defaultItemFactory = defaultItemFactory;
            Init(defaultSize);
        }

        public T this[int index]
        {
            get
            {
                if (index < _items.Count)
                {
                    return _items[index];
                }
                return _defaultItemFactory();
            }
            set
            {
                if (index >= _items.Count)
                {
                    for (int i = _items.Count; i <= index; i++)
                    {
                        _items.Add(_defaultItemFactory());
                    }
                }
                _items[index] = value;
            }
        }

        public int Count()
        {
            return _items.Count;
        }

        public void Clear()
        {
            _items.Clear();
        }

        public List<T> Get()
        {
            return _items;
        }

        public void Remove(T item)
        {
            _items.Remove(item);
        }

        public void Update(IEnumerable<T> items)
        {
            _items = items.ToList();
        }

        public void Reset()
        {
            var size = _items.Count;
            this._items.Clear();
            this.Init(size);
        }

        private void Init(int defaultSize)
        {
            for (int i = 0; i < defaultSize; i++)
            {
                _items.Add(_defaultItemFactory());
            }
        }
    }
}
