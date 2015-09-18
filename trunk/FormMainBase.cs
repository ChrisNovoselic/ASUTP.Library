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
        private static object lockCounter = new object ();
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

            this.HandleCreated += new EventHandler(FormMainBase_HandleCreated);
            this.FormClosed += new FormClosedEventHandler(FormMainBase_FormClosed);

            delegateStartWait = new DelegateFunc(startWait);
            delegateStopWait = new DelegateFunc(stopWait);
        }
        /// <summary>
        /// ������������� �������������� ���������� �����
        /// </summary>
        private void InitializeComponent()
        {
        }
        /// <summary>
        /// ������������ ��������� ���������� ������
        /// </summary>
        /// <param name="msg"></param>
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

            MessageBox.Show(this, msgThrow, "������ � ������ ���������!", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            if (bThrow == true) Abort(msgThrow); else ;
        }
        /// <summary>
        /// ��������� (����������) ����� 'FormWait'
        /// </summary>
        private void startWait()
        {
            m_formWait.StartWaitForm (this.Location, this.Size);
        }
        /// <summary>
        /// ���������� (������) ����� 'FormWait' 
        /// </summary>
        private void stopWait()
        {
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
                                //������ ������� � �������
                                findMainMenuItemOfText(mi as ToolStripMenuItem, text);
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

                if (! (itemRes == null))
                    break;
                else
                    ;
            }

            return itemRes;
        }
        /// <summary>
        /// ������� ����
        /// </summary>
        /// <param name="bForce">������� ������������ �������� ����</param>
        public virtual void Close (bool bForce) { base.Close (); }
        /// <summary>
        /// ���������� ������� �������� ����������� ����
        ///  ��� �������� ���-�� ������������ ����������� ����
        ///  ��� �������������� ������ ������� ������� �������� ���� 'FormWait'
        /// </summary>
        /// <param name="obj">������, �������������� ������� - this</param>
        /// <param name="ev">�������� �������</param>
        private void FormMainBase_HandleCreated (object obj, EventArgs ev)
        {
            lock (lockCounter)
            {
                //��������� �������
                formCounter ++;
            }
        }
        /// <summary>
        /// ���������� ������� - �������� �����
        ///  ��� �������� ���-�� ������������ ����������� ����
        /// </summary>
        /// <param name="obj">������, �������������� ������� - this</param>
        /// <param name="ev">�������� �������</param>
        private void  FormMainBase_FormClosed (object obj, FormClosedEventArgs ev)
        {
            lock (lockCounter)
            {
                //���������������� �������
                formCounter--;
                //��������� ���-�� ������������ ����������� ����
                if (formCounter == 0)
                    //������ ������� 'FormWait'
                    m_formWait.StopWaitForm (true);
                else
                    ;
            }
        }
    }
}