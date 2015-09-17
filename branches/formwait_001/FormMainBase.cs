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
        private FormWait m_formWait;
        protected static FIleConnSett s_fileConnSett;

        private static object lockCounter = new object ();
        private static int formCounter = 0;

        protected DelegateFunc delegateStartWait;
        protected DelegateFunc delegateStopWait;
        //private DelegateFunc delegateStopWaitForm;
        protected DelegateFunc delegateEvent;
        protected DelegateIntFunc delegateUpdateActiveGui;
        protected DelegateFunc delegateHideGraphicsSettings;
        protected DelegateFunc delegateParamsApply;

        protected bool show_error_alert = false;

        public static int s_iMainSourceData = -1;

        protected FormMainBase()
        {
            InitializeComponent();

            lock (lockCounter)
            {
                formCounter ++;
            }

            m_formWait = FormWait.This;

            this.FormClosing += new FormClosingEventHandler(FormMainBase_FormClosing);

            delegateStartWait = new DelegateFunc(startWait);
            delegateStopWait = new DelegateFunc(stopWait);
        }

        private void InitializeComponent()
        {
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

        private void startWait()
        {
            m_formWait.StartWaitForm (this.Location, this.Size);
        }

        private void stopWait()
        {
            m_formWait.StopWaitForm();
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
            lock (lockCounter)
            {
                formCounter--;

                if (formCounter == 0)
                    m_formWait.StopWaitForm (true);
                else
                    ;
            }
        }
    }
}