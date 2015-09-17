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
        /// <summary>
        /// Объект для блокирования изменения значения счетчика вызовов окна на отображение
        /// </summary>
        private object lockCounter;
        /// <summary>
        /// Счетчик вызовов окна на отображение
        /// </summary>
        private int waitCounter;
        /// <summary>
        /// Поток обработки событий по изменению состоянию окна
        /// </summary>
        private Thread m_threadState;
        private enum INDEX_SYNCSTATE { UNKNOWN = -1, CLOSING, SHOW, HIDE, COUNT_INDEX_SYNCSTATE }
        private AutoResetEvent[] m_arSyncState;
        
        private Thread m_threadWait;

        private DelegateFunc delegateFuncClose;

        /// <summary>
        /// Признак запуска окна
        /// </summary>
        private bool isStarted { get { return waitCounter > 0; } }
        /// <summary>
        /// Объект синхронизации - блокирует использующие потоки до момента создания дескриптора окна
        /// </summary>
        private Semaphore m_semaHandleCreated
        /// <summary>
        /// Объект синхронизации - блокирует использующие потоки до момента закрытия окна
        /// </summary>
            , m_semaFormClosed;
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
            //Инициализация объектов подсчета кол-ва вызовов на отображение формы
            lockCounter = new object ();
            waitCounter = 0;
            //Создать/инициализировать объект синхронизации создания/отображения окна
            m_semaHandleCreated = new Semaphore(1, 1);
            //Задать состояние - окно НЕ отображается
            m_semaHandleCreated.WaitOne ();
            //Создать/инициализировать объект синхронизации закрытия окна (состояние - окно НЕ отображается)
            m_semaFormClosed = new Semaphore(1, 1);
            //Создать/инициализировать объекты синхронизации по изменению состояния окна
            if (m_arSyncState == null)
            {
                m_arSyncState = new AutoResetEvent[(int)INDEX_SYNCSTATE.COUNT_INDEX_SYNCSTATE];
                for (int i = 0; i < (int)INDEX_SYNCSTATE.COUNT_INDEX_SYNCSTATE; i++)
                    m_arSyncState[i] = new AutoResetEvent(false);
            }
            else
                ;
            
            if (m_threadState == null)
            {
                m_threadState = new Thread(new ParameterizedThreadStart(ThreadProc));
                m_threadState.IsBackground = true;
                m_threadState.Start(null);
            }
            else
                ;

            delegateFuncClose = new DelegateFunc (close);

            HandleCreated += new EventHandler(FormWait_HandleCreated);
            FormClosing += new System.Windows.Forms.FormClosingEventHandler(WaitForm_FormClosing);
            FormClosed += new System.Windows.Forms.FormClosedEventHandler(WaitForm_FormClosed);
        }

        private void startThreadWait ()
        {
            if (!(m_threadWait == null))
            {
                m_threadWait.Join (66);
                
                if (m_threadWait.IsAlive == true)
                {
                    Console.WriteLine (@"FormWait::ABORT threadWait ...");
                    
                    m_threadWait.Abort ();
                }
                else
                    ;
                m_threadWait = null;
            }
            else
                ;

            m_threadWait = new Thread(new ParameterizedThreadStart(show));
            m_threadWait.IsBackground = true;
            m_threadWait.Start ();
        }

        public void StartWaitForm(Point ptLocationParent, Size szParent)
        {
            lock (lockCounter)
            {
                waitCounter++;
                Console.WriteLine(@"FormWait::START; waitCounter=" + waitCounter);

                if (waitCounter == 1)
                {
                    m_semaFormClosed.WaitOne();
                    setLocation(ptLocationParent, szParent);
                    m_arSyncState[(int)INDEX_SYNCSTATE.SHOW].Set();
                }
                else
                    ;
            }
        }

        public void StopWaitForm(bool bStopped = false)
        {
            lock (lockCounter)
            {
                if (waitCounter > 0)
                {
                    if (bStopped == false)
                    {
                            waitCounter--;
                    }
                    else
                        waitCounter = 0;

                    if (waitCounter == 0)
                    {
                        m_arSyncState [(int)INDEX_SYNCSTATE.HIDE].Set();
                    }
                    else
                        ;
                }
                else
                    ;
                Console.WriteLine(@"FormWait::STOP; waitCounter=" + waitCounter);
                if (bStopped == true)
                    m_arSyncState[(int)INDEX_SYNCSTATE.CLOSING].Set();
                else
                    ;
            }
        }

        private void show(object obj)
        {
            //Console.WriteLine(@"FormWait::show () - ...");
            Location = _location;
            ShowDialog ();            
        }

        private void hide()
        {
            m_semaHandleCreated.WaitOne();
            BeginInvoke (delegateFuncClose);
            //Console.WriteLine(@"FormWait::hide () - ...");
        }

        private void close()
        {
            Close ();
        }

        private void setLocation(Point ptLocationParent, Size szParent)
        {
            _location = new Point(ptLocationParent.X + (szParent.Width - this.Width) / 2, ptLocationParent.Y + (szParent.Height - this.Height) / 2);
        }

        private void FormWait_HandleCreated(object sender, EventArgs e)
        {
            m_semaHandleCreated.Release(1);
        }

        private void WaitForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            ////Отменить закрытие, если установлен признак отображения
            //lock (lockCounter)
            //{
            //    e.Cancel = isStarted;
            //}

            Console.WriteLine(@"FormWait::WaitForm_FormClosing (отмена=" + e.Cancel.ToString() + @") - ...");
        }

        private void WaitForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            //m_threadWait.Join ();
            m_semaFormClosed.Release(1);
        }

        public void ThreadProc(object data)
        {
            INDEX_SYNCSTATE indx = INDEX_SYNCSTATE.UNKNOWN;
            //FormWait fw = data as FormWait;

            while (!(indx == INDEX_SYNCSTATE.CLOSING))
            {
                Console.WriteLine(@"FormMainBase::ThreadProc () - indx=" + indx.ToString() + @" - ...");
                indx = (INDEX_SYNCSTATE)WaitHandle.WaitAny(m_arSyncState);

                switch (indx)
                {
                    case INDEX_SYNCSTATE.CLOSING:
                        break;
                    case INDEX_SYNCSTATE.SHOW:                        
                        startThreadWait ();
                        break;
                    case INDEX_SYNCSTATE.HIDE:
                        hide ();
                        break;
                    default:
                        break;
                }
            }
        }
    }
}