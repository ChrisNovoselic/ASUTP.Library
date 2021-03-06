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

        private DelegateFunc delegateFuncClose;

        /// <summary>
        /// ������� ������� ����
        /// </summary>
        private bool isStarted { get { return waitCounter > 0; } }
        /// <summary>
        /// ������ ������������� - ��������� ������������ ������ �� ������� �������� ����������� ����
        /// </summary>
        private Semaphore m_semaHandleCreated
        ///  <summary>
        /// ������ ������������� - ��������� ������������ ������ �� ������� ����������� ����������� ����
        ///  </summary>
            , m_semaHandleDestroyed
        ///// <summary>
        ///// ������ ������������� - ��������� ������������ ������ �� ������� �������� ����
        ///// </summary>
            //, m_semaFormClosed        
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
            //������������� �������� �������� ���-�� ������� �� ����������� �����
            lockCounter = new object ();
            waitCounter = 0;
            //�������/���������������� ������ ������������� ��������/����������� ����
            m_semaHandleCreated = new Semaphore(1, 1);
            //������ ��������� - ���� �� ������������
            m_semaHandleCreated.WaitOne ();
            //�������/���������������� ������ ������������� �������� ���� (��������� - ���� �� ������������)
            m_semaHandleDestroyed = new Semaphore(1, 1);
            ////�������/���������������� ������ ������������� �������� ���� (��������� - ���� �� ������������)
            //m_semaFormClosed = new Semaphore(1, 1);
            //�������/���������������� ������� ������������� �� ��������� ��������� ����
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

            delegateFuncClose = new DelegateFunc (close);

            HandleCreated += new EventHandler(FormWait_HandleCreated);
            HandleDestroyed += new EventHandler(FormWait_HandleDestroyed);
            FormClosing += new System.Windows.Forms.FormClosingEventHandler(WaitForm_FormClosing);
        }
        /// <summary>
        /// ������� �� ����������� ����
        /// </summary>
        /// <param name="ptLocationParent">������� ����������� ������������� ����</param>
        /// <param name="szParent">������ ������������� ����</param>
        public void StartWaitForm(Point ptLocationParent, Size szParent)
        {
            lock (lockCounter)
            {
                waitCounter++;
                Console.WriteLine(@"FormWait::START; waitCounter=" + waitCounter);
                //���������� ������ ���� ���
                if (waitCounter == 1)
                {
                    //������� ������ � �����������
                    m_semaHandleDestroyed.WaitOne();
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
            //Console.WriteLine(@"FormWait::show () - ...");
            Location = _location;
            ShowDialog();
        }
        /// <summary>
        /// ����� � ����������� ����
        /// </summary>
        private void hide()
        {
            m_semaHandleCreated.WaitOne();
            if (InvokeRequired == true)
                BeginInvoke (delegateFuncClose);
            else
                close ();
            //Console.WriteLine(@"FormWait::hide () - ...");
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
        }
        /// <summary>
        /// ���������� ������� - �������� ����������� ����
        ///  ��������������� ����������� � ������ ����
        /// </summary>
        /// <param name="sender">������, �������������� ������� - this</param>
        /// <param name="e">�������� �������</param>
        private void FormWait_HandleCreated(object sender, EventArgs e)
        {
            m_semaHandleCreated.Release(1);
        }
        /// <summary>
        /// ���������� ������� - ����������� ����������� ����
        /// </summary>
        /// <param name="sender">������, �������������� ������� - this</param>
        /// <param name="e">�������� �������</param>
        private void FormWait_HandleDestroyed(object sender, EventArgs e)
        {
            m_semaHandleDestroyed.Release(1);
        }

        private void WaitForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            ////�������� ��������, ���� ���������� ������� �����������
            //lock (lockCounter)
            //{
            //    e.Cancel = isStarted;
            //}

            Console.WriteLine(@"FormWait::WaitForm_FormClosing (������=" + e.Cancel.ToString() + @") - ...");
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
                Console.WriteLine(@"FormMainBase::ThreadProcShow () - indx=" + indx.ToString() + @" - ...");

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
                Console.WriteLine(@"FormMainBase::ThreadProcHide () - indx=" + indx.ToString() + @" - ...");

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
        }
    }
}