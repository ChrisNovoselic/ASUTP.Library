using System;
using System.Threading;
using System.ComponentModel; //BackgroundWorker
using System.Windows.Forms;
using System.Drawing;

namespace HClassLibrary
{
    /// <summary>
    /// Класс для описания окна визуализации длительного выполнения операции
    /// </summary>
    public partial class FormWait : Form
    {
        private enum STATE { UNKNOWN = -1, UNVISIBLED, SHOWING, VISIBLED, CLOUSING }

        private STATE _state;
        /// <summary>
        /// Объект для блокирования изменения значения счетчика вызовов окна на отображение
        /// </summary>
        private object lockState;
        /// <summary>
        /// Счетчик вызовов окна на отображение
        /// </summary>
        private int _waitCounter;
        ///// <summary>
        ///// Дата/время начала отображения окна
        ///// </summary>
        //private DateTime m_dtStartShow;
        /// <summary>
        /// Максимальное время отображения окна (секунды)
        /// </summary>
        public static int s_secMaxShowing = 6;
        /// <summary>
        /// Поток обработки событий по изменению состоянию окна - отображение
        /// </summary>
        private BackgroundWorker //Thread
            m_threadShowDialog
            ;
        private enum INDEX_SYNCSTATE { UNKNOWN = -1, EXIT, SHOWDIALOG, CLOSE, COUNT_INDEX_SYNCSTATE }

        private DelegateFunc delegateFuncClose
            //, delegateFuncShowDialog
            ;

        /// <summary>
        /// Признак запуска окна
        /// </summary>
        private bool isContinue { get { return _waitCounter > 0; } }

        private Semaphore m_semaRunWorkerCompleted;
        /// <summary>
        /// Ссылка на самого себя
        ///  для реализации создания одного и только одного объекта в границах приложения
        /// </summary>
        private static FormWait _this;
        private Point _location;
        //private bool _focused;
        private Form _parent;
        /// <summary>
        /// Получить объект из внешенго кода
        /// </summary>
        public static FormWait This { get { if (_this == null) _this = new FormWait(); else ; return _this; } }
        /// <summary>
        /// Конструктор - основной (без параметров)
        /// </summary>
        private FormWait()
            : base()
        {
            InitializeComponent();
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            //Инициализация объектов подсчета кол-ва вызовов на отображение формы
            lockState = new object();
            _state = STATE.UNVISIBLED;
            _waitCounter = 0;
            //m_dtStartShow = DateTime.MinValue;

            m_semaRunWorkerCompleted = new Semaphore(0, 1);

            m_threadShowDialog = new BackgroundWorker();
            m_threadShowDialog.DoWork += new DoWorkEventHandler(fThreadProcShowDialog_DoWork);
            m_threadShowDialog.RunWorkerCompleted += new RunWorkerCompletedEventHandler(fThreadProcShowDialog_RunWorkerCompleted);

            //delegateFuncShowDialog = new DelegateFunc(showDialog);
            delegateFuncClose = new DelegateFunc(close);

            this.Shown += new EventHandler(FormWait_Shown);
            //this.HandleCreated += new EventHandler(FormWait_Shown);
            //FormClosed += new FormClosedEventHandler(FormWait_FormClosed);
            //this.HandleDestroyed += new EventHandler(FormWait_HandleDestroyed);
        }
        /// <summary>
        /// Вызвать на отображение окно
        /// </summary>
        /// <param name="ptLocationParent">Позиция отображения родительского окна</param>
        /// <param name="szParent">Размер родительского окна</param>
        public void StartWaitForm(Point ptParent, Size szParent)
        {
            //Зафиксировать вХод в 'FormWait::StartWaitForm'
            //Logging.Logg().Warning(@"FormWait::StartWaitForm () - вХод...", Logging.INDEX_MESSAGE.NOT_SET);
            //Logging.Logg().Warning(@"FormWait::StartWaitForm (_state=" + _state.ToString() + @", _waitCounter=" + _waitCounter + @") - вХод...", Logging.INDEX_MESSAGE.NOT_SET);

            lock (lockState)
            {
                ////Зафиксировать вХод в 'FormWait::StartWaitForm'
                //Logging.Logg().Warning(@"FormWait::StartWaitForm (_state=" + _state.ToString () + @", _waitCounter=" + _waitCounter + @") - вХод...", Logging.INDEX_MESSAGE.NOT_SET);
                //Console.WriteLine(@"FormWait::START; _state=" + _state.ToString () + @", waitCounter=" + waitCounter);

                _waitCounter++;

                if (_state == STATE.UNVISIBLED)
                {
                    //Установить координаты для отображения
                    setLocation(ptParent, szParent);
                    //Рпзрешить отображение
                    m_threadShowDialog.RunWorkerAsync();

                    _state = STATE.SHOWING;
                }
                else
                    //if (_state == STATE.CLOUSING)
                    //    _waitCounter++;
                    //else
                    ;
            }
        }
        /// <summary>
        /// Снять с отображения окно
        /// </summary>
        /// <param name="bStopped"></param>
        public void StopWaitForm(bool bExit = false)
        {
            lock (lockState)
            {
                ////Зафиксировать вХод в 'FormWait::StartWaitForm'
                //Logging.Logg().Warning(@"FormWait::StopWaitForm (_state=" + _state.ToString() + @", _waitCounter=" + _waitCounter + @") - вХод...", Logging.INDEX_MESSAGE.NOT_SET);

                if (_waitCounter > 0)
                    _waitCounter--;
                else
                    ;

                if (_state == STATE.VISIBLED)
                {
                    if (InvokeRequired == true)
                        BeginInvoke(delegateFuncClose);
                    else
                        close();

                    _state = STATE.CLOUSING;
                }
                else
                    //if (_state == STATE.SHOWING)
                    //    _waitCounter--;
                    //else
                    //
                    ;

                //Console.WriteLine(@"FormWait::STOP; waitCounter=" + waitCounter);
            }

            //Logging.Logg().Warning(@"FormWait::StopWaitForm (_state=" + _state.ToString() + @", _waitCounter=" + _waitCounter + @") - вЫХод...", Logging.INDEX_MESSAGE.NOT_SET);
        }

