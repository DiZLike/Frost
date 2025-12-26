// PlaylistControl.Events.cs
using FrostPlayer.Controls.Playlist;
using System;
using System.Windows.Forms;

namespace FrostPlayer.Controls
{
    public partial class PlaylistControl
    {
        private static class EventHandlers
        {
            public static void HandleMouseDown(PlaylistControl control, MouseEventArgs e)
            {
                if (e.Y < control.HeaderHeight)
                {
                    HandleHeaderClick(control, e);
                }
                else
                {
                    HandleRowClick(control, e);
                }
            }

            private static void HandleHeaderClick(PlaylistControl control, MouseEventArgs e)
            {
                int x = 0;
                control.ForEachColumn((column, i) =>
                {
                    if (e.X >= x && e.X < x + column.Width)
                    {
                        bool isOnRightBorder = e.X > x + column.Width - PlaylistConstants.ResizeAreaWidth &&
                                              e.X < x + column.Width + PlaylistConstants.ResizeAreaWidth;

                        if (isOnRightBorder)
                        {
                            control.SetResizingColumnIndex(i);
                            control.SetIsResizing(true);
                            control.SetResizeStartX(e.X);
                            control.SetResizeStartWidth(column.Width);
                            control.Cursor = Cursors.SizeWE;
                        }
                        else if (column.Sortable)
                        {
                            HandleSorting(control, i);
                            control.OnColumnClick(i);
                        }
                        return;
                    }
                    x += column.Width;
                });
            }

            private static void HandleSorting(PlaylistControl control, int columnIndex)
            {
                if (control.GetSortColumnIndex() == columnIndex)
                {
                    control.SetSortOrder(control.GetSortOrder() switch
                    {
                        SortOrder.None => SortOrder.Ascending,
                        SortOrder.Ascending => SortOrder.Descending,
                        _ => SortOrder.Ascending
                    });
                }
                else
                {
                    control.SetSortColumnIndex(columnIndex);
                    control.SetSortOrder(SortOrder.Ascending);
                }

                control.SortByColumn(columnIndex, control.GetSortOrder());
            }

            private static void HandleRowClick(PlaylistControl control, MouseEventArgs e)
            {
                int rowIndex = (e.Y - control.HeaderHeight + control.ScrollOffsetInternal) / control.RowHeight;
                if (rowIndex >= 0 && rowIndex < control.ItemsInternal.Count)
                {
                    control.SelectedIndex = rowIndex;

                    if (e.Clicks == 2)
                    {
                        control.OnItemDoubleClick(rowIndex);
                    }
                }
            }

            public static void HandleMouseMove(PlaylistControl control, MouseEventArgs e)
            {
                if (control.GetIsResizing())
                {
                    HandleColumnResize(control, e);
                }
                else if (e.Y < control.HeaderHeight)
                {
                    HandleHeaderHover(control, e);
                }
                else
                {
                    HandleRowHover(control, e);
                }
            }

            private static void HandleColumnResize(PlaylistControl control, MouseEventArgs e)
            {
                int delta = e.X - control.GetResizeStartX();
                var column = control.ColumnsInternal[control.GetResizingColumnIndex()];
                column.Width = Math.Max(PlaylistConstants.MinColumnWidth, control.GetResizeStartWidth() + delta);
                control.UpdateWidthProperty(column.Name, column.Width);
                control.Invalidate();
            }

            private static void HandleHeaderHover(PlaylistControl control, MouseEventArgs e)
            {
                int x = 0;
                bool isResizeCursor = false;

                control.ForEachColumn((column, i) =>
                {
                    if (e.X >= x + column.Width - PlaylistConstants.ResizeAreaWidth &&
                        e.X <= x + column.Width + PlaylistConstants.ResizeAreaWidth)
                    {
                        isResizeCursor = true;
                        return;
                    }
                    x += column.Width;
                });

                control.Cursor = isResizeCursor ? Cursors.SizeWE : Cursors.Default;
            }

            private static void HandleRowHover(PlaylistControl control, MouseEventArgs e)
            {
                control.Cursor = Cursors.Default;

                int rowIndex = (e.Y - control.HeaderHeight + control.ScrollOffsetInternal) / control.RowHeight;
                int newHoveredIndex = rowIndex >= 0 && rowIndex < control.ItemsInternal.Count ? rowIndex : -1;

                if (control.HoveredIndexInternal != newHoveredIndex)
                {
                    control.SetHoveredIndex(newHoveredIndex);
                    control.Invalidate();
                }
            }

            public static void HandleMouseUp(PlaylistControl control, MouseEventArgs e)
            {
                if (control.GetIsResizing())
                {
                    control.SetIsResizing(false);
                    control.SetResizingColumnIndex(-1);
                    control.Cursor = Cursors.Default;
                }
            }
        }
    }
}