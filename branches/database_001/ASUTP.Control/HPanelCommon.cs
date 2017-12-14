using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ASUTP.Control
{
    /// <summary>
    /// Класс панели с макетом размещения дочерних элементов управления
    /// </summary>
    public abstract class HPanelCommon : TableLayoutPanel//, IDisposable
    {
        /// <summary>
        /// Конструктор - основной (с аргументами)
        /// </summary>
        /// <param name="iColumnCount">Количество столбцов</param>
        /// <param name="iRowCount">Количество строк</param>
        public HPanelCommon (int iColumnCount/* = 16*/, int iRowCount/* = 16*/)
        {
            if (iColumnCount > 0)
                this.ColumnCount = iColumnCount;
            else
                ;
            if (iRowCount > 0)
                this.RowCount = iRowCount;
            else
                ;

            InitializeComponent ();

            iActive = -1;
        }

        /// <summary>
        /// Конструктор - дополнительный (с аргументами)
        /// </summary>
        /// <param name="container">Родительский контейнер для элемента интерфейса</param>
        /// <param name="iColumnCount">Количество столбцов</param>
        /// <param name="iRowCount">Количество строк</param>
        public HPanelCommon (IContainer container, int iColumnCount/* = 16*/, int iRowCount/* = 16*/)
        {
            container.Add (this);

            if (iColumnCount > 0)
                this.ColumnCount = iColumnCount;
            else
                ;
            if (iRowCount > 0)
                this.RowCount = iRowCount;
            else
                ;

            InitializeComponent ();

            iActive = -1;
        }
        /// <summary>
        /// Признак активности панели
        ///  (-1 - исходное, 0 - старт, 1 - активная)
        /// </summary>
        private int iActive;
        /// <summary>
        /// Признак активности панели
        /// </summary>
        public bool Actived
        {
            get
            {
                return (iActive > 0) && (iActive % 2 == 1);
            }
        }
        /// <summary>
        /// Признак выполнения запуска (вызов метода 'Start')
        /// </summary>
        public bool Started
        {
            get
            {
                return !(iActive < 0);
            }
        }
        /// <summary>
        /// Признак 
        /// </summary>
        public bool IsFirstActivated
        {
            get
            {
                return iActive == 1;
            }
        }
        /// <summary>
        /// Запустить на выполнение панель
        /// </summary>
        public virtual void Start ()
        {
            iActive = 0;
        }
        /// <summary>
        /// Остановить панель
        /// </summary>
        public virtual void Stop ()
        {
            iActive = -1;
        }
        /// <summary>
        /// Активировать/деактивировать панель
        /// </summary>
        /// <param name="active">Признак активации/деактивации</param>
        /// <returns>Признак изменения состояния элемента управления</returns>
        public virtual bool Activate (bool active)
        {
            bool bRes = false;

            if ((Started == false)
                && (active == true))
                throw new Exception (@"HPanelCommon::Activate (" + active.ToString () + @") - не выполнен метод 'Старт' ...");
            else
                ;

            if (!(Actived == active)) {
                this.iActive++;

                bRes = true;
            } else
                ;

            return bRes;
        }
        /// <summary>
        /// Инициализировать размеры/пропорции ячеек объекта
        /// </summary>
        /// <param name="cols">Количество столбцов в макете/сетке</param>
        /// <param name="rows">Количество строк в макете/сетке</param>
        protected abstract void initializeLayoutStyle (int cols = -1, int rows = -1);
        /// <summary>
        /// Инициализировать размеры/пропорции ячеек объекта
        ///  (равномерное распределение)
        /// </summary>
        /// <param name="cols">Количество столбцов в макете/сетке</param>
        /// <param name="rows">Количество строк в макете/сетке</param>
        protected void initializeLayoutStyleEvenly (int cols = -1, int rows = -1)
        {
            if (cols > 0)
                this.ColumnCount = cols;
            else
                ;

            if (rows > 0)
                this.RowCount = rows;
            else
                ;

            float val = (float)100 / this.ColumnCount;
            //Добавить стили "ширина" столлбцов
            for (int s = 0; s < this.ColumnCount - 0; s++)
                this.ColumnStyles.Add (new ColumnStyle (SizeType.Percent, val));
            //this.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize, val));

            val = (float)100 / this.RowCount;
            //Добавить стили "высота" строк
            for (int s = 0; s < this.RowCount - 0; s++)
                this.RowStyles.Add (new RowStyle (SizeType.Percent, val));
            //this.RowStyles.Add(new RowStyle(SizeType.AutoSize, val));
        }
        /// <summary>
        /// Индекс строки в [0]-ой ячейке которой не размещен ни один из элементов управления
        ///  (для размещения очередного элемента управления)
        /// </summary>
        public int IndexLastRowControl
        {
            get
            {
                int iRes = 0;

                while (!(GetControlFromPosition (0, iRes) == null))
                    iRes++;

                return iRes;
            }
        }
        /// <summary>
        /// Опции при поиске
        /// </summary>
        [Flags]
        protected enum OptionFindControl {
            /// <summary>
            /// Поиск осуществляется в поле 'Name' элемента управления
            /// </summary>
            Name,
            /// <summary>
            /// Поиск осуществляется в поле 'Tag' элемента управления
            /// </summary>
            Tag
        }
        /// <summary>
        /// Найти элемент управления на панели идентификатору
        /// </summary>
        /// <param name="id">Идентификатор-строка элемента управления</param>
        /// <param name="opt">Опция при поиске: в каком поле искать идентификатор</param>
        /// <param name="bSearchAllChildren">True - если требуется найти все дочерние элементы</param>
        /// <returns>Элемент управления на панели, если не найден null</returns>
        protected System.Windows.Forms.Control findControl (string id, OptionFindControl opt = OptionFindControl.Name, bool bSearchAllChildren = true)
        {
            System.Windows.Forms.Control ctrlRes = null;

            System.Windows.Forms.Control [] arFind;

            arFind = Controls.Find (id, bSearchAllChildren);
            if (arFind.Length == 1)
                ctrlRes = arFind [0];
            else if (arFind.Length == 0)
                ctrlRes = new System.Windows.Forms.Control ();
            else
                ctrlRes = new System.Windows.Forms.Control ();

            return ctrlRes;
        }
        #region Обязательный код для корректного освобождения памяти
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose (bool disposing)
        {
            if (disposing && (components != null)) {
                components.Dispose ();
            }
            base.Dispose (disposing);
        }
        #endregion

        #region Код, автоматически созданный конструктором компонентов
        private void InitializeComponent ()
        {
            this.SuspendLayout ();

            this.Dock = DockStyle.Fill;
            //this.CellBorderStyle = TableLayoutPanelCellBorderStyle.Single;            

            //initializeLayoutStyle();

            this.ResumeLayout (false);
            this.PerformLayout ();
        }
        #endregion
    }
}
