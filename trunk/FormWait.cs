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
        /// Дата/время начала отображения окна
        /// </summary>
        private DateTime m_dtStartShow;
        /// <summary>
        /// Максимальное время отображения окна (секунды)
        /// </summary>
        protected static int s_secMaxShowing = 6;
        /// <summary>
        /// Поток обработки событий по изменению состоянию окна - отображение
        /// </summary>
        private Thread
            m_threadShow
        /// <summary>
        /// Поток обработки событий по изменению состоянию окна - снятие с отображения
        /// </summary>
            , m_threadHide
            ;
        private enum INDEX_SYNCSTATE { UNKNOWN = -1, CLOSING, SHOW, HIDE, COUNT_INDEX_SYNCSTATE }
        private AutoResetEvent[] m_arSyncState;

        private DelegateFunc delegateFuncClose
            , delegateFuncShowDialog;

        /// <summary>
        /// Признак запуска окна
        /// </summary>
        private bool isStarted { get { return waitCounter > 0; } }
        /// <summary>
        /// Объект синхронизации - блокирует использующие потоки до момента создания дескриптора окна
        /// </summary>
        private Semaphore m_semaHandleCreated
        ///  <summary>
        /// Объект синхронизации - блокирует использующие потоки до момента уничтожения дескриптора окна
        ///  </summary>
            , m_semaHandleDestroyed
        ///// <summary>
        ///// Объект синхронизации - блокирует использующие потоки до момента закрытия окна
        ///// </summary>
            //, m_semaFormClosed        
            ;
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
            m_dtStartShow = DateTime.MinValue;
            //Создать/инициализировать объект синхронизации создания/отображения окна
            m_semaHandleCreated = new Semaphore(1, 1);
            //Задать состояние - окно НЕ отображается
            m_semaHandleCreated.WaitOne ();
            //Создать/инициализировать объект синхронизации закрытия окна (состояние - окно НЕ отображается)
            m_semaHandleDestroyed = new Semaphore(1, 1);
            ////Создать/инициализировать объект синхронизации закрытия окна (состояние - окно НЕ отображается)
            //m_semaFormClosed = new Semaphore(1, 1);
            //Создать/инициализировать объекты синхронизации по изменению состояния окна
            if (m_arSyncState == null)
            {
                m_arSyncState = new AutoResetEvent[(int)INDEX_SYNCSTATE.COUNT_INDEX_SYNCSTATE];
                for (int i = 0; i < (int)INDEX_SYNCSTATE.COUNT_INDEX_SYNCSTATE; i++)
                    m_arSyncState[i] = new AutoResetEvent(false);
            }
            else
                ;

            m_threadShow = new Thread(new ParameterizedThreadStart(ThreadProcShow));
            m_threadShow.Name = @"FormWait.Thread - SHOW";
            m_threadShow.IsBackground = true;
            m_threadShow.Start(null);

            m_threadHide = new Thread(new ParameterizedThreadStart(ThreadProcHide));
            m_threadHide.Name = @"FormWait.Thread - HIDE";
            m_threadHide.IsBackground = true;
            m_threadHide.Start(null);

            delegateFuncShowDialog = new DelegateFunc (showDialog);
            delegateFuncClose = new DelegateFunc (close);

            HandleCreated += new EventHandler(FormWait_HandleCreated);
            HandleDestroyed += new EventHandler(FormWait_HandleDestroyed);
            FormClosing += new System.Windows.Forms.FormClosingEventHandler(WaitForm_FormClosing);
        }
        /// <summary>
        /// Вызвать на отображение окно
        /// </summary>
        /// <param name="ptLocationParent">Позиция отображения родительского окна</param>
        /// <param name="szParent">Размер родительского окна</param>
        public void StartWaitForm(Point ptLocationParent, Size szParent)
        {
            lock (lockCounter)
            {
                ////Зафиксировать вХод в 'FormWait::StartWaitForm'
                //Logging.Logg().Debug(@"FormWait::StartWaitForm (waitCounter=" + waitCounter + @") - вХод ...", Logging.INDEX_MESSAGE.NOT_SET);
                //Блок для исключения ситуации неограниченного увеличения значения счетчика
                // ограничитель - максимальное допустимое время (секунды) отображения окна
                if (waitCounter > 0)
                    //Проверить признак 1-го отображения окна
                    if (!(m_dtStartShow == DateTime.MinValue))
                        //Проверить условие СБРОСА (снятия с отображения)
                        if ((m_dtStartShow - DateTime.Now).TotalSeconds > s_secMaxShowing)
                        {
                            Logging.Logg().Warning(@"FormWait::StartWaitForm (waitCounter=" + waitCounter + @") - СБРОС счетчика - превышение максмального времени ожидания ...", Logging.INDEX_MESSAGE.NOT_SET);
                            //Выполнить СБРОС (снятие с отображения)
                            waitCounter = 0;
                            m_arSyncState[(int)INDEX_SYNCSTATE.HIDE].Set();
                        }
                        else
                            ;
                    else
                        ;
                else
                    ;
                waitCounter++;
                //Console.WriteLine(@"FormWait::START; waitCounter=" + waitCounter);
                //Отображать только один раз
                if (waitCounter == 1)
                {
                    //Зафиксировать дату/время начала отображения окна
                    m_dtStartShow = DateTime.Now;
                    ////Ожидать снятия с отображения
                    //m_semaHandleDestroyed.WaitOne();
                    //Установить координаты для отображения
                    setLocation(ptLocationParent, szParent);
                    //Рпзрешить отображение
                    m_arSyncState[(int)INDEX_SYNCSTATE.SHOW].Set();
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
                //Console.WriteLine(@"FormWait::STOP; waitCounter=" + waitCounter);
                if (bStopped == true)
                {
                    // для потока 'SHOW' (или наоборот)
                    m_arSyncState[(int)INDEX_SYNCSTATE.CLOSING].Set();
                    // для потока 'HIDE' (или наоборот)
                    m_arSyncState[(int)INDEX_SYNCSTATE.CLOSING].Set();
                }
                else
                    ;
            }
        }
        /// <summary>
        /// Отобразить окно
        /// </summary>
        private void show()
        {
            ////Зафиксировать вХод в 'FormWait::show'
            //Logging.Logg().Debug(@"FormWait::show () waitCounter=" + waitCounter + @" - вХод ...", Logging.INDEX_MESSAGE.NOT_SET);
            //Console.WriteLine(@"FormWait::show () - ...");

            ////Ожидать снятия с отображения
            m_semaHandleDestroyed.WaitOne();

            Location = _location;
            if (InvokeRequired == true)
                BeginInvoke(delegateFuncShowDialog);
            else
                showDialog();

            ////Зафиксировать вЫХод в 'FormWait::show'
            //Logging.Logg().Debug(@"FormWait::show () waitCounter=" + waitCounter + @" - вЫХод ...", Logging.INDEX_MESSAGE.NOT_SET);
        }
        /// <summary>
        /// Снять с отображения окно
        /// </summary>
        private void hide()
        {
            ////Зафиксировать вХод в 'FormWait::hide'
            //Logging.Logg().Debug(@"FormWait::hide () waitCounter=" + waitCounter + @" - вХод ...", Logging.INDEX_MESSAGE.NOT_SET);
            
            //Ожидать создания дескриптора окна (по сути - отображения)
            m_semaHandleCreated.WaitOne();

            if (InvokeRequired == true)
                BeginInvoke(delegateFuncClose);
            else
                close ();

            ////Зафиксировать вЫХод в 'FormWait::hide'
            //Logging.Logg().Debug(@"FormWait::hide () waitCounter=" + waitCounter + @" - вЫХод ...", Logging.INDEX_MESSAGE.NOT_SET);
        }

        private void showDialog()
        {
            ShowDialog();
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
        /// Обработчик события - создание дескриптора окна
        ///  гарантированное отображение н экране огна
        /// </summary>
        /// <param name="sender">Объект, инициоровавший событие - this</param>
        /// <param name="e">Аргумент события</param>
        private void FormWait_HandleCreated(object sender, EventArgs e)
        {
            m_semaHandleCreated.Release(1);
        }
        /// <summary>
        /// Обработчик события - уничтожение дескриптора окна
        /// </summary>
        /// <param name="sender">Объект, инициоровавший событие - this</param>
        /// <param name="e">Аргумент события</param>
        private void FormWait_HandleDestroyed(object sender, EventArgs e)
        {
            m_semaHandleDestroyed.Release(1);
        }
        /// <summary>
        /// Обработчик события - перед закрытием окна
        ///  проверяется признак отображения окна 'FormWait'
        /// </summary>
        /// <param name="sender">Объект, инициоровавший событие - this</param>
        /// <param name="e">Аргумент события</param>
        private void WaitForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            ////Отменить закрытие, если установлен признак отображения
            //lock (lockCounter)
            //{
            //    e.Cancel = isStarted;
            //}

            //Console.WriteLine(@"FormWait::WaitForm_FormClosing (отмена=" + e.Cancel.ToString() + @") - ...");
        }
        /// <summary>
        /// Потоковая функция отображения оркна
        /// </summary>
        /// <param name="data">Аргумент при запуске потока</param>
        public void ThreadProcShow(object data)
        {
            INDEX_SYNCSTATE indx = INDEX_SYNCSTATE.UNKNOWN;

            while (!(indx == INDEX_SYNCSTATE.CLOSING))
            {
                //Ожидать разрешения на выполнение операции
                indx = (INDEX_SYNCSTATE)WaitHandle.WaitAny(new AutoResetEvent [] { m_arSyncState[(int)INDEX_SYNCSTATE.CLOSING], m_arSyncState[(int)INDEX_SYNCSTATE.SHOW] });
                //Console.WriteLine(@"FormMainBase::ThreadProcShow () - indx=" + indx.ToString() + @" - ...");

                switch (indx)
                {
                    case INDEX_SYNCSTATE.CLOSING: // завершение потоковой функции
                        break;
                    case INDEX_SYNCSTATE.SHOW: // отобразить окно
                        show ();
                        break;
                    default:
                        break;
                }
            }

            //Logging.Logg().Debug(@"FormMainBase::ThreadProcShow () - indx=" + indx.ToString() + @" - ...", Logging.INDEX_MESSAGE.NOT_SET);
        }
        /// <summary>
        /// Потоковая функция снятия с отображения оркна
        /// </summary>
        /// <param name="data">Аргумент при запуске потока</param>
        public void ThreadProcHide(object data)
        {
            INDEX_SYNCSTATE indx = INDEX_SYNCSTATE.UNKNOWN;

            while (!(indx == INDEX_SYNCSTATE.CLOSING))
            {
                indx = (INDEX_SYNCSTATE)WaitHandle.WaitAny(new AutoResetEvent[] { m_arSyncState[(int)INDEX_SYNCSTATE.CLOSING], m_arSyncState[(int)INDEX_SYNCSTATE.HIDE] });
                indx = indx == INDEX_SYNCSTATE.CLOSING ? INDEX_SYNCSTATE.CLOSING : indx + 1;
                //Console.WriteLine(@"FormMainBase::ThreadProcHide () - indx=" + indx.ToString() + @" - ...");

                switch (indx)
                {
                    case INDEX_SYNCSTATE.CLOSING: // завершение потоковой функции
                        break;
                    case INDEX_SYNCSTATE.HIDE: // снять с отображения окно
                        hide();
                        break;
                    default:
                        break;
                }
            }

            //Logging.Logg().Debug(@"FormMainBase::ThreadProcHide () - indx=" + indx.ToString() + @" - ...", Logging.INDEX_MESSAGE.NOT_SET);
        }
    }
}