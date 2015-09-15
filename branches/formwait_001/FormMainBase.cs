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

        protected bool show_error_alert = false;

        public static int s_iMainSourceData = -1;

        protected FormMainBase()
        {
            InitializeComponent();

            formWait = new FormWait();
            delegateStopWaitForm = new DelegateFunc(formWait.StopWaitForm);

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
            FormWait fw = (FormWait)data;
            fw.StartWaitForm();
        }

        public void StartWait()
        {
            lock (lockValue)
            {
                if (waitCounter == 0)
                {
                    //this.Opacity = 0.75;
                    if ((! (m_threadFormWait == null))
                        && (m_threadFormWait.IsAlive == true))
                        m_threadFormWait.Join();
                    else
                        ;

                    formWait.m_semaFormClosed.WaitOne ();

                    m_threadFormWait = null;
                    m_threadFormWait = new Thread(new ParameterizedThreadStart(ThreadProc));
                    formWait.Location = new Point(this.Location.X + (this.Width - formWait.Width) / 2, this.Location.Y + (this.Height - formWait.Height) / 2);
                    m_threadFormWait.IsBackground = true;
                    m_threadFormWait.Start(formWait);
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
                    formWait.m_semaHandleCreated.WaitOne ();

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

                if (! (itemRes == null))
                    break;
                else
                    ;
            }

            return itemRes;
        }

        public virtual void Close (bool bForce) { base.Close (); }
    }
}