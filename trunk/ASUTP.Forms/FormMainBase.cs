using ASUTP.Core;
using ASUTP.Database;
using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace ASUTP.Forms {
    //public class HException : Exception
    //{
    //    public HException(string msg) : base(msg) { }
    //}

    /// <summary>
    /// ������� ����� ��� ������� ����� ����������
    ///  , ������������ ���� ������������ � ����������� ��� ���������� � ��
    ///  , ���������� ��������� ��� ��������� ���������� ������
    /// </summary>
    public abstract class FormMainBase : Form, ASUTP.Helper.IFormMainBase
    {
        /// <summary>
        /// �����, ������������ ��������������� ���������� ��������
        /// </summary>
        private FormWait m_formWait;
        /// <summary>
        /// ������ ��� ������ � ����������� ������ � ����������� ���������� � �� (������������)
        /// </summary>
        protected static FIleConnSett s_fileConnSett;
        /// <summary>
        /// ������ ��� ������������� ������� � �������� ���-�� ������������ ����������� ����
        /// </summary>
        private static object lockCounter = new object();
        /// <summary>
        /// ������� ���-�� ����������� ������������ ����
        /// </summary>
        private static int formCounter = 0;
        /// <summary>
        /// ������� ��� ������ �� ����������� ���� 'FormWait'
        /// </summary>        
        protected DelegateFunc delegateStartWait;
        /// <summary>
        /// ������� ��� ������ � ����������� ���� 'FormWait'
        /// </summary>
        protected DelegateFunc delegateStopWait;
        /// <summary>
        /// ������� ��� ��������� ������� �������������� ���������� ������ ��������� ����������� �����
        /// </summary>
        protected DelegateFunc delegateEvent;
        /// <summary>
        /// ������� ��� ��������� ������� - ���������� ���������� (� �����������) ����������� ������������� ������
        /// </summary>        
        protected DelegateIntFunc delegateUpdateActiveGui;
        /// <summary>
        /// ������� ��� ��������� ������� - ������ ����� � ����������� ����������� ������������� ������
        /// </summary>
        protected DelegateFunc delegateHideGraphicsSettings;
        /// <summary>
        /// ������� ��� ��������� ������� - ���������� ����������
        /// </summary>
        protected DelegateFunc delegateParamsApply;
        /// <summary>
        /// ������������� ��������� ��������� ������
        /// </summary>
        public static int s_iMainSourceData = -1;

        /// <summary>
        /// ����������� - �������� (��� ����������)
        /// </summary>
        protected FormMainBase()
        {
            InitializeComponent();

            m_formWait = FormWait.This;

            this.Shown += new EventHandler(FormMainBase_Shown);
            //this.HandleCreated += new EventHandler(FormMainBase_HandleCreated);
            this.HandleDestroyed += new EventHandler(FormMainBase_HandleDestroyed);
            this.FormClosed += new FormClosedEventHandler(FormMainBase_FormClosed);

            delegateStartWait = new DelegateFunc(startWait);
            delegateStopWait = new DelegateFunc(stopWait);
        }

        /// <summary>
        /// ������������� �������������� ���������� �����
        /// </summary>
        private void InitializeComponent()
        {
            //TODO:
        }

        /// <summary>
        /// ������������ ��������� ���������� ������
        /// </summary>
        /// <param name="msg">��������� ��� ���������� (�������� ���������� ������)</param>
        protected void Abort(string msg)
        {
            throw new Exception(msg);
        }

        /// <summary>
        /// ������������ (��� �������������) �������� ����������
        ///  , ���������� ���������
        /// </summary>
        /// <param name="msg">����� ���������</param>
        /// <param name="bThrow">������� ������������� ���������� ����������</param>
        /// <param name="bSupport">������� ����������� ���������� ���������� ����./���������</param>
        protected virtual void Abort(string msg, bool bThrow = false, bool bSupport = true)
        {
            this.Activate();

            string msgThrow = msg + @".";
            if (bSupport == true)
                msgThrow += Environment.NewLine + @"���������� � ��������� ���./��������� �� ���. 4444 ��� �� ���. 289-03-37.";
            else
                ;

            if (bThrow == true)
                Abort(msgThrow);
            else
                MessageBox.Show(this, msgThrow, "������ � ������ ���������!", MessageBoxButtons.OK, MessageBoxIcon.Stop);
        }

        /// <summary>
        /// ��������� (����������) ����� 'FormWait'
        /// </summary>
        private void startWait()
        {
            //Logging.Logg().Debug(@"FormMainBase::startWait (WindowState=" + this.WindowState + @") - ...", Logging.INDEX_MESSAGE.NOT_SET);

            if (!(this.WindowState == FormWindowState.Minimized))
                //m_formWait.StartWaitForm (this)
                m_formWait.StartWaitForm(this.Location, this.Size)
                ;
            else
                ;
        }

        /// <summary>
        /// ���������� (������) ����� 'FormWait' 
        /// </summary>
        private void stopWait()
        {
            //Logging.Logg().Debug(@"FormMainBase::stopWait (WindowState=" + this.WindowState + @") - ...", Logging.INDEX_MESSAGE.NOT_SET);

            m_formWait.StopWaitForm();
        }

        /// <summary>
        /// ����������� ������� ������ �������� ���� � ��������� ������ ����
        /// </summary>
        /// <param name="miParent">����� ���� � ������� �������������� �����</param>
        /// <param name="text"></param>
        /// <returns>��������� - ���� ���� � ������� ��� ������</returns>
        private ToolStripMenuItem findMainMenuItemOfText(ToolStripMenuItem miParent, string text)
        {
            //��������� 
            ToolStripMenuItem itemRes = null;

            if (miParent.Text == text)
                itemRes = miParent;
            else
                //���� �� ���� ��������� ������ ����
                foreach (ToolStripItem mi in miParent.DropDownItems)
                    if (mi is ToolStripMenuItem)
                        if (mi.Text == text)
                        {
                            itemRes = mi as ToolStripMenuItem;
                            break;
                        }
                        else
                            //��������� ������� �������
                            if (((ToolStripMenuItem)mi).DropDownItems.Count > 0)
                            {
                                //������ ������� � �������
                                itemRes = findMainMenuItemOfText(mi as ToolStripMenuItem, text);

                                if (!(itemRes == null))
                                    break;
                                else
                                    ;
                            }
                            else
                                ;
                    else
                        ;

            return itemRes;
        }

        /// <summary>
        /// ����� � ������� ���� �������� � �������
        /// </summary>
        /// <param name="text">����� ������ (���)���� ��� ������</param>
        /// <returns>��������� - ���� ���� � ������� ��� ������</returns>
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

        private void removeMainMenuItem(ToolStripMenuItem findItem)
        {
            ToolStripMenuItem ownerItem = null;

            ownerItem = findItem.OwnerItem as ToolStripMenuItem;

            if (!(ownerItem == null))
            {
                ownerItem.DropDownItems.Remove(findItem);

                if (ownerItem.DropDownItems.Count == 0)
                    removeMainMenuItem(ownerItem);
                else
                    ;
            }
            else
                if (ownerItem == null)
                    MainMenuStrip.Items.Remove(findItem);
                else
                    ;
        }

        /// <summary>
        /// ������� �.�������� ���� ���������� (�� ������)
        /// </summary>
        /// <param name="text">����� (���)������ ����</param>
        /// <returns>������� ���������� �������� (-1 - ������, 0 - ������� �� ������, 1 - ����� ���� ������)</returns>
        public int RemoveMainMenuItemOfText(string text)
        {
            int iRes = 0; //-1 - ������, 0 - ������� �� ������, 1 - ����� ���� ������
            ToolStripMenuItem findItem = null;            

            foreach (ToolStripMenuItem mi in MainMenuStrip.Items)
            {
                findItem = findMainMenuItemOfText(mi, text);

                if (!(findItem == null))
                    break;
                else
                    ;
            }

            if (!(findItem == null))
            {
                removeMainMenuItem(findItem);
            }
            else
                ;

            return iRes;
        }

        /// <summary>
        /// ������� ����
        /// </summary>
        /// <param name="bForce">������� ������������ �������� ����</param>
        public virtual void Close(bool bForce) { base.Close(); }

        /// <summary>
        /// ���������� ������� �������� ����������� ����
        ///  ��� �������� ���-�� ������������ ����������� ����
        ///  ��� �������������� ������ ������� ������� �������� ���� 'FormWait'
        /// </summary>
        /// <param name="obj">������, �������������� ������� - this</param>
        /// <param name="ev">�������� �������</param>
        private void FormMainBase_Shown(object obj, EventArgs ev)
        {
            lock (lockCounter)
            {
                //��������� ������� - ��������������� ���������� ����������� � ������������ ����
                formCounter++;

                //Console.WriteLine(@"FormMainBase::InitializeComponent () - formCounter=" + formCounter);
            }
        }

        //private void FormMainBase_HandleCreated(object obj, EventArgs ev)
        //{
        //    Logging.Logg().Debug(@"FormMainBase::FormMainBase_HandleCreated () ...", Logging.INDEX_MESSAGE.NOT_SET);
        //}

        private void FormMainBase_HandleDestroyed(object obj, EventArgs ev)
        {
            //Logging.Logg().Debug(@"FormMainBase::FormMainBase_HandleDestroyed () - formCounter=" + (formCounter - 1) + @"...", Logging.INDEX_MESSAGE.NOT_SET);
            
            lock (lockCounter)
            {
                //���������������� ������� - ��������������� ���������� ����������� � ������������ ����
                formCounter--;

                //Console.WriteLine(@"FormMainBase::InitializeComponent () - formCounter=" + formCounter);
            }
        }

        /// <summary>
        /// ���������� ������� - �������� �����
        ///  ��� �������� ���-�� ������������ ����������� ����
        /// </summary>
        /// <param name="obj">������, �������������� ������� - this</param>
        /// <param name="ev">�������� �������</param>
        private void FormMainBase_FormClosed(object obj, FormClosedEventArgs ev)
        {
            lock (lockCounter)
            {
                //��������� ���-�� ������������ ����������� ����
                if ((formCounter - 1) == 0)
                    //������ ������� 'FormWait'
                    m_formWait.StopWaitForm(true);
                else
                    ;
            }
        }
    }
}