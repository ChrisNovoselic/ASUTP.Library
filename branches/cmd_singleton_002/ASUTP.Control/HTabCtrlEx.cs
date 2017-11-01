using ASUTP.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace ASUTP.Control {
    public partial class HTabCtrlEx : System.Windows.Forms.TabControl {
        /// <summary>
        /// Признак разрешения изменения порядка следования вкладок
        /// </summary>
        private const bool ALLOW_DROP = false;
        /// <summary>
        /// Перечисление - типы вкладок (фиксированные, плавающие)
        /// </summary>
        public enum TYPE_TAB { FIXED, FLOAT };
        /// <summary>
        /// Структура для хранения свойств вкладки
        /// </summary>
        protected struct PropertyTab {
            public PropertyTab (int id, TYPE_TAB type)
            {
                this.id = id;
                this.type = type;
            }
            /// <summary>
            /// Идентификатор вкладки
            /// </summary>
            public int id;
            /// <summary>
            /// Тип вкладки
            /// </summary>
            public TYPE_TAB type;
        }
        /// <summary>
        /// Список объектов со свойствами вкладок
        /// </summary>
        protected List<PropertyTab> m_listPropTabs;
        /// <summary>
        /// Свойства крайней закрытой вкладки
        /// </summary>
        protected PropertyTab? _propTabLastRemoved;
        /// <summary>
        /// Параметры для позиционирования пиктогамм на заголовке вкладки
        /// </summary>
        private static RectangleF s_rectPositionImg = new RectangleF (18, 4, 14, 14);
        /// <summary>
        /// Перечисление - индексы пиктограмм, размещаемых на заголовкее вкладки
        /// </summary>
        private enum INDEX_BITMAP { FLOAT, CLOSE, COUNT_BITMAP };
        /// <summary>
        /// Перечисление - индексы возможных состояний пиктограмм
        /// </summary>
        private enum INDEX_STATE_BITMAP { NON_ACTIVE, IN_ACTIVE, COUNT_STATE_BITMAP };
        /// <summary>
        /// Массив пиктограмм для отображения на заголовке вкладки
        /// </summary>
        private Icon [] m_arBitmap;
        /// <summary>
        /// Тип делегата для событий/обработки
        /// </summary>
        /// <param name="sender">Объект, инициировавший событие</param>
        /// <param name="e">Аргумент события</param>
        public delegate void DelegateHTabCtrlEx (object sender, HTabCtrlExEventArgs e);
        /// <summary>
        /// Событие - закрыть вкладку
        /// </summary>
        public event DelegateHTabCtrlEx EventHTabCtrlExClose;
        /// <summary>
        /// Событие - Отделить вкладку от главного окна
        /// </summary>
        public event DelegateHTabCtrlEx EventHTabCtrlExFloat;

        private int iPrevSelectedIndex;
        /// <summary>
        /// Индекс вкладки, бывшей активной, перед текущей активной вкладкой
        /// </summary>
        public int PrevSelectedIndex
        {
            get
            {
                return iPrevSelectedIndex;
            }
            set
            {
                //if (!(iPrevSelectedIndex == value)) {
                EventPrevSelectedIndexChanged (iPrevSelectedIndex);
                iPrevSelectedIndex = value;
                //} else ;
            }
        }
        /// <summary>
        /// Событие - изменение индекса предыдущей активной вкладки
        /// </summary>
        public event DelegateIntFunc EventPrevSelectedIndexChanged;
        /// <summary>
        /// Конструктор - основной (без параметров)
        /// </summary>
        public HTabCtrlEx ()
        {
            InitializeComponent ();

            this.AllowDrop = ALLOW_DROP; // установить признак возможности изменения порядка следования вкладок
            iPrevSelectedIndex = -1; // предыдущей активной вкладки нет
        }

        public HTabCtrlEx (IContainer container)
        {
            container.Add (this);

            InitializeComponent ();

            this.AllowDrop = ALLOW_DROP;
            iPrevSelectedIndex = -1;
        }
        /// <summary>
        /// Возвратить пиктограмму для отображения по ее типу и состоянию
        /// </summary>
        /// <param name="indx">Тип пиктограммы</param>
        /// <param name="state">Состояние пиктограммы</param>
        /// <returns>Пиктограмма для отображения</returns>
        private Bitmap getBitmap (INDEX_BITMAP indx, INDEX_STATE_BITMAP state)
        {
            return m_arBitmap [(int)indx * (int)INDEX_STATE_BITMAP.COUNT_STATE_BITMAP + (int)state].ToBitmap ();
        }
        /// <span class="code-SummaryComment"><summary></span>
        /// override to draw the close button
        /// <span class="code-SummaryComment"></summary></span>
        /// <span class="code-SummaryComment"><param name="e"></param></span>
        protected override void OnDrawItem (DrawItemEventArgs e)
        {
            RectangleF tabTextAreaText = RectangleF.Empty
                , tabTextAreaImg = RectangleF.Empty;
            for (int nIndex = 0; nIndex < this.TabCount; nIndex++) {
                Image img;
                INDEX_STATE_BITMAP state;
                tabTextAreaText = (RectangleF)this.GetTabRect (nIndex);
                //e.Graphics.IntersectClip(tabTextAreaText);
                tabTextAreaImg = new RectangleF (tabTextAreaText.X + tabTextAreaText.Width - s_rectPositionImg.X, s_rectPositionImg.Y, s_rectPositionImg.Width, s_rectPositionImg.Height);
                //if (nIndex > 0) {
                if (!(nIndex == this.SelectedIndex)) {
                    state = INDEX_STATE_BITMAP.NON_ACTIVE;
                } else {
                    state = INDEX_STATE_BITMAP.IN_ACTIVE;
                }

                using (img = getBitmap (INDEX_BITMAP.CLOSE, state)) {
                    e.Graphics.DrawImage (img, tabTextAreaImg);
                    //Console.WriteLine (@"OnDrawItem () - " + @"Индекс=" + nIndex + @"; X:" + tabTextArea.X + @"; width:" + tabTextArea.Width);
                }

                if (m_listPropTabs [nIndex].type == TYPE_TAB.FLOAT) {
                    tabTextAreaImg = new RectangleF (tabTextAreaImg.X - (s_rectPositionImg.Width + 1), s_rectPositionImg.Y, s_rectPositionImg.Width, s_rectPositionImg.Height);

                    using (img = getBitmap (INDEX_BITMAP.FLOAT, state)) {
                        e.Graphics.DrawImage (img, tabTextAreaImg);
                    }
                } else
                    ;
                //}
                //else
                //    ;

                string str = this.TabPages [nIndex].Text;
                StringFormat stringFormat = new StringFormat ();
                stringFormat.Alignment = StringAlignment.Near;
                using (SolidBrush brush = new SolidBrush (this.TabPages [nIndex].ForeColor)) {
                    /*Draw the tab header text*/
                    e.Graphics.DrawString (str, this.Font, brush, tabTextAreaText, stringFormat);
                }
            }
        }
        /// <span class="code-SummaryComment"><summary></span>
        /// Get the stream of the embedded bitmap image
        /// <span class="code-SummaryComment"></summary></span>
        /// <span class="code-SummaryComment"><param name="filename"></param></span>
        /// <span class="code-SummaryComment"><returns></returns></span>
        private Stream GetContentFromResource (string filename)
        {
            Assembly asm = Assembly.GetExecutingAssembly ();
            Stream stream = asm.GetManifestResourceStream ("HTabCtrlLibrary." + filename);

            return stream;
        }

        protected override void OnMouseMove (MouseEventArgs e)
        {
            if (DesignMode == false) {
                if ((e.Button == MouseButtons.Left)
                    && (!(_draggedTab == null))) {
                    this.DoDragDrop (_draggedTab, DragDropEffects.Move);

                    base.OnMouseMove (e);
                } else {
                    Image img;
                    INDEX_STATE_BITMAP state;
                    Graphics g = CreateGraphics ();
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    for (int nIndex = 0; nIndex < this.TabCount; nIndex++) {
                        RectangleF tabTextAreaText = (RectangleF)this.GetTabRect (nIndex)
                            , tabTextAreaClose = new RectangleF (tabTextAreaText.X + tabTextAreaText.Width - s_rectPositionImg.X, s_rectPositionImg.Y, s_rectPositionImg.Width, s_rectPositionImg.Height)
                            , tabTextAreaFloat = new RectangleF (tabTextAreaClose.X - (s_rectPositionImg.Width + 1), s_rectPositionImg.Y, s_rectPositionImg.Width, s_rectPositionImg.Height);
                        //Console.WriteLine(@"OnMouseMove () - " + @"Индекс=" + nIndex + @"; X:" + tabTextArea.X + @"; width:" + tabTextArea.Width);

                        Point pt = new Point (e.X, e.Y);
                        if (tabTextAreaClose.Contains (pt) == true)
                            state = INDEX_STATE_BITMAP.IN_ACTIVE;
                        else
                            if (!(nIndex == this.SelectedIndex))
                            state = INDEX_STATE_BITMAP.NON_ACTIVE;
                        else
                            state = INDEX_STATE_BITMAP.IN_ACTIVE;

                        img = getBitmap (INDEX_BITMAP.CLOSE, state);

                        using (img) {
                            g.DrawImage (img, tabTextAreaClose);
                        }

                        if (m_listPropTabs [nIndex].type == TYPE_TAB.FLOAT) {
                            if (tabTextAreaFloat.Contains (pt))
                                state = INDEX_STATE_BITMAP.IN_ACTIVE;
                            else
                                if (!(nIndex == this.SelectedIndex))
                                state = INDEX_STATE_BITMAP.NON_ACTIVE;
                            else
                                state = INDEX_STATE_BITMAP.IN_ACTIVE;

                            img = getBitmap (INDEX_BITMAP.FLOAT, state);

                            g.DrawImage (img, tabTextAreaFloat);
                        } else
                            ;
                    }
                    g.Dispose ();
                }
            } else
                ;
        }

        protected override void OnMouseDown (MouseEventArgs e)
        {
            if ((DesignMode == false)
                /* && (SelectedIndex > 0)*///Здесь запрет закрыть вкладку с индексом "0"
                                           //&& (TabPages.Count > 1)
                ) {
                RectangleF tabTextAreaText = (RectangleF)this.GetTabRect (SelectedIndex)
                    , tabTextAreaClose = new RectangleF (tabTextAreaText.X + tabTextAreaText.Width - s_rectPositionImg.X, s_rectPositionImg.Y, s_rectPositionImg.Width, s_rectPositionImg.Height)
                    , tabTextAreaFloat = new RectangleF (tabTextAreaClose.X - (s_rectPositionImg.Width + 1), s_rectPositionImg.Y, s_rectPositionImg.Width, s_rectPositionImg.Height);

                Point pt = new Point (e.X, e.Y);
                if (tabTextAreaClose.Contains (pt) == true) {
                    if (MessageBox.Show ("Вы закрывайте вкладку с объектом: " + this.TabPages [SelectedIndex].Text.TrimEnd () + ". Продолжить?", "Подтверждение", MessageBoxButtons.YesNo) == DialogResult.No)
                        return;
                    else
                        ;

                    //Fire Event to Client
                    if (!(EventHTabCtrlExClose == null))
                        EventHTabCtrlExClose (this, new HTabCtrlExEventArgs (m_listPropTabs [SelectedIndex].id, SelectedIndex, this.TabPages [SelectedIndex].Text.Trim ()));
                    else
                        ;
                } else
                    if ((m_listPropTabs [SelectedIndex].type == TYPE_TAB.FLOAT)
                        && (tabTextAreaFloat.Contains (pt) == true))
                    if (!(EventHTabCtrlExFloat == null))
                        EventHTabCtrlExFloat (this, new HTabCtrlExEventArgs (m_listPropTabs [SelectedIndex].id, SelectedIndex, this.TabPages [SelectedIndex].Text.Trim ()));
                    else
                        ;
                else
                        if (this.AllowDrop == true) {
                    _draggedTab = getPointedTab ();

                    base.OnMouseDown (e);
                } else
                    ;
            }
        }

        //public void TabPagesClear()
        //{
        //    while (TabCount > 1)
        //        TabPages.RemoveAt (TabCount - 1);

        //    m_listTypeTabs.Clear ();
        //}
        /// <summary>
        /// Добавить вкладку
        /// </summary>
        /// <param name="tab">Объект вкладки</param>
        /// <param name="name">Наименование(заголовок) вкладки</param>
        /// <param name="id">Идентификатор вкладки</param>
        /// <param name="typeTab">Тип вкладки</param>
        public void AddTabPage (System.Windows.Forms.Control tab, string name, int id, TYPE_TAB typeTab)
        {
            m_listPropTabs.Add (new PropertyTab (id, typeTab));
            this.TabPages.Add (name, getNameTab (name, typeTab));
            this.TabPages [TabCount - 1].Controls.Add (tab);
        }
        /// <summary>
        /// Удалить активную вкладку
        /// </summary>
        /// <returns>Признак выполнения операции удаления вкладки</returns>
        public bool RemoveTabPage ()
        {
            bool bRes = RemoveTabPage (this.SelectedIndex);

            if (bRes == false)
                Console.WriteLine (@"Ошибка удаления вкладки [" + SelectedTab.Text + "]...");
            else
                ;

            return bRes;
        }
        ///// <summary>
        ///// Удалить вкладку по заголовку
        ///// </summary>
        ///// <param name="name">Заголовок вкладки</param>
        ///// <returns>Признак выполнения операции удаления вкладки</returns>
        //public bool RemoveTabPage (string name) {
        //    bool bRes = RemoveTabPage (this.TabPages.IndexOfKey (name.Trim ()));

        //    if (bRes == false)
        //        Console.WriteLine (@"Ошибка удаления вкладки [" + name + "]...");
        //    else
        //        ;

        //    return bRes;
        //}
        /// <summary>
        /// Удалить вкладку по индексу
        /// </summary>
        /// <param name="indx">Индекс вкладки</param>
        /// <returns>Признак выполнения операции удаления вкладки</returns>
        public bool RemoveTabPage (int indx)
        {
            bool bRes = true;

            if ((!(indx < 0))
                && (indx < this.TabCount)
                && (indx < m_listPropTabs.Count)) {
                _propTabLastRemoved = m_listPropTabs [indx];
                m_listPropTabs.RemoveAt (indx);
                this.TabPages.RemoveAt (indx);

                if (this.TabPages.Count == 0)
                    iPrevSelectedIndex = -1;
                else
                    ;
            } else
                bRes = false;

            return bRes;
        }
        /// <summary>
        /// Возвратить строку для заголовка вкладки с учетом размещения пиктограмм
        /// </summary>
        /// <param name="text">Тест для заголовка</param>
        /// <param name="type">Тип вкладки (количество размещаемых пиктограмм)</param>
        /// <returns>Строка заголовка вкладки</returns>
        private static string getNameTab (string text, TYPE_TAB type)
        {
            int cntSpace = 5;

            if (type == TYPE_TAB.FLOAT)
                cntSpace += 4;
            else
                ;

            return new string (' ', 1) + text + new string (' ', cntSpace);
        }
        /// <summary>
        /// Возвратить индекс вкладки по ее идентификатору
        /// </summary>
        /// <param name="id">Идентификатор вкладки</param>
        /// <returns>Индекс вкладки</returns>
        public int IndexOfID (int id)
        {
            int iRes = -1;

            foreach (PropertyTab propTab in m_listPropTabs)
                if (propTab.id == id) {
                    iRes = m_listPropTabs.IndexOf (propTab);

                    break;
                } else
                    ;

            return iRes;
        }

        public int GetTabPageId ()
        {
            return GetTabPageId (SelectedIndex);
        }

        public int GetTabPageId (int indx)
        {
            int iRes = -1;

            if (indx == TabPages.Count)
                if (!(_propTabLastRemoved == null))
                    iRes = _propTabLastRemoved.Value.id;
                else
                    ;
            else
                if ((!(indx < 0))
                    && (TabPages.Count > 0))
                if (indx < TabPages.Count)
                    iRes = m_listPropTabs [indx].id;
                else
                    ;
            else
                ;

            return iRes;
        }
        /// <summary>
        /// Возвратить строку-перечисление с идентификаторами отображаемых вкладок через разделитель ','
        /// </summary>
        public string VisibleIDs
        {
            get
            {
                string strRes = string.Empty;

                foreach (PropertyTab tab in m_listPropTabs)
                    strRes += tab.id + @",";

                if (strRes.Length > 1)
                    strRes = strRes.Substring (0, strRes.Length - 1);
                else
                    ;

                return strRes;
            }
        }
        /// <summary>
        /// Объект, перемещаемой вкладки
        /// </summary>
        private TabPage _draggedTab;

        protected override void OnMouseUp (MouseEventArgs e)
        {
            _draggedTab = null;

            base.OnMouseUp (e);
        }

        protected override void OnDragOver (DragEventArgs drgevent)
        {
            TabPage draggedTab = (TabPage)drgevent.Data.GetData (typeof (TabPage));
            TabPage pointedTab = getPointedTab ();

            if ((draggedTab == _draggedTab)
                && (!(pointedTab == null))) {
                drgevent.Effect = DragDropEffects.Move;

                if (pointedTab != draggedTab)
                    swapTabPages (draggedTab, pointedTab);
                else
                    ;
            }

            base.OnDragOver (drgevent);
        }
        /// <summary>
        /// Возвратить вкладку по текущей позиции курсора указателя
        /// </summary>
        /// <returns>Объект-вкладка</returns>
        private TabPage getPointedTab ()
        {
            for (int i = 0; i < this.TabPages.Count; i++)
                if (this.GetTabRect (i).Contains (this.PointToClient (Cursor.Position)))
                    return this.TabPages [i];
                else
                    ;

            return null;
        }
        /// <summary>
        /// Поменять местами индексы, объекты на вкладках
        /// </summary>
        /// <param name="src">Вкладка-источник</param>
        /// <param name="dst">Вкладка-назначение</param>
        private void swapTabPages (TabPage src, TabPage dst)
        {
            int srci = this.TabPages.IndexOf (src);
            int dsti = this.TabPages.IndexOf (dst);

            this.TabPages [dsti] = src;
            this.TabPages [srci] = dst;

            if (this.SelectedIndex == srci)
                this.SelectedIndex = dsti;
            else if (this.SelectedIndex == dsti)
                this.SelectedIndex = srci;

            this.Refresh ();
        }
    }

    public class HTabCtrlExEventArgs : EventArgs {
        private int nTabIndex = -1;
        private string strHeaderText = string.Empty;
        private HTabCtrlEx.TYPE_TAB typeTab;
        private int nId;

        public HTabCtrlExEventArgs (int nId, int nTabIndex, string text)
        {
            this.nTabIndex = nTabIndex;
            this.strHeaderText = text;
            this.nId = nId;
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

        public HTabCtrlEx.TYPE_TAB TabType
        {
            get
            {
                return this.typeTab;
            }
            set
            {
                this.typeTab = value;
            }
        }

        public int Id
        {
            get
            {
                return this.nId;
            }
            set
            {
                this.nId = value;
            }
        }
    }
}