        private void showDialog()
        {
            //Logging.Logg().Debug(@"FormWait::showDialog () - !!!!!!!!!!!!!", Logging.INDEX_MESSAGE.NOT_SET);

            Location = _location;
            ShowDialog();
        }
        /// <summary>
        /// Делегат для вызова метода закрытия окна
        /// </summary>
        private void close()
        {
            Close();
        }
        /// <summary>
        /// Установить позицию окна
        ///  в зависимости от позиции родительского
        /// </summary>
        /// <param name="ptLocationParent">Позиция отображения родительского окна</param>
        /// <param name="szParent">Размер родительского окна</param>
        private void setLocation(Point ptParent, Size szParent)
        {
            //_parent = parent;
            //_focused = focused; //_parent.Focused;
            //_location = new Point(_parent.Location.X + (_parent.Size.Width - this.Width) / 2, _parent.Location.Y + (_parent.Size.Height - this.Height) / 2);
            _location = new Point(ptParent.X + (szParent.Width - this.Width) / 2, ptParent.Y + (szParent.Height - this.Height) / 2);
        }
        /// <summary>
        /// Обработчик события - 
        /// </summary>
        /// <param name="sender">Объект, инициоровавший событие - this</param>
        /// <param name="e">Аргумент события</param>
        private void FormWait_Shown(object sender, EventArgs e)
        {
            lock (lockState)
            {
                _state = STATE.VISIBLED;

                if (isContinue == false)
                {
                    _state = STATE.CLOUSING;

                    if (InvokeRequired == true)
                        BeginInvoke(delegateFuncClose);
                    else
                        close();
                }
                else
                    ;
            }
        }
        ///// <summary>
        ///// Обработчик события - 
        ///// </summary>
        ///// <param name="sender">Объект, инициоровавший событие - this</param>
        ///// <param name="e">Аргумент события</param>
        //private void FormWait_FormClosed(object sender, FormClosedEventArgs e)
        //{
        //}

        //private void FormWait_HandleDestroyed(object sender, EventArgs e)
        //{
        //    m_arSyncStates[(int)INDEX_SYNCSTATE.CLOSE - 1].Set();
        //}
        ///// <summary>
        ///// Потоковая функция отображения окна
        ///// </summary>
        ///// <param name="data">Аргумент при запуске потока</param>
        //private void fThreadProcShowDialog(object data)
        private void fThreadProcShowDialog_DoWork(object obj, DoWorkEventArgs ev)
        {
            ////Зафиксировать событие
            //Logging.Logg().Debug(@"FormMainBase::fThreadProcShowDialog_DoWork () - _state=" + _state.ToString() + @" - ...", Logging.INDEX_MESSAGE.NOT_SET);
            //Console.WriteLine(@"FormMainBase::fThreadProcShowDialog_DoWork () - indx=" + indx.ToString() + @" - ...");

            //if (InvokeRequired == true)
            //    BeginInvoke(delegateFuncShowDialog);
            //else
            showDialog();

            //Logging.Logg().Debug(@"FormMainBase::fThreadProcShowDialog () - indx=" + indx.ToString() + @" - ...", Logging.INDEX_MESSAGE.NOT_SET);
        }

        private void fThreadProcShowDialog_RunWorkerCompleted(object obj, RunWorkerCompletedEventArgs ev)
        {
            //Logging.Logg().Debug(@"FormMainBase::fThreadProcShowDialog_RunWorkerCompleted () - _state=" + _state.ToString() + @" - ...", Logging.INDEX_MESSAGE.NOT_SET);

            lock (lockState)
            {
                _state = STATE.UNVISIBLED;

                if (isContinue == true)
                {
                    _state = STATE.SHOWING;

                    m_threadShowDialog.RunWorkerAsync();
                }
                else
                    ;
            }
        }
    }
}