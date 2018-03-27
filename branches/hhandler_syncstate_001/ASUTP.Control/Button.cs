using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ASUTP.Control {
    /// <summary>
    /// Класс для граф./элемента - кнопка с возможностью инициирования нескольких действий
    /// (по аналогии со списком)
    /// </summary>
    public class DropDownButton : Button {
        private int rightPadding = 16;
        /// <summary>
        /// Конструктор - основной (без аргументов)
        /// </summary>
        public DropDownButton ()
        {
            Padding = new Padding (0, 0, rightPadding, 0);
        }
        /// <summary>
        /// Обработчик события(переопределенный) - "прорисовка" объекта
        /// </summary>
        /// <param name="pevent">Аргумент события(контекст)</param>
        protected override void OnPaint (PaintEventArgs pevent)
        {
            base.OnPaint (pevent);

            var rect = new Rectangle (ClientRectangle.Width - rightPadding, ClientRectangle.Top + 3, rightPadding - 3, ClientRectangle.Height - 6);

            int x = ClientRectangle.Width - rightPadding + 3;
            int y = ClientRectangle.Height / 2 - 1;

            var brush = Enabled ? SystemBrushes.ControlText : SystemBrushes.ButtonShadow;
            var arrows = new Point [] { new Point (x, y), new Point (x + 7, y), new Point (x + 3, y + 4) };
            pevent.Graphics.FillPolygon (brush, arrows);
            pevent.Graphics.DrawLine (Pens.Silver, rect.Left, rect.Top, rect.Left, rect.Bottom);
        }

        /// <summary>
        /// Обработчик события - нажатие кнопки
        /// </summary>
        /// <param name="e">Аргумент события</param>
        protected override void OnMouseDown (MouseEventArgs e)
        {
            if (e.X > Width - rightPadding && e.Button == MouseButtons.Left) {
                if (ContextMenuStrip != null) {
                    ContextMenuStrip.MinimumSize = new Size (Width - rightPadding, 0);
                    ContextMenuStrip.Show (this, 0, Height);
                }
            } else
                base.OnMouseDown (e);
        }
    }
}
