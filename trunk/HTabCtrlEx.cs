using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;

using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;

namespace HClassLibrary
{
    public partial class HTabCtrlEx : System.Windows.Forms.TabControl
    {
        public delegate void DelegateOnCloseTab(object sender, CloseTabEventArgs e);
        public event DelegateOnCloseTab OnClose;

        public HTabCtrlEx()
        {
            InitializeComponent();
        }

        public HTabCtrlEx(IContainer container)
        {
            container.Add(this);

            InitializeComponent();
        }

        private bool confirmOnClose = true;
        public bool ConfirmOnClose
        {
            get
            {
                return this.confirmOnClose;
            }
            set
            {
                this.confirmOnClose = value;
            }
        }

        /// <span class="code-SummaryComment"><summary></span>
        /// override to draw the close button
        /// <span class="code-SummaryComment"></summary></span>
        /// <span class="code-SummaryComment"><param name="e"></param></span>
        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            RectangleF tabTextArea = RectangleF.Empty;
            for (int nIndex = 0; nIndex < this.TabCount; nIndex++)
            {
                Image img;
                tabTextArea = (RectangleF)this.GetTabRect(nIndex);
                //if (nIndex > 0) {
                    if (! (nIndex == this.SelectedIndex))
                    {
                        img = HClassLibrary.Properties.Resources.closeNonActive.ToBitmap();
                    }
                    else
                    {
                        img = HClassLibrary.Properties.Resources.closeInActive.ToBitmap();
                    }

                    using (img)
                    {
                        e.Graphics.DrawImage(img, tabTextArea.X + tabTextArea.Width - 16, 3, 13, 13);
                    }
                //}
                //else
                //    ;

                string str = this.TabPages[nIndex].Text;
                StringFormat stringFormat = new StringFormat();
                stringFormat.Alignment = StringAlignment.Near;
                using (SolidBrush brush = new SolidBrush(this.TabPages[nIndex].ForeColor))
                {
                    /*Draw the tab header text*/
                    e.Graphics.DrawString(str, this.Font, brush, tabTextArea, stringFormat);
                }
            }
        }

        /// <span class="code-SummaryComment"><summary></span>
        /// Get the stream of the embedded bitmap image
        /// <span class="code-SummaryComment"></summary></span>
        /// <span class="code-SummaryComment"><param name="filename"></param></span>
        /// <span class="code-SummaryComment"><returns></returns></span>
        private Stream GetContentFromResource(string filename)
        {
            Assembly asm = Assembly.GetExecutingAssembly ();
            Stream stream = asm.GetManifestResourceStream ("HTabCtrlLibrary." + filename);

            return stream;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (DesignMode == false)
            {
                Image img;
                Graphics g = CreateGraphics();
                g.SmoothingMode = SmoothingMode.AntiAlias;
                for (int nIndex = 0; nIndex < this.TabCount; nIndex++)
                {
                    RectangleF tabTextArea = (RectangleF)this.GetTabRect(nIndex);
                    tabTextArea = new RectangleF(tabTextArea.X + tabTextArea.Width - 22, 3, tabTextArea.Height - 3, tabTextArea.Height - 5);

                    Point pt = new Point(e.X, e.Y);
                    if (tabTextArea.Contains(pt))
                    {
                        img = HClassLibrary.Properties.Resources.closeInActive.ToBitmap();
                    }
                    else
                    {
                        if (! (nIndex == SelectedIndex))
                        {
                            img = HClassLibrary.Properties.Resources.closeNonActive.ToBitmap();
                        }
                        else
                            img = HClassLibrary.Properties.Resources.closeInActive.ToBitmap();
                    }

                    using (img)
                    {
                        g.DrawImage(img, tabTextArea.X + tabTextArea.Width - 9, 3, 13, 13);
                    }
                }
                g.Dispose();
            }
            else
                ;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if ((DesignMode == false)/* && (SelectedIndex > 0)*/) //Здесь запрет закрыть вкладку с индексом "0"
            {
                RectangleF tabTextArea = (RectangleF)this.GetTabRect(SelectedIndex);
                tabTextArea = new RectangleF(tabTextArea.X + tabTextArea.Width - 22, 3, tabTextArea.Height - 3, tabTextArea.Height - 5);
                Point pt = new Point(e.X, e.Y);
                if (tabTextArea.Contains(pt) == true)
                {
                    if (confirmOnClose == true)
                    {
                        if (MessageBox.Show("Вы закрывайте вкладку с объектом: " + this.TabPages[SelectedIndex].Text.TrimEnd() + ". Продолжить?", "Подтверждение", MessageBoxButtons.YesNo) == DialogResult.No)
                            return;
                        else
                            ;
                    }
                    else
                        ;

                    //Fire Event to Client
                    if (! (OnClose == null))
                    {
                        OnClose(this, new CloseTabEventArgs(SelectedIndex, this.TabPages[SelectedIndex].Text.Trim()));
                    }
                    else
                        ;
                }
            }
        }

        public void TabPagesClear()
        {
            while (TabPages.Count > 1)
                TabPages.RemoveAt (TabPages.Count - 1);
        }

        public void AddTabPage (string name) {
            string text = GetNameTab (name);
            this.TabPages.Add(text, text);
        }

        public static string GetNameTab (string text) { return new string(' ', 1) + text + new string(' ', 5); }
    }

    public class CloseTabEventArgs : EventArgs
    {
        private int nTabIndex = -1;
        private string strHeaderText = string.Empty;
        public CloseTabEventArgs(int nTabIndex, string text)
        {
            this.nTabIndex = nTabIndex;
            this.strHeaderText = text;
        }
        /// <summary>
        /// Get/Set the tab index value where the close button is clicked
        /// </summary>
        public int TabIndex
        {
            get
            {
                return this.nTabIndex;
            }
            set
            {
                this.nTabIndex = value;
            }
        }

        /// <summary>
        /// Get/Set the tab index value where the close button is clicked
        /// </summary>
        public string TabHeaderText
        {
            get
            {
                return this.strHeaderText;
            }
            set
            {
                this.strHeaderText = value;
            }
        }
    }
}
