// PlaylistControl.Paint.cs
using FrostPlayer.Controls.Playlist;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace FrostPlayer.Controls
{
    public partial class PlaylistControl
    {
        private static class PaintManager
        {
            public static void Paint(PlaylistControl control, PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                e.Graphics.Clear(control.BackColor);

                var clientRect = new Rectangle(0, 0,
                    control.ClientSize.Width - control.ScrollBarInternal.Width,
                    control.ClientSize.Height);

                control.SetVisibleRows(Math.Max(0, (clientRect.Height - control.HeaderHeight) / control.RowHeight));

                DrawColumnHeaders(control, e.Graphics, clientRect);
                DrawRows(control, e.Graphics, clientRect);
                DrawBorder(control, e.Graphics);
            }

            private static void DrawColumnHeaders(PlaylistControl control, Graphics g, Rectangle clientRect)
            {
                int x = 0;

                control.ForEachColumn((column, i) =>
                {
                    var headerRect = new Rectangle(x, 0, column.Width, control.HeaderHeight);

                    using (var brush = new SolidBrush(control.HeaderBackgroundColor))
                    {
                        g.FillRectangle(brush, headerRect);
                    }

                    var format = new StringFormat
                    {
                        LineAlignment = StringAlignment.Center,
                        Alignment = GetStringAlignment(column.TextAlignment),
                        Trimming = StringTrimming.EllipsisCharacter
                    };

                    using (var textBrush = new SolidBrush(control.HeaderTextColor))
                    using (var textFont = new Font(control.Font.FontFamily, control.Font.Size - 1, FontStyle.Bold))
                    {
                        string text = column.Text;
                        if (control.GetSortColumnIndex() == i && control.GetSortOrder() != SortOrder.None)
                        {
                            text += control.GetSortOrder() == SortOrder.Ascending ? " ↑" : " ↓";
                        }
                        g.DrawString(text, textFont, textBrush, headerRect, format);
                    }

                    using (var borderPen = new Pen(control._borderColor))
                    {
                        g.DrawLine(borderPen, x + column.Width - 1, 0, x + column.Width - 1, control.HeaderHeight);
                    }

                    x += column.Width;
                });

                using (var linePen = new Pen(control._borderColor))
                {
                    g.DrawLine(linePen, 0, control.HeaderHeight - 1, x, control.HeaderHeight - 1);
                }
            }

            private static void DrawRows(PlaylistControl control, Graphics g, Rectangle clientRect)
            {
                if (control.ItemsInternal.Count == 0)
                    return;

                int startRow = Math.Max(0, Math.Min(control.ScrollOffsetInternal / control.RowHeight,
                    control.ItemsInternal.Count - control.VisibleRowsInternal));
                int endRow = Math.Min(startRow + control.VisibleRowsInternal + 1, control.ItemsInternal.Count);

                int y = control.HeaderHeight;

                for (int i = startRow; i < endRow; i++)
                {
                    var item = control.ItemsInternal[i];
                    var rowRect = new Rectangle(0, y, clientRect.Width, control.RowHeight);

                    Color backgroundColor = GetRowBackgroundColor(control, i);
                    using (var brush = new SolidBrush(backgroundColor))
                    {
                        g.FillRectangle(brush, rowRect);
                    }

                    DrawRowCells(control, g, item, i, rowRect);

                    if (control.ShowGridLines)
                    {
                        using (var linePen = new Pen(Color.FromArgb(240, 240, 240)))
                        {
                            g.DrawLine(linePen, 0, y + control.RowHeight - 1,
                                clientRect.Width, y + control.RowHeight - 1);
                        }
                    }

                    y += control.RowHeight;
                }
            }

            private static Color GetRowBackgroundColor(PlaylistControl control, int rowIndex)
            {
                if (rowIndex == control.SelectedIndexInternal)
                    return control.SelectionColor;

                if (rowIndex == control.HoveredIndexInternal)
                    return Color.FromArgb(240, 240, 240);

                return rowIndex % 2 == 0 ? control.RowBackgroundColor : control.AlternateRowBackgroundColor;
            }

            private static void DrawRowCells(PlaylistControl control, Graphics g, PlaylistItem item,
                int rowIndex, Rectangle rowRect)
            {
                int x = 0;

                control.ForEachColumn((column, colIndex) =>
                {
                    var cellRect = new Rectangle(x, rowRect.Top, column.Width, rowRect.Height);

                    switch (column.Name)
                    {
                        case "Status":
                            DrawStatusCell(control, g, rowIndex, cellRect);
                            break;
                        case "Index":
                            DrawTextCell(g, item.Index.ToString(), cellRect,
                                column.TextAlignment, control.TextColor, control.Font);
                            break;
                        case "Artist":
                            DrawTextCell(g, item.Artist ?? "-", cellRect,
                                column.TextAlignment, control.TextColor, control.Font);
                            break;
                        case "Album":
                            DrawTextCell(g, item.Album ?? "-", cellRect,
                                column.TextAlignment, control.TextColor, control.Font);
                            break;
                        case "Title":
                            DrawTextCell(g, item.Title ?? "-", cellRect,
                                column.TextAlignment, control.TextColor, control.Font);
                            break;
                        case "Duration":
                            DrawTextCell(g, item.DurationFormatted, cellRect,
                                column.TextAlignment, control.DurationTextColor, control.Font);
                            break;
                    }

                    if (control.ShowGridLines && colIndex < control.ColumnsInternal.Count - 1)
                    {
                        using (var linePen = new Pen(Color.FromArgb(240, 240, 240)))
                        {
                            g.DrawLine(linePen, x + column.Width - 1, rowRect.Top,
                                x + column.Width - 1, rowRect.Bottom - 1);
                        }
                    }

                    x += column.Width;
                });
            }

            private static void DrawStatusCell(PlaylistControl control, Graphics g, int rowIndex, Rectangle cellRect)
            {
                if (rowIndex == control.PlayingIndexInternal)
                {
                    int size = PlaylistConstants.PlayingIndicatorSize;
                    int x = cellRect.X + (cellRect.Width - size) / 2;
                    int y = cellRect.Y + (cellRect.Height - size) / 2;

                    using (var brush = new SolidBrush(control.PlayingIndicatorColor))
                    {
                        g.FillEllipse(brush, x, y, size, size);
                    }

                    using (var brush = new SolidBrush(Color.White))
                    {
                        g.FillEllipse(brush, x + 2, y + 2, size - 4, size - 4);
                    }
                }
                else if (rowIndex == control.SelectedIndexInternal)
                {
                    int size = PlaylistConstants.SelectionIndicatorSize;
                    int x = cellRect.X + (cellRect.Width - size) / 2;
                    int y = cellRect.Y + (cellRect.Height - size) / 2;

                    using (var brush = new SolidBrush(control.PlayingIndicatorColor))
                    {
                        g.FillEllipse(brush, x, y, size, size);
                    }
                }
            }

            private static void DrawTextCell(Graphics g, string text, Rectangle cellRect,
                HorizontalAlignment alignment, Color color, Font baseFont)
            {
                var format = new StringFormat
                {
                    LineAlignment = StringAlignment.Center,
                    Alignment = GetStringAlignment(alignment),
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.NoWrap
                };

                using (var brush = new SolidBrush(color))
                using (var cellFont = new Font(baseFont.FontFamily, baseFont.Size - 1, baseFont.Style))
                {
                    var textRect = new Rectangle(cellRect.X + PlaylistConstants.CellPadding, cellRect.Y,
                        cellRect.Width - 2 * PlaylistConstants.CellPadding, cellRect.Height);
                    g.DrawString(text, cellFont, brush, textRect, format);
                }
            }

            private static StringAlignment GetStringAlignment(HorizontalAlignment alignment)
            {
                switch (alignment)
                {
                    case HorizontalAlignment.Left: return StringAlignment.Near;
                    case HorizontalAlignment.Right: return StringAlignment.Far;
                    default: return StringAlignment.Center;
                }
            }

            private static void DrawBorder(PlaylistControl control, Graphics g)
            {
                using (var borderPen = new Pen(control._borderColor))
                {
                    g.DrawRectangle(borderPen,
                        new Rectangle(0, 0, control.ClientSize.Width - 1, control.ClientSize.Height - 1));
                }
            }
        }
    }
}