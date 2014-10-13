using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace StatisticCommon
{
    partial class HLabel
    {
        /// <summary>
        /// Требуется переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором компонентов

        /// <summary>
        /// Обязательный метод для поддержки конструктора - не изменяйте
        /// содержимое данного метода при помощи редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
        }

        #endregion
    }

    public partial class HLabel : System.Windows.Forms.Label
    {
        public enum TYPE_HLABEL { UNKNOWN = -1, TG, TOTAL, TOTAL_ZOOM, COUNT_TYPE_HLABEL };
        public TYPE_HLABEL m_type;

        public HLabel(Point pt, Size sz, Color foreColor, Color backColor, Single szFont, ContentAlignment align)
        {
            InitializeComponent();

            this.BorderStyle = BorderStyle.Fixed3D;

            if (((pt.X < 0) || (pt.Y < 0)) ||
                ((sz.Width < 0) || (sz.Height < 0)))
                this.Dock = DockStyle.Fill;
            else
            {
                this.Location = pt;
                this.Size = sz;
            }

            this.ForeColor = foreColor;
            this.BackColor = backColor;

            this.TextAlign = align;

            this.Font = new System.Drawing.Font("Microsoft Sans Serif", szFont, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));

            //this.Text = text;

            m_type = TYPE_HLABEL.UNKNOWN;
        }

        public HLabel(HLabelStyles prop)
            : this(new Point(-1, -1), new Size(-1, -1), prop.m_foreColor, prop.m_backColor, prop.m_szFont, prop.m_align)
        {
        }

        public HLabel(IContainer container, Point pt, Size sz, Color foreColor, Color backColor, Single szFont, ContentAlignment align)
            : this(pt, sz, foreColor, backColor, szFont, align)
        {
            container.Add(this);
        }

        public HLabel(IContainer container, HLabelStyles prop)
            : this(new Point(-1, -1), new Size(-1, -1), prop.m_foreColor, prop.m_backColor, prop.m_szFont, prop.m_align)
        {
            container.Add(this);
        }

        public static Label createLabel(string name, HLabelStyles prop)
        {
            Label lblRes = new Label();
            lblRes.Text = name;
            if (((prop.m_pt.X < 0) && (prop.m_pt.Y < 0)) &&
                ((prop.m_sz.Width < 0) && (prop.m_sz.Height < 0)))
                lblRes.Dock = DockStyle.Fill;
            else
            {
                lblRes.Location = prop.m_pt;
                if ((prop.m_sz.Width < 0) && (prop.m_sz.Height < 0))
                    lblRes.AutoSize = true;
                else
                    lblRes.Size = prop.m_sz;
            }
            lblRes.BorderStyle = BorderStyle.Fixed3D;
            lblRes.Font = new System.Drawing.Font("Microsoft Sans Serif", prop.m_szFont, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            lblRes.TextAlign = prop.m_align;
            lblRes.ForeColor = prop.m_foreColor;
            lblRes.BackColor = prop.m_backColor;

            return lblRes;
        }
    }

    public class HLabelStyles
    {
        public Color m_foreColor,
                    m_backColor;
        public ContentAlignment m_align;
        public Single m_szFont;
        public Point m_pt;
        public Size m_sz;

        public HLabelStyles(Color foreColor, Color backColor, Single szFont, ContentAlignment align)
            : this(new Point(-1, -1), new Size(-1, -1), foreColor, backColor, szFont, align)
        {
        }

        public HLabelStyles(Point pt, Size sz, Color foreColor, Color backColor, Single szFont, ContentAlignment align)
        {
            m_pt = pt;
            m_sz = sz;

            m_foreColor = foreColor;
            m_backColor = backColor;
            m_szFont = szFont;
            m_align = align;
        }
    };
}
