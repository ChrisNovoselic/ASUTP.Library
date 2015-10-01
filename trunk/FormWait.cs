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
        private enum STATE { UNKNOWN = -1, UNVISIBLED, SHOWING, VISIBLED, CLOUSING }

        private STATE _state;
        /// <summary>
        /// ������ ��� ������������ ��������� �������� �������� ������� ���� �� �����������
        /// </summary>
        private object lockState;
        /// <summary>
        /// ������� ������� ���� �� �����������
        /// </summary>
        private int _waitCounter;
        ///// <summary>
        ///// ����/����� ������ ����������� ����
        ///// </summary>
        //private DateTime m_dtStartShow;
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
        /// <summary>
        /// ����� ��������� ������� �� ��������� ��������� ���� - �����������/������ � �����������
        /// </summary>
            , m_threadState
            ;
        private enum INDEX_SYNCSTATE { UNKNOWN = -1, EXIT, SHOWDIALOG, CLOSE, COUNT_INDEX_SYNCSTATE }
        private AutoResetEvent[] m_arSyncManaged;

        private DelegateFunc delegateFuncClose
            , delegateFuncShowDialog;

        /// <summary>
        /// ������� ������� ����
        /// </summary>
        private bool isRepeat { get { return _waitCounter > 0; } }

        private AutoResetEvent [] m_arSyncStates;
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
            lockState = new object ();
            _state = STATE.UNVISIBLED;
            _waitCounter = 0;
            //m_dtStartShow = DateTime.MinValue;
            
            if (m_arSyncStates == null)
            {
                m_arSyncStates = new AutoResetEvent[(int)INDEX_SYNCSTATE.COUNT_INDEX_SYNCSTATE - 1];
                for (int i = (int)INDEX_SYNCSTATE.SHOWDIALOG; i < (int)INDEX_SYNCSTATE.COUNT_INDEX_SYNCSTATE; i++)
                    m_arSyncStates[i - 1] = new AutoResetEvent(false);
            }
            else
                ;

            if (m_arSyncManaged == null)
            {
                m_arSyncManaged = new AutoResetEvent[(int)INDEX_SYNCSTATE.COUNT_INDEX_SYNCSTATE];
                for (int i = 0; i < (int)INDEX_SYNCSTATE.COUNT_INDEX_SYNCSTATE; i++)
                    m_arSyncManaged[i] = new AutoResetEvent(false);
            }
            else
                ;

            m_threadShow = new Thread(new ParameterizedThreadStart(fThreadProcShowDialog));
            m_threadShow.Name = @"FormWait.Thread - SHOWDIALOG";
            m_threadShow.IsBackground = true;
            m_threadShow.Start(null);

            m_threadHide = new Thread(new ParameterizedThreadStart(fThreadProcClose));
            m_threadHide.Name = @"FormWait.Thread - CLOSE";
            m_threadHide.IsBackground = true;
            m_threadHide.Start(null);

            m_threadState = new Thread(new ParameterizedThreadStart(fThreadProcState));
            m_threadState.Name = @"FormWait.Thread - STATE";
            m_threadState.IsBackground = true;
            m_threadState.Start(null);

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

            lock (lockState)
            {
                //Console.WriteLine(@"FormWait::START; waitCounter=" + waitCounter);

                if (_state == STATE.UNVISIBLED)
                {
                    //���������� ���������� ��� �����������
                    setLocation(ptLocationParent, szParent);
                    //��������� �����������
                    m_arSyncManaged[(int)INDEX_SYNCSTATE.SHOWDIALOG].Set();

                    _state = STATE.SHOWING;
                }
                else
                    if (_state == STATE.CLOUSING)
                    {
                        _waitCounter++;
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
            lock (lockState)
            {
                if (_state == STATE.VISIBLED)
                {
                    m_arSyncManaged[(int)INDEX_SYNCSTATE.CLOSE].Set();

                    _state = STATE.CLOUSING;
                }
                else
                    if (_state == STATE.SHOWING)
                    {
                        _waitCounter--;
                    }
                    else
                        ;

                //Console.WriteLine(@"FormWait::STOP; waitCounter=" + waitCounter);

                if (bStopped == true)
                {
                    bool bClosed = false;
                    if (!(_state == STATE.UNVISIBLED))
                        //������� �������� ����
                        bClosed = m_arSyncStates[(int)INDEX_SYNCSTATE.CLOSE - 1].WaitOne();
                    else
                        ;
                    
                    // ��� ������ 'SHOWDIALOG' (��� ��������)
                    m_arSyncManaged[(int)INDEX_SYNCSTATE.EXIT].Set();
                    // ��� ������ 'CLOSE' (��� ��������)
                    m_arSyncManaged[(int)INDEX_SYNCSTATE.EXIT].Set();
                    // ��� ������ 'STATE' (��� ��������)
                    m_arSyncManaged[(int)INDEX_SYNCSTATE.EXIT].Set();
                }
                else
                    ;
            }
        }

        private void showDialog()
        {
            Location = _location;
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
        /// ���������� ������� - 
        /// </summary>
        /// <param name="sender">������, �������������� ������� - this</param>
        /// <param name="e">�������� �������</param>
        private void FormWait_Shown(object sender, EventArgs e)
        {
            m_arSyncStates[(int)INDEX_SYNCSTATE.SHOWDIALOG - 1].Set();
        }
        /// <summary>
        /// ���������� ������� - 
        /// </summary>
        /// <param name="sender">������, �������������� ������� - this</param>
        /// <param name="e">�������� �������</param>
        private void FormWait_FormClosed(object sender, FormClosedEventArgs e)
        {
            m_arSyncStates[(int)INDEX_SYNCSTATE.CLOSE - 1].Set();
        }
        /// <summary>
        /// ��������� ������� ����������� ����
        /// </summary>
        /// <param name="data">�������� ��� ������� ������</param>
        private void fThreadProcShowDialog(object data)
        {
            INDEX_SYNCSTATE indx = INDEX_SYNCSTATE.UNKNOWN;

            while (!(indx == INDEX_SYNCSTATE.EXIT))
            {
                //������� ���������� �� ���������� ��������
                indx = (INDEX_SYNCSTATE)WaitHandle.WaitAny(new AutoResetEvent [] { m_arSyncManaged[(int)INDEX_SYNCSTATE.EXIT]
                                                                                    , m_arSyncManaged[(int)INDEX_SYNCSTATE.SHOWDIALOG] });
                //������������� �������
                Logging.Logg().Debug(@"FormWait::fThreadProcShowDialog () - indx=" + ((INDEX_SYNCSTATE)indx).ToString() + @"...", Logging.INDEX_MESSAGE.NOT_SET);
                //Console.WriteLine(@"FormMainBase::fThreadProcShowDialog () - indx=" + indx.ToString() + @" - ...");

                switch (indx)
                {
                    case INDEX_SYNCSTATE.EXIT: // ���������� ��������� �������
                        break;
                    case INDEX_SYNCSTATE.SHOWDIALOG: // ���������� ����
                        showDialog();
                        break;
                    default:
                        break;
                }
            }

            //Logging.Logg().Debug(@"FormMainBase::fThreadProcShowDialog () - indx=" + indx.ToString() + @" - ...", Logging.INDEX_MESSAGE.NOT_SET);
        }
        /// <summary>
        /// ��������� ������� ������ � ����������� �����
        /// </summary>
        /// <param name="data">�������� ��� ������� ������</param>
        private void fThreadProcClose(object data)
        {
            INDEX_SYNCSTATE indx = INDEX_SYNCSTATE.UNKNOWN;

            while (!(indx == INDEX_SYNCSTATE.EXIT))
            {
                indx = (INDEX_SYNCSTATE)WaitHandle.WaitAny(new AutoResetEvent[] { m_arSyncManaged[(int)INDEX_SYNCSTATE.EXIT]
                                                                                , m_arSyncManaged[(int)INDEX_SYNCSTATE.CLOSE] });
                indx = indx == INDEX_SYNCSTATE.EXIT ? INDEX_SYNCSTATE.EXIT : indx + 1;
                //������������� �������
                Logging.Logg().Debug(@"FormWait::fThreadProcClose () - indx=" + ((INDEX_SYNCSTATE)indx).ToString() + @"...", Logging.INDEX_MESSAGE.NOT_SET);
                //Console.WriteLine(@"FormMainBase::fThreadProcClose () - indx=" + indx.ToString() + @" - ...");

                switch (indx)
                {
                    case INDEX_SYNCSTATE.EXIT: // ���������� ��������� �������
                        break;
                    case INDEX_SYNCSTATE.CLOSE: // ����� � ����������� ����
                        if (InvokeRequired == true)
                            BeginInvoke(delegateFuncClose);
                        else
                            close();
                        break;
                    default:
                        break;
                }
            }

            //Logging.Logg().Debug(@"FormMainBase::fThreadProcClose () - indx=" + indx.ToString() + @" - ...", Logging.INDEX_MESSAGE.NOT_SET);
        }

        private void fThreadProcState(object data)
        {
            INDEX_SYNCSTATE indx = INDEX_SYNCSTATE.UNKNOWN;

            while (!(indx == INDEX_SYNCSTATE.EXIT))
            {
                indx = (INDEX_SYNCSTATE)WaitHandle.WaitAny(new WaitHandle[] { m_arSyncManaged[(int)INDEX_SYNCSTATE.EXIT]
                                                                                , m_arSyncStates[(int)INDEX_SYNCSTATE.SHOWDIALOG - 1]
                                                                                , m_arSyncStates[(int)INDEX_SYNCSTATE.CLOSE - 1]});

                switch (indx)
                {
                    case INDEX_SYNCSTATE.EXIT: // ���������� ��������� �������
                        break;
                    case INDEX_SYNCSTATE.SHOWDIALOG: // ���������� ����
                        lock (lockState)
                        {
                            _state = STATE.VISIBLED;

                            _waitCounter = 1;
                        }
                        break;
                    case INDEX_SYNCSTATE.CLOSE: // ����� � ����������� ����
                        lock (lockState)
                        {
                            _state = STATE.UNVISIBLED;

                            _waitCounter = 0;
                        }
                        break;
                    default:
                        break;
                }
            }
        }
    }
}