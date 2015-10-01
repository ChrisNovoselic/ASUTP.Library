using System;
using System.Threading;
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
        private Thread
            m_threadShow
        /// <summary>
        /// Поток обработки событий по изменению состоянию окна - снятие с отображения
        /// </summary>
            , m_threadHide
        /// <summary>
        /// Поток обработки событий по изменению состоянию окна - отображение/снятие с отображения
        /// </summary>
            , m_threadState
            ;
        private enum INDEX_SYNCSTATE { UNKNOWN = -1, EXIT, SHOWDIALOG, CLOSE, COUNT_INDEX_SYNCSTATE }
        private AutoResetEvent[] m_arSyncManaged;

        private DelegateFunc delegateFuncClose
            , delegateFuncShowDialog;

        /// <summary>
        /// Признак запуска окна
        /// </summary>
        private bool isRepeat { get { return _waitCounter > 0; } }

        private AutoResetEvent [] m_arSyncStates;
        /// <summary>
        /// Ссылка на самого себя
        ///  для реализации создания одного и только одного объекта в границах приложения
        /// </summary>
        private static FormWait _this;
        private Point _location;
        private Form _parent;
        /// <summary>
        /// Получить объект из внешенго кода
        /// </summary>
        public static FormWait This { get { if (_this == null) _this = new FormWait (); else ; return _this; } }
        /// <summary>
        /// Конструктор - основной (без параметров)
        /// </summary>
        private FormWait () : base ()
        {
            InitializeComponent();
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            //Инициализация объектов подсчета кол-ва вызовов на отображение формы
            lockState = new object ();
            _state = STATE.UNVISIBLED;
            _waitCounter = 0;
            //m_dtStartShow = DateTime.MinValue;
            
            if (m_arSyncStates == null)
            {
                m_arSyncStates = new AutoResetEvent[(int)INDEX_SYNCSTATE.COUNT_INDEX_SYNCSTATE - 1];
                for (int i = (int)INDEX_SYNCSTATE.SHOWDIALOG; i < (int)INDEX_SYNCSTATE.COUNT_INDEX_SYNCSTATE; i++)
                    m_arSyncStates[i - 1] = new AutoResetEvent(false);
            }
            else
                ;

            if (m_arSyncManaged == null)
            {
                m_arSyncManaged = new AutoResetEvent[(int)INDEX_SYNCSTATE.COUNT_INDEX_SYNCSTATE];
                for (int i = 0; i < (int)INDEX_SYNCSTATE.COUNT_INDEX_SYNCSTATE; i++)
                    m_arSyncManaged[i] = new AutoResetEvent(false);
            }
            else
                ;

            m_threadShow = new Thread(new ParameterizedThreadStart(fThreadProcShowDialog));
            m_threadShow.Name = @"FormWait.Thread - SHOWDIALOG";
            m_threadShow.IsBackground = true;
            m_threadShow.Start(null);

            m_threadHide = new Thread(new ParameterizedThreadStart(fThreadProcClose));
            m_threadHide.Name = @"FormWait.Thread - CLOSE";
            m_threadHide.IsBackground = true;
            m_threadHide.Start(null);

            m_threadState = new Thread(new ParameterizedThreadStart(fThreadProcState));
            m_threadState.Name = @"FormWait.Thread - STATE";
            m_threadState.IsBackground = true;
            m_threadState.Start(null);

            delegateFuncShowDialog = new DelegateFunc (showDialog);
            delegateFuncClose = new DelegateFunc (close);

            Shown += new EventHandler(FormWait_Shown);
            FormClosed +=new FormClosedEventHandler(FormWait_FormClosed);
        }
        /// <summary>
        /// Вызвать на отображение окно
        /// </summary>
        /// <param name="ptLocationParent">Позиция отображения родительского окна</param>
        /// <param name="szParent">Размер родительского окна</param>
        public void StartWaitForm(Point ptLocationParent, Size szParent)
        {
            //Зафиксировать вХод в 'FormWait::StartWaitForm'
            Logging.Logg().Warning(@"FormWait::StartWaitForm () - вХод...", Logging.INDEX_MESSAGE.NOT_SET);

            lock (lockState)
            {
                //Console.WriteLine(@"FormWait::START; waitCounter=" + waitCounter);

                if (_state == STATE.UNVISIBLED)
                {
                    //Установить координаты для отображения
                    setLocation(ptLocationParent, szParent);
                    //Рпзрешить отображение
                    m_arSyncManaged[(int)INDEX_SYNCSTATE.SHOWDIALOG].Set();

                    _state = STATE.SHOWING;
                }
                else
                    if (_state == STATE.CLOUSING)
                    {
                        _waitCounter++;
                    }
                    else
                        ;
            }
        }
        /// <summary>
        /// Снять с отображения окно
        /// </summary>
        /// <param name="bStopped"></param>
        public void StopWaitForm(bool bStopped = false)
        {
            lock (lockState)
            {
                if (_state == STATE.VISIBLED)
                {
                    m_arSyncManaged[(int)INDEX_SYNCSTATE.CLOSE].Set();

                    _state = STATE.CLOUSING;
                }
                else
                    if (_state == STATE.SHOWING)
                    {
                        _waitCounter--;
                    }
                    else
                        ;

                //Console.WriteLine(@"FormWait::STOP; waitCounter=" + waitCounter);

                if (bStopped == true)
                {
                    bool bClosed = false;
                    if (!(_state == STATE.UNVISIBLED))
                        //Ожидать закрытия окна
                        bClosed = m_arSyncStates[(int)INDEX_SYNCSTATE.CLOSE - 1].WaitOne();
                    else
                        ;
                    
                    // для потока 'SHOWDIALOG' (или наоборот)
                    m_arSyncManaged[(int)INDEX_SYNCSTATE.EXIT].Set();
                    // для потока 'CLOSE' (или наоборот)
                    m_arSyncManaged[(int)INDEX_SYNCSTATE.EXIT].Set();
                    // для потока 'STATE' (или наоборот)
                    m_arSyncManaged[(int)INDEX_SYNCSTATE.EXIT].Set();
                }
                else
                    ;
            }
        }

        private void showDialog()
        {
            Location = _location;
            ShowDialog();
            Focus ();
        }
        /// <summary>
        /// Делегат для вызова метода закрытия окна
        /// </summary>
        private void close()
        {
            Close ();
        }
        /// <summary>
        /// Установить позицию окна
        ///  в зависимости от позиции родительского
        /// </summary>
        /// <param name="ptLocationParent">Позиция отображения родительского окна</param>
        /// <param name="szParent">Размер родительского окна</param>
        private void setLocation(Point ptLocationParent, Size szParent)
        {
            _location = new Point(ptLocationParent.X + (szParent.Width - this.Width) / 2, ptLocationParent.Y + (szParent.Height - this.Height) / 2);
            //_parent = parent;
            //_location = new Point(_parent.Location.X + (_parent.Size.Width - this.Width) / 2, _parent.Location.Y + (_parent.Size.Height - this.Height) / 2);
        }        
        /// <summary>
        /// Обработчик события - 
        /// </summary>
        /// <param name="sender">Объект, инициоровавший событие - this</param>
        /// <param name="e">Аргумент события</param>
        private void FormWait_Shown(object sender, EventArgs e)
        {
            m_arSyncStates[(int)INDEX_SYNCSTATE.SHOWDIALOG - 1].Set();
        }
        /// <summary>
        /// Обработчик события - 
        /// </summary>
        /// <param name="sender">Объект, инициоровавший событие - this</param>
        /// <param name="e">Аргумент события</param>
        private void FormWait_FormClosed(object sender, FormClosedEventArgs e)
        {
            m_arSyncStates[(int)INDEX_SYNCSTATE.CLOSE - 1].Set();
        }
        /// <summary>
        /// Потоковая функция отображения окна
        /// </summary>
        /// <param name="data">Аргумент при запуске потока</param>
        private void fThreadProcShowDialog(object data)
        {
            INDEX_SYNCSTATE indx = INDEX_SYNCSTATE.UNKNOWN;

            while (!(indx == INDEX_SYNCSTATE.EXIT))
            {
                //Ожидать разрешения на выполнение операции
                indx = (INDEX_SYNCSTATE)WaitHandle.WaitAny(new AutoResetEvent [] { m_arSyncManaged[(int)INDEX_SYNCSTATE.EXIT]
                                                                                    , m_arSyncManaged[(int)INDEX_SYNCSTATE.SHOWDIALOG] });
                //Зафиксировать событие
                Logging.Logg().Debug(@"FormWait::fThreadProcShowDialog () - indx=" + ((INDEX_SYNCSTATE)indx).ToString() + @"...", Logging.INDEX_MESSAGE.NOT_SET);
                //Console.WriteLine(@"FormMainBase::fThreadProcShowDialog () - indx=" + indx.ToString() + @" - ...");

                switch (indx)
                {
                    case INDEX_SYNCSTATE.EXIT: // завершение потоковой функции
                        break;
                    case INDEX_SYNCSTATE.SHOWDIALOG: // отобразить окно
                        showDialog();
                        break;
                    default:
                        break;
                }
            }

            //Logging.Logg().Debug(@"FormMainBase::fThreadProcShowDialog () - indx=" + indx.ToString() + @" - ...", Logging.INDEX_MESSAGE.NOT_SET);
        }
        /// <summary>
        /// Потоковая функция снятия с отображения оркна
        /// </summary>
        /// <param name="data">Аргумент при запуске потока</param>
        private void fThreadProcClose(object data)
        {
            INDEX_SYNCSTATE indx = INDEX_SYNCSTATE.UNKNOWN;

            while (!(indx == INDEX_SYNCSTATE.EXIT))
            {
                indx = (INDEX_SYNCSTATE)WaitHandle.WaitAny(new AutoResetEvent[] { m_arSyncManaged[(int)INDEX_SYNCSTATE.EXIT]
                                                                                , m_arSyncManaged[(int)INDEX_SYNCSTATE.CLOSE] });
                indx = indx == INDEX_SYNCSTATE.EXIT ? INDEX_SYNCSTATE.EXIT : indx + 1;
                //Зафиксировать событие
                Logging.Logg().Debug(@"FormWait::fThreadProcClose () - indx=" + ((INDEX_SYNCSTATE)indx).ToString() + @"...", Logging.INDEX_MESSAGE.NOT_SET);
                //Console.WriteLine(@"FormMainBase::fThreadProcClose () - indx=" + indx.ToString() + @" - ...");

                switch (indx)
                {
                    case INDEX_SYNCSTATE.EXIT: // завершение потоковой функции
                        break;
                    case INDEX_SYNCSTATE.CLOSE: // снять с отображения окно
                        if (InvokeRequired == true)
                            BeginInvoke(delegateFuncClose);
                        else
                            close();
                        break;
                    default:
                        break;
                }
            }

            //Logging.Logg().Debug(@"FormMainBase::fThreadProcClose () - indx=" + indx.ToString() + @" - ...", Logging.INDEX_MESSAGE.NOT_SET);
        }

        private void fThreadProcState(object data)
        {
            INDEX_SYNCSTATE indx = INDEX_SYNCSTATE.UNKNOWN;

            while (!(indx == INDEX_SYNCSTATE.EXIT))
            {
                indx = (INDEX_SYNCSTATE)WaitHandle.WaitAny(new WaitHandle[] { m_arSyncManaged[(int)INDEX_SYNCSTATE.EXIT]
                                                                                , m_arSyncStates[(int)INDEX_SYNCSTATE.SHOWDIALOG - 1]
                                                                                , m_arSyncStates[(int)INDEX_SYNCSTATE.CLOSE - 1]});

                switch (indx)
                {
                    case INDEX_SYNCSTATE.EXIT: // завершение потоковой функции
                        break;
                    case INDEX_SYNCSTATE.SHOWDIALOG: // отобразить окно
                        lock (lockState)
                        {
                            _state = STATE.VISIBLED;

                            _waitCounter = 1;
                        }
                        break;
                    case INDEX_SYNCSTATE.CLOSE: // снять с отображения окно
                        lock (lockState)
                        {
                            _state = STATE.UNVISIBLED;

                            _waitCounter = 0;
                        }
                        break;
                    default:
                        break;
                }
            }
        }
    }
}