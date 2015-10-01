using System;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;

namespace HClassLibrary
{
    /// <summary>
    /// ����� ��� �������� ���� ������������ ����������� ���������� ��������
    /// </summary>
    public partial class FormWait : Form
    {
        /// <summary>
        /// ������ ��� ������������ ��������� �������� �������� ������� ���� �� �����������
        /// </summary>
        private object lockCounter;
        /// <summary>
        /// ������� ������� ���� �� �����������
        /// </summary>
        private int waitCounter;
        /// <summary>
        /// ����/����� ������ ����������� ����
        /// </summary>
        private DateTime m_dtStartShow;
        /// <summary>
        /// ������������ ����� ����������� ���� (�������)
        /// </summary>
        public static int s_secMaxShowing = 6;
        /// <summary>
        /// ����� ��������� ������� �� ��������� ��������� ���� - �����������
        /// </summary>
        private Thread
            m_threadShow
        /// <summary>
        /// ����� ��������� ������� �� ��������� ��������� ���� - ������ � �����������
        /// </summary>
            , m_threadHide
            ;
        private enum INDEX_SYNCSTATE { UNKNOWN = -1, CLOSING, SHOW, HIDE, COUNT_INDEX_SYNCSTATE }
        private AutoResetEvent[] m_arSyncState;

        private DelegateFunc delegateFuncClose
            , delegateFuncShowDialog;

        /// <summary>
        /// ������� ������� ����
        /// </summary>
        private bool isStarted { get { return waitCounter > 0; } }
        /// <summary>
        /// ������ ������������� - ��������� ������������ ������ �� ������� �������� ����������� ����
        /// </summary>
        private Semaphore m_semaShown
            /// <summary>
            /// ������ ������������� - ��������� ������������ ������ �� ������� �������� ����
            /// </summary>
            , m_semaFormClosed        
            ;
        /// <summary>
        /// ������ �� ������ ����
        ///  ��� ���������� �������� ������ � ������ ������ ������� � �������� ����������
        /// </summary>
        private static FormWait _this;
        private Point _location;
        private Form _parent;
        /// <summary>
        /// �������� ������ �� �������� ����
        /// </summary>
        public static FormWait This { get { if (_this == null) _this = new FormWait (); else ; return _this; } }
        /// <summary>
        /// ����������� - �������� (��� ����������)
        /// </summary>
        private FormWait () : base ()
        {
            InitializeComponent();
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            //������������� �������� �������� ���-�� ������� �� ����������� �����
            lockCounter = new object ();
            waitCounter = 0;
            m_dtStartShow = DateTime.MinValue;
            //�������/���������������� ������ ������������� ��������/����������� ����
            m_semaShown = new Semaphore(0, 1);
            //m_semaShown.WaitOne ();
            //�������/���������������� ������ ������������� �������� ���� (��������� - ���� �� ������������)
            m_semaFormClosed = new Semaphore(1, 1);
            ////�������/���������������� ������ ������������� �������� ���� (��������� - ���� �� ������������)
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

            Shown += new EventHandler(FormWait_Shown);
            FormClosed +=new FormClosedEventHandler(FormWait_FormClosed);
        }
        /// <summary>
        /// ������� �� ����������� ����
        /// </summary>
        /// <param name="ptLocationParent">������� ����������� ������������� ����</param>
        /// <param name="szParent">������ ������������� ����</param>
        public void StartWaitForm(Point ptLocationParent, Size szParent)
        {
            //������������� ���� � 'FormWait::StartWaitForm'
            Logging.Logg().Warning(@"FormWait::StartWaitForm () - ����...", Logging.INDEX_MESSAGE.NOT_SET);

            lock (lockCounter)
            {
                ////������������� ���� � 'FormWait::StartWaitForm'
                //Logging.Logg().Debug(@"FormWait::StartWaitForm (waitCounter=" + waitCounter + @") - ���� ...", Logging.INDEX_MESSAGE.NOT_SET);
                //���� ��� ���������� �������� ��������������� ���������� �������� ��������
                // ������������ - ������������ ���������� ����� (�������) ����������� ����
                if (waitCounter > 0)
                    //��������� ������� 1-�� ����������� ����
                    if (!(m_dtStartShow == DateTime.MinValue))
                        //��������� ������� ������ (������ � �����������)
                        if ((DateTime.Now - m_dtStartShow).TotalSeconds > s_secMaxShowing)
                        {
                            Logging.Logg().Warning(@"FormWait::StartWaitForm (waitCounter=" + waitCounter + @") - ����� �������� - ���������� ������������ ������� �������� ...", Logging.INDEX_MESSAGE.NOT_SET);
                            //��������� ����� (������ � �����������)
                            waitCounter = 1; //0
                            //m_arSyncState[(int)INDEX_SYNCSTATE.HIDE].Set();
                        }
                        else
                            ;
                    else
                        ;
                else
                    ;
                //waitCounter++;
                //Console.WriteLine(@"FormWait::START; waitCounter=" + waitCounter);
                //���������� ������ ���� ���
                if (waitCounter == 0) //1
                {
                    //������������� ����/����� ������ ����������� ����
                    m_dtStartShow = DateTime.Now;
                    ////������� ������ � �����������
                    //m_semaHandleDestroyed.WaitOne();
                    //���������� ���������� ��� �����������
                    setLocation(ptLocationParent, szParent);
                    //��������� �����������
                    m_arSyncState[(int)INDEX_SYNCSTATE.SHOW].Set();
                }
                else
                    ;
            }
        }
        /// <summary>
        /// ����� � ����������� ����
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
                            //waitCounter--;
                    }
                    else
                        waitCounter = 1; //0

                    if (waitCounter == 1) // == 0
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
                    // ��� ������ 'SHOW' (��� ��������)
                    m_arSyncState[(int)INDEX_SYNCSTATE.CLOSING].Set();
                    // ��� ������ 'HIDE' (��� ��������)
                    m_arSyncState[(int)INDEX_SYNCSTATE.CLOSING].Set();
                }
                else
                    ;
            }
        }
        /// <summary>
        /// ���������� ����
        /// </summary>
        private void show()
        {
            bool bFormClosed = false;            

            ////������������� ���� � 'FormWait::show'
            //Logging.Logg().Debug(@"FormWait::show () waitCounter=" + waitCounter + @" - ���� ...", Logging.INDEX_MESSAGE.NOT_SET);
            //Console.WriteLine(@"FormWait::show () - ...");

            //������� ������ � �����������
            bFormClosed = m_semaFormClosed.WaitOne(); //s_secMaxShowing

            Location = _location;
            if (bFormClosed == true)
                if (InvokeRequired == true)
                    BeginInvoke(delegateFuncShowDialog);
                else
                    showDialog();
            else
                Logging.Logg().Warning(@"FormWait::show () waitCounter=" + waitCounter + @" - m_semaFormClosed=" + bFormClosed.ToString() + @"...", Logging.INDEX_MESSAGE.NOT_SET); ;

            ////������������� ����� � 'FormWait::show'
            //Logging.Logg().Debug(@"FormWait::show () waitCounter=" + waitCounter + @" - ����� ...", Logging.INDEX_MESSAGE.NOT_SET);
        }
        /// <summary>
        /// ����� � ����������� ����
        /// </summary>
        private void hide()
        {
            bool bShown = false;

            ////������������� ���� � 'FormWait::hide'
            //Logging.Logg().Debug(@"FormWait::hide () waitCounter=" + waitCounter + @" - ���� ...", Logging.INDEX_MESSAGE.NOT_SET);

            //������� �������� ����������� ���� (�� ���� - �����������)
            bShown = m_semaShown.WaitOne(); //s_secMaxShowing

            if (bShown == true)
                if (InvokeRequired == true)
                    BeginInvoke(delegateFuncClose);
                else
                    close ();
            else
                Logging.Logg().Warning(@"FormWait::hide () waitCounter=" + waitCounter + @" - m_semaHandleCreated=" + bShown.ToString() + @"...", Logging.INDEX_MESSAGE.NOT_SET); ;
            
            ////������������� ����� � 'FormWait::hide'
            //Logging.Logg().Debug(@"FormWait::hide () waitCounter=" + waitCounter + @" - ����� ...", Logging.INDEX_MESSAGE.NOT_SET);
        }

        private void showDialog()
        {
            ShowDialog();
            Focus ();
        }
        /// <summary>
        /// ������� ��� ������ ������ �������� ����
        /// </summary>
        private void close()
        {
            Close ();
        }
        /// <summary>
        /// ���������� ������� ����
        ///  � ����������� �� ������� �������������
        /// </summary>
        /// <param name="ptLocationParent">������� ����������� ������������� ����</param>
        /// <param name="szParent">������ ������������� ����</param>
        private void setLocation(Point ptLocationParent, Size szParent)
        {
            _location = new Point(ptLocationParent.X + (szParent.Width - this.Width) / 2, ptLocationParent.Y + (szParent.Height - this.Height) / 2);
            //_parent = parent;
            //_location = new Point(_parent.Location.X + (_parent.Size.Width - this.Width) / 2, _parent.Location.Y + (_parent.Size.Height - this.Height) / 2);
        }        
        /// <summary>
        /// ���������� ������� - ����������� ����������� ����
        /// </summary>
        /// <param name="sender">������, �������������� ������� - this</param>
        /// <param name="e">�������� �������</param>
        private void FormWait_Shown(object sender, EventArgs e)
        {
            lock (lockCounter)
            {
                waitCounter = 1;
            }
            
            m_semaShown.Release(1);
        }
        /// <summary>
        /// ���������� ������� - �������� ����������� ����
        ///  ��������������� ����������� � ������ ����
        /// </summary>
        /// <param name="sender">������, �������������� ������� - this</param>
        /// <param name="e">�������� �������</param>
        private void FormWait_FormClosed(object sender, FormClosedEventArgs e)
        {
            m_semaFormClosed.Release(1);

            lock (lockCounter)
            {
                waitCounter = 0;
            }
        }
        /// <summary>
        /// ��������� ������� ����������� �����
        /// </summary>
        /// <param name="data">�������� ��� ������� ������</param>
        public void ThreadProcShow(object data)
        {
            INDEX_SYNCSTATE indx = INDEX_SYNCSTATE.UNKNOWN;

            while (!(indx == INDEX_SYNCSTATE.CLOSING))
            {
                //������� ���������� �� ���������� ��������
                indx = (INDEX_SYNCSTATE)WaitHandle.WaitAny(new AutoResetEvent [] { m_arSyncState[(int)INDEX_SYNCSTATE.CLOSING], m_arSyncState[(int)INDEX_SYNCSTATE.SHOW] });
                //������������� �������
                Logging.Logg().Debug(@"FormWait::ThreadProcShow (waitCounter=" + waitCounter + @") - indx=" + ((INDEX_SYNCSTATE)indx).ToString() + @"...", Logging.INDEX_MESSAGE.NOT_SET);
                //Console.WriteLine(@"FormMainBase::ThreadProcShow () - indx=" + indx.ToString() + @" - ...");

                switch (indx)
                {
                    case INDEX_SYNCSTATE.CLOSING: // ���������� ��������� �������
                        break;
                    case INDEX_SYNCSTATE.SHOW: // ���������� ����
                        show ();
                        break;
                    default:
                        break;
                }
            }

            //Logging.Logg().Debug(@"FormMainBase::ThreadProcShow () - indx=" + indx.ToString() + @" - ...", Logging.INDEX_MESSAGE.NOT_SET);
        }
        /// <summary>
        /// ��������� ������� ������ � ����������� �����
        /// </summary>
        /// <param name="data">�������� ��� ������� ������</param>
        public void ThreadProcHide(object data)
        {
            INDEX_SYNCSTATE indx = INDEX_SYNCSTATE.UNKNOWN;

            while (!(indx == INDEX_SYNCSTATE.CLOSING))
            {
                indx = (INDEX_SYNCSTATE)WaitHandle.WaitAny(new AutoResetEvent[] { m_arSyncState[(int)INDEX_SYNCSTATE.CLOSING], m_arSyncState[(int)INDEX_SYNCSTATE.HIDE] });
                indx = indx == INDEX_SYNCSTATE.CLOSING ? INDEX_SYNCSTATE.CLOSING : indx + 1;
                //������������� �������
                Logging.Logg().Debug(@"FormWait::ThreadProcHide (waitCounter=" + waitCounter + @") - indx=" + ((INDEX_SYNCSTATE)indx).ToString() + @"...", Logging.INDEX_MESSAGE.NOT_SET);
                //Console.WriteLine(@"FormMainBase::ThreadProcHide () - indx=" + indx.ToString() + @" - ...");

                switch (indx)
                {
                    case INDEX_SYNCSTATE.CLOSING: // ���������� ��������� �������
                        break;
                    case INDEX_SYNCSTATE.HIDE: // ����� � ����������� ����
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