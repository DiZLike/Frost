// PlaylistControl.Data.cs
using System;
using System.Collections.Generic;
using System.Linq;

namespace FrostPlayer.Controls
{
    public partial class PlaylistControl
    {
        public void AddItem(PlaylistItem item)
        {
            _items.Add(item);
            item.Index = _items.Count;
            QueueScrollbarUpdate();
            Invalidate();
        }

        public void AddRange(IEnumerable<PlaylistItem> items)
        {
            var itemsList = items.ToList();
            _items.AddRange(itemsList);

            for (int i = 0; i < itemsList.Count; i++)
            {
                itemsList[i].Index = _items.Count - itemsList.Count + i + 1;
            }

            QueueScrollbarUpdate();
            Invalidate();
        }

        public void RemoveItem(PlaylistItem item)
        {
            int index = _items.IndexOf(item);
            if (index >= 0)
            {
                _items.RemoveAt(index);

                for (int i = index; i < _items.Count; i++)
                {
                    _items[i].Index = i + 1;
                }

                if (_selectedIndex == index)
                    _selectedIndex = -1;
                else if (_selectedIndex > index)
                    _selectedIndex--;

                if (_playingIndex == index)
                    _playingIndex = -1;
                else if (_playingIndex > index)
                    _playingIndex--;

                QueueScrollbarUpdate();
                Invalidate();
            }
        }

        public void Clear()
        {
            _items.Clear();
            _selectedIndex = -1;
            _playingIndex = -1;
            _hoveredIndex = -1;
            _scrollOffset = 0;
            QueueScrollbarUpdate();
            Invalidate();
        }

        public void SortByColumn(int columnIndex, SortOrder order)
        {
            if (columnIndex < 0 || columnIndex >= _columns.Count || !_columns[columnIndex].Sortable)
                return;

            _sortColumnIndex = columnIndex;
            _sortOrder = order;

            var column = _columns[columnIndex];
            IComparer<PlaylistItem> comparer = null;

            switch (column.Name)
            {
                case "Index":
                    comparer = new IndexComparer(order);
                    break;
                case "Artist":
                    comparer = new ArtistComparer(order);
                    break;
                case "Album":
                    comparer = new AlbumComparer(order);
                    break;
                case "Title":
                    comparer = new TitleComparer(order);
                    break;
                case "Duration":
                    comparer = new DurationComparer(order);
                    break;
            }

            if (comparer != null)
            {
                _items.Sort(comparer);

                for (int i = 0; i < _items.Count; i++)
                {
                    _items[i].Index = i + 1;
                }

                Invalidate();
            }
        }

        private class IndexComparer : IComparer<PlaylistItem>
        {
            private readonly SortOrder _order;
            public IndexComparer(SortOrder order) => _order = order;

            public int Compare(PlaylistItem x, PlaylistItem y)
            {
                int result = x.Index.CompareTo(y.Index);
                return _order == SortOrder.Ascending ? result : -result;
            }
        }

        private class ArtistComparer : IComparer<PlaylistItem>
        {
            private readonly SortOrder _order;
            public ArtistComparer(SortOrder order) => _order = order;

            public int Compare(PlaylistItem x, PlaylistItem y)
            {
                int result = string.Compare(x.Artist, y.Artist, StringComparison.OrdinalIgnoreCase);
                return _order == SortOrder.Ascending ? result : -result;
            }
        }

        private class AlbumComparer : IComparer<PlaylistItem>
        {
            private readonly SortOrder _order;
            public AlbumComparer(SortOrder order) => _order = order;

            public int Compare(PlaylistItem x, PlaylistItem y)
            {
                int result = string.Compare(x.Album, y.Album, StringComparison.OrdinalIgnoreCase);
                return _order == SortOrder.Ascending ? result : -result;
            }
        }

        private class TitleComparer : IComparer<PlaylistItem>
        {
            private readonly SortOrder _order;
            public TitleComparer(SortOrder order) => _order = order;

            public int Compare(PlaylistItem x, PlaylistItem y)
            {
                int result = string.Compare(x.Title, y.Title, StringComparison.OrdinalIgnoreCase);
                return _order == SortOrder.Ascending ? result : -result;
            }
        }

        private class DurationComparer : IComparer<PlaylistItem>
        {
            private readonly SortOrder _order;
            public DurationComparer(SortOrder order) => _order = order;

            public int Compare(PlaylistItem x, PlaylistItem y)
            {
                int result = x.Duration.CompareTo(y.Duration);
                return _order == SortOrder.Ascending ? result : -result;
            }
        }
    }
}