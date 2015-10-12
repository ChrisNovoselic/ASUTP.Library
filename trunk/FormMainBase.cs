using System;
using System.Drawing;
using System.Threading;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace HClassLibrary
{
    //public class HException : Exception
    //{
    //    public HException(string msg) : base(msg) { }
    //}

    public abstract class FormMainBase : Form
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr handle);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenThread(uint desiredAccess, bool inheritHandle, uint threadId);
        
        protected FormWait formWait;
        protected static FIleConnSett s_fileConnSett;

        protected object lockEvent;
        private object lockValue;
        private int waitCounter;

        private Point _ptLocation;
        //private Thread m_threadFormWait;        
        private IntPtr m_handleFormWait;
        private ParameterizedThreadStart _threadStart;
        //private BackgroundWorker m_threadBWorkerFormWait;

        protected DelegateFunc delegateStartWait;
        protected DelegateFunc delegateStopWait;
        protected DelegateFunc delegateStopWaitForm;
        protected DelegateFunc delegateEvent;
        protected DelegateIntFunc delegateUpdateActiveGui;
        protected DelegateFunc delegateHideGraphicsSettings;
        protected DelegateFunc delegateParamsApply;

        protected bool show_error_alert = false;

        public static int s_iMainSourceData = -1;

        protected FormMainBase()
        {
            Logging.Logg().Debug(@"FormMainBase::ctor () - ...", Logging.INDEX_MESSAGE.NOT_SET);

            InitializeComponent();

            formWait = new FormWait();
            delegateStopWaitForm = new DelegateFunc(formWait.StopWaitForm);            
            _ptLocation = new Point (-1, -1);

            m_handleFormWait = IntPtr.Zero;
            _threadStart = new ParameterizedThreadStart(ThreadProc);
            //m_threadBWorkerFormWait = new BackgroundWorker();
            //m_threadBWorkerFormWait.WorkerReportsProgress = false;
            //m_threadBWorkerFormWait.WorkerSupportsCancellation = true;
            //m_threadBWorkerFormWait.DoWork += new DoWorkEventHandler(ThreadBWorkerProc);
            //m_threadBWorkerFormWait.RunWorkerCompleted += new RunWorkerCompletedEventHandler(ThreadBWorker_RunCompleted);

            delegateStartWait = new DelegateFunc(StartWait);
            delegateStopWait = new DelegateFunc(StopWait);
        }

        private void InitializeComponent()
        {
            lockEvent = new object();

            lockValue = new object();
            waitCounter = 0;
        }

        protected void Abort(string msg)
        {
            throw new Exception(msg);
        }

        protected virtual void Abort(string msg, bool bThrow = false, bool bSupport = true)
        {
            this.Activate();

            string msgThrow = msg + @".";
            if (bSupport == true)
                msgThrow += Environment.NewLine + @"Обратитесь к оператору тех./поддержки по тел. 4444 или по тел. 289-03-37.";
            else
                ;

            MessageBox.Show(this, msgThrow, "Ошибка в работе программы!", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            if (bThrow == true) Abort(msgThrow); else ;
        }

        public static void ThreadProc(object data)
        {
            FormWait fw =
                (FormWait)data
                //(FormWait)ev.Argument
                ;
            fw.StartWaitForm();
        }

        //public void ThreadBWorkerProc(object data, DoWorkEventArgs ev)
        //{
        //    FormWait fw = (FormWait)ev.Argument;
        //    fw.StartWaitForm();
        //}

        //public void ThreadBWorker_RunCompleted(object data, RunWorkerCompletedEventArgs ev)
        //{
        //    formWait.m_semaStop.Release(1);
        //}
        [MTAThread]
        public void StartWait()
        {
            Logging.Logg().Debug(@"FormMainBase::StartWait () - ...", Logging.INDEX_MESSAGE.NOT_SET);
            
            lock (lockValue)
            {
                if (waitCounter == 0)
                {
                    formWait.m_arSemaStop[1].WaitOne();
                    //formWait.m_arSemaStop[0].WaitOne();

                    //this.Opacity = 0.75;
                    //Вариант №0
                    if (!(m_handleFormWait == IntPtr.Zero))
                        CloseHandle(m_handleFormWait);
                    else
                        ;
                    //Вариант №1
                    //if ((!(m_threadFormWait == null))
                    //    && (m_threadFormWait.IsAlive == true))
                    //{
                    //    m_threadFormWait.Join();
                    //    CloseHandle(OpenThread(0x40000000, false, (uint)m_threadFormWait.ManagedThreadId));
                    //    m_threadFormWait = null;
                    //}
                    //else
                    //    ;
                    ////Вариант №2
                    //if (!(m_threadBWorkerFormWait == null))
                    //{
                    //    if (m_threadBWorkerFormWait.IsBusy == true)
                    //        m_threadBWorkerFormWait.CancelAsync();
                    //    else
                    //        ;
                    //    while (m_threadBWorkerFormWait.IsBusy == true) ;
                    //    m_threadBWorkerFormWait = null;
                    //}
                    //else
                    //    ;

                    //formWait.Location = new Point(this.Location.X + (this.Width - formWait.Width) / 2, this.Location.Y + (this.Height - formWait.Height) / 2);
                    _ptLocation.X = this.Location.X + (this.Width - formWait.Width) / 2;
                    _ptLocation.Y = this.Location.Y + (this.Height - formWait.Height) / 2;
                    formWait.Location = _ptLocation;
                    //Вариант №1
                    Thread threadFormWait = new Thread(_threadStart) { IsBackground = true };
                    threadFormWait.Start(formWait);
                    m_handleFormWait = OpenThread(0x40000000, false, (uint)threadFormWait.ManagedThreadId);
                    ////Вариант №2
                    //m_threadBWorkerFormWait.RunWorkerAsync(formWait);
                }
                else
                    ;

                waitCounter++;
            }
        }

        public void StopWait()
        {
            lock (lockValue)
            {
                waitCounter--;
                if (waitCounter < 0)
                    waitCounter = 0;

                if (waitCounter == 0)
                {
                    //Прозрачность
                    //this.Opacity = 1.0;
                    //Ожидать закрытия десккриптора окна
                    ////Вариант №1
                    //while (formWait.IsHandleCreated == false)
                    //    ;
                    //Вариант №2
                    formWait.m_semaHandleCreated.WaitOne();

                    formWait.Invoke(delegateStopWaitForm);
                }
                else
                    ;
            }
        }

        private ToolStripMenuItem findMainMenuItemOfText(ToolStripMenuItem miParent, string text)
        {
            ToolStripMenuItem itemRes = null;

            if (miParent.Text == text)
                itemRes = miParent;
            else
                foreach (ToolStripItem mi in miParent.DropDownItems)
                    if (mi is ToolStripMenuItem)
                        if (mi.Text == text)
                        {
                            itemRes = mi as ToolStripMenuItem;
                            break;
                        }
                        else
                            if (((ToolStripMenuItem)mi).DropDownItems.Count > 0)
                                findMainMenuItemOfText(mi as ToolStripMenuItem, text);
                            else
                                ;
                    else
                        ;

            return itemRes;
        }

        public ToolStripMenuItem FindMainMenuItemOfText(string text)
        {
            ToolStripMenuItem itemRes = null;

            foreach (ToolStripMenuItem mi in MainMenuStrip.Items)
            {
                itemRes = findMainMenuItemOfText(mi, text);

                if (!(itemRes == null))
                    break;
                else
                    ;
            }

            return itemRes;
        }

        public virtual void Close(bool bForce) { base.Close(); }
    }
}