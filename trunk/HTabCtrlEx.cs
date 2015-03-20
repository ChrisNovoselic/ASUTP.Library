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
        public enum TYPE_TAB { FIXED, FLOAT };
        private List <TYPE_TAB> m_listTypeTabs;

        private static RectangleF s_rectPositionImg = new RectangleF (18, 4, 14, 14);

        private enum INDEX_BITMAP {FLOAT, CLOSE, COUNT_BITMAP };
        private enum INDEX_STATE_BITMAP { NON_ACTIVE, IN_ACTIVE, COUNT_STATE_BITMAP };
        private Icon [] m_arBitmap;
        public delegate void DelegateOnHTabCtrlEx(object sender, HTabCtrlExEventArgs e);
        public event DelegateOnHTabCtrlEx OnClose;
        public event DelegateOnHTabCtrlEx OnFloat;

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

        private Bitmap getBitmap (INDEX_BITMAP indx, INDEX_STATE_BITMAP state)
        {
            return m_arBitmap[(int)indx * (int)INDEX_STATE_BITMAP.COUNT_STATE_BITMAP + (int)state].ToBitmap ();
        }

        /// <span class="code-SummaryComment"><summary></span>
        /// override to draw the close button
        /// <span class="code-SummaryComment"></summary></span>
        /// <span class="code-SummaryComment"><param name="e"></param></span>
        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            RectangleF tabTextAreaText = RectangleF.Empty
                , tabTextAreaImg = RectangleF.Empty;
            for (int nIndex = 0; nIndex < this.TabCount; nIndex++)
            {
                Image img;
                INDEX_STATE_BITMAP state;
                tabTextAreaText = (RectangleF)this.GetTabRect(nIndex);
                //e.Graphics.IntersectClip(tabTextAreaText);
                tabTextAreaImg = new RectangleF(tabTextAreaText.X + tabTextAreaText.Width - s_rectPositionImg.X, s_rectPositionImg.Y, s_rectPositionImg.Width, s_rectPositionImg.Height);
                //if (nIndex > 0) {
                    if (! (nIndex == this.SelectedIndex))
                    {
                        state = INDEX_STATE_BITMAP.NON_ACTIVE;
                    }
                    else
                    {
                        state = INDEX_STATE_BITMAP.IN_ACTIVE;
                    }

                    using (img = getBitmap(INDEX_BITMAP.CLOSE, state))
                    {
                        e.Graphics.DrawImage(img, tabTextAreaImg);
                        //Console.WriteLine (@"OnDrawItem () - " + @"Индекс=" + nIndex + @"; X:" + tabTextArea.X + @"; width:" + tabTextArea.Width);
                    }

                    if (m_listTypeTabs[nIndex] == TYPE_TAB.FLOAT) {
                        tabTextAreaImg = new RectangleF(tabTextAreaImg.X - (s_rectPositionImg.Width + 1), s_rectPositionImg.Y, s_rectPositionImg.Width, s_rectPositionImg.Height);

                        using (img = getBitmap(INDEX_BITMAP.FLOAT, state))
                        {
                            e.Graphics.DrawImage(img, tabTextAreaImg);
                        }
                    }
                    else
                        ;
                //}
                //else
                //    ;

                string str = this.TabPages[nIndex].Text;
                StringFormat stringFormat = new StringFormat();
                stringFormat.Alignment = StringAlignment.Near;
                using (SolidBrush brush = new SolidBrush(this.TabPages[nIndex].ForeColor))
                {
                    /*Draw the tab header text*/
                    e.Graphics.DrawString(str, this.Font, brush, tabTextAreaText, stringFormat);
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
                INDEX_STATE_BITMAP state;
                Graphics g = CreateGraphics();
                g.SmoothingMode = SmoothingMode.AntiAlias;
                for (int nIndex = 0; nIndex < this.TabCount; nIndex++)
                {
                    RectangleF tabTextAreaText = (RectangleF)this.GetTabRect(nIndex)
                        , tabTextAreaClose = new RectangleF(tabTextAreaText.X + tabTextAreaText.Width - s_rectPositionImg.X, s_rectPositionImg.Y, s_rectPositionImg.Width, s_rectPositionImg.Height);
                    //Console.WriteLine(@"OnMouseMove () - " + @"Индекс=" + nIndex + @"; X:" + tabTextArea.X + @"; width:" + tabTextArea.Width);

                    Point pt = new Point(e.X, e.Y);
                    if (tabTextAreaClose.Contains(pt) == true)
                    {
                        state = INDEX_STATE_BITMAP.IN_ACTIVE;
                    }
                    else
                    {
                        if (!(nIndex == this.SelectedIndex))
                        {
                            state = INDEX_STATE_BITMAP.NON_ACTIVE;
                        }
                        else
                        {
                            state = INDEX_STATE_BITMAP.IN_ACTIVE;
                        }
                    }

                    img = getBitmap(INDEX_BITMAP.CLOSE, state);

                    using (img)
                    {
                        g.DrawImage(img, tabTextAreaClose);
                    }

                    if (m_listTypeTabs[nIndex] == TYPE_TAB.FLOAT)
                    {
                        RectangleF tabTextAreaFloat = new RectangleF(tabTextAreaClose.X - (s_rectPositionImg.Width + 1), s_rectPositionImg.Y, s_rectPositionImg.Width, s_rectPositionImg.Height);
                        
                        if (tabTextAreaFloat.Contains(pt))
                        {
                            state = INDEX_STATE_BITMAP.IN_ACTIVE;
                        }
                        else
                        {
                            if (!(nIndex == this.SelectedIndex))
                            {
                                state = INDEX_STATE_BITMAP.NON_ACTIVE;
                            }
                            else
                            {
                                state = INDEX_STATE_BITMAP.IN_ACTIVE;
                            }
                        }

                        img = getBitmap(INDEX_BITMAP.FLOAT, state);

                        g.DrawImage(img, tabTextAreaFloat);
                    }
                    else
                        ;
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
                RectangleF tabTextAreaText = (RectangleF)this.GetTabRect(SelectedIndex)
                    , tabTextAreaClose = new RectangleF(tabTextAreaText.X + tabTextAreaText.Width - s_rectPositionImg.X, s_rectPositionImg.Y, s_rectPositionImg.Width, s_rectPositionImg.Height);
                Point pt = new Point(e.X, e.Y);
                if (tabTextAreaClose.Contains(pt) == true)
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
                        OnClose(this, new HTabCtrlExEventArgs(SelectedIndex, this.TabPages[SelectedIndex].Text.Trim()));
                    }
                    else
                        ;
                }
                else {
                    if (m_listTypeTabs[SelectedIndex] == TYPE_TAB.FLOAT)
                    {
                        RectangleF tabTextAreaFloat = new RectangleF(tabTextAreaClose.X - (s_rectPositionImg.Width + 1), s_rectPositionImg.Y, s_rectPositionImg.Width, s_rectPositionImg.Height);
                        
                        if (tabTextAreaFloat.Contains(pt) == true)
                        {
                            if (!(OnFloat == null))
                            {
                                OnFloat(this, new HTabCtrlExEventArgs(SelectedIndex, this.TabPages[SelectedIndex].Text.Trim()));
                            }
                            else
                                ;
                        } else {
                        }
                    } else {
                    }

                }
            }
        }

        //public void TabPagesClear()
        //{
        //    while (TabCount > 1)
        //        TabPages.RemoveAt (TabCount - 1);

        //    m_listTypeTabs.Clear ();
        //}

        public void AddTabPage (string name, int id, TYPE_TAB typeTab) {
            m_listTypeTabs.Add(typeTab);
            this.TabPages.Add(name, getNameTab(name, typeTab));
        }

        public bool RemoveTabPage (string name) {
            bool bRes = RemoveTabPage (this.TabPages.IndexOfKey (name.Trim ()));

            if (bRes == false)
                Console.WriteLine (@"Ошибка удаления вкладки [" + name + "]...");
            else
                ;

            return bRes;
        }

        public bool RemoveTabPage(int indx)
        {
            bool bRes = true;
            
            if ((! (indx < 0))
                && (indx < this.TabCount)
                && (indx < m_listTypeTabs.Count))
            {
                m_listTypeTabs.RemoveAt(indx);
                this.TabPages.RemoveAt(indx);              
            }
            else
                bRes = false;

            return bRes;
        }

        public int IndexOfItemControl (Control ctrl) {
            int iRes = -1
                , indx = 0;

            while ((indx < TabCount) && (iRes < 0)) {
                if (TabPages [indx].Controls.Contains (ctrl) == true)
                    iRes = indx;
                else
                    ;

                indx ++;
            }

            return iRes;
        }

        private static string getNameTab (string text, TYPE_TAB type) {
            int cntSpace = 5;

            if (type == TYPE_TAB.FLOAT)
                cntSpace += 4;
            else
                ;

            return new string(' ', 1) + text + new string(' ', cntSpace);
        }
    }

    public class HTabCtrlExEventArgs : EventArgs
    {
        private int nTabIndex = -1;
        private string strHeaderText = string.Empty;
        private HTabCtrlEx.TYPE_TAB typeTab;
        private int nId;

        public HTabCtrlExEventArgs(int nId, int nTabIndex, string text)
        {
            this.nTabIndex = nTabIndex;
            this.strHeaderText = text;
            this.nId = nId;
        }
        /// <summary>
        /// Get/Set the tab index value where the close button is clicked
        /// </summary>
        public int TabIndex { get { return this.nTabIndex; } set { this.nTabIndex = value; } }

        /// <summary>
        /// Get/Set the tab index value where the close button is clicked
        /// </summary>
        public string TabHeaderText { get { return this.strHeaderText; } set { this.strHeaderText = value; } }

        public HTabCtrlEx.TYPE_TAB TabType { get { return this.typeTab; } set { this.typeTab = value; } }

        public int Id { get { return this.nId; } set { this.nId = value; } }
    }
};