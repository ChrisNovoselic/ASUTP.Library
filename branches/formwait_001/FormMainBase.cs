using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace HClassLibrary
{
    //public class HException : Exception
    //{
    //    public HException(string msg) : base(msg) { }
    //}
    
    public abstract class FormMainBase : Form
    {
        protected FormWait formWait;
        protected static FIleConnSett s_fileConnSett;

        protected object lockEvent;
        private object lockValue;
        private int waitCounter;

        private Thread m_threadFormWait;

        protected DelegateFunc delegateStartWait;
        protected DelegateFunc delegateStopWait;
        private DelegateFunc delegateStopWaitForm;
        protected DelegateFunc delegateEvent;
        protected DelegateIntFunc delegateUpdateActiveGui;
        protected DelegateFunc delegateHideGraphicsSettings;
        protected DelegateFunc delegateParamsApply;

        private enum INDEX_SYNCWAIT { UNKNOWN = -1, CLOSING, START, STOP, COUNT_INDEX_SYNCWAIT }
        private AutoResetEvent [] m_arSyncWait;

        protected bool show_error_alert = false;

        public static int s_iMainSourceData = -1;

        protected FormMainBase()
        {
            InitializeComponent();

            this.FormClosing += new FormClosingEventHandler(FormMainBase_FormClosing);

            formWait = new FormWait();
            delegateStopWaitForm = new DelegateFunc(formWait.StopWaitForm);

            delegateStartWait = new DelegateFunc(StartWait);
            delegateStopWait = new DelegateFunc(StopWait);

            m_arSyncWait = new AutoResetEvent [(int)INDEX_SYNCWAIT.COUNT_INDEX_SYNCWAIT];
            for (int i = 0; i < (int)INDEX_SYNCWAIT.COUNT_INDEX_SYNCWAIT; i ++)
                m_arSyncWait[i] = new AutoResetEvent (false);

            m_threadFormWait = new Thread(new ParameterizedThreadStart(ThreadProc));
            m_threadFormWait.IsBackground = true;
            m_threadFormWait.Start(formWait);
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
                msgThrow += Environment.NewLine + @"���������� � ��������� ���./��������� �� ���. 4444 ��� �� ���. 289-03-37.";
            else
                ;

            MessageBox.Show(this, msgThrow, "������ � ������ ���������!", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            if (bThrow == true) Abort(msgThrow); else ;
        }

        public void ThreadProc(object data)
        {
            INDEX_SYNCWAIT indx = INDEX_SYNCWAIT.UNKNOWN;
            FormWait fw = data as FormWait;

            while (! (indx == INDEX_SYNCWAIT.CLOSING))
            {
                indx = (INDEX_SYNCWAIT)WaitHandle.WaitAny(m_arSyncWait);
                Console.WriteLine(@"FormMainBase::ThreadProc () - indx=" + indx.ToString () + @" - ...");

                switch (indx)
                {
                    case INDEX_SYNCWAIT.CLOSING:
                        break;
                    case INDEX_SYNCWAIT.START:
                        fw.Location = new Point(this.Location.X + (this.Width - formWait.Width) / 2, this.Location.Y + (this.Height - formWait.Height) / 2);
                        //fw.StartWaitForm ();
                        BeginInvoke (new DelegateFunc (fw.StartWaitForm));
                        break;
                    case INDEX_SYNCWAIT.STOP:
                        //fw.StopWaitForm();
                        BeginInvoke(new DelegateFunc(fw.StopWaitForm));
                        break;
                    default:
                        break;
                }
            }
        }

        public void StartWait()
        {
            lock (lockValue)
            {
                if (waitCounter == 0)
                {
                    formWait.m_semaFormClosed.WaitOne ();

                    m_arSyncWait[(int)INDEX_SYNCWAIT.START].Set();

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
                    //formWait.m_semaHandleCreated.WaitOne ();

                    m_arSyncWait[(int)INDEX_SYNCWAIT.STOP].Set ();
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

                if (! (itemRes == null))
                    break;
                else
                    ;
            }

            return itemRes;
        }

        public virtual void Close (bool bForce) { base.Close (); }

        private void  FormMainBase_FormClosing (object obj, FormClosingEventArgs ev)
        {
            m_arSyncWait[(int)INDEX_SYNCWAIT.CLOSING].Set ();
        }
    }
}