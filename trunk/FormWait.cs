using System;
using System.Threading;
using System.ComponentModel; //BackgroundWorker
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
        private BackgroundWorker //Thread
            m_threadShowDialog
        ///// <summary>
        ///// ����� ��������� ������� �� ��������� ��������� ���� - ������ � �����������
        ///// </summary>
        //    , m_threadHide
        ///// <summary>
        ///// ����� ��������� ������� �� ��������� ��������� ���� - �����������/������ � �����������
        ///// </summary>
        //    , m_threadState
            ;
        private enum INDEX_SYNCSTATE { UNKNOWN = -1, EXIT, SHOWDIALOG, CLOSE, COUNT_INDEX_SYNCSTATE }
        private AutoResetEvent[] m_arSyncManaged;

        private DelegateFunc delegateFuncClose
            , delegateFuncShowDialog;

        /// <summary>
        /// ������� ������� ����
        /// </summary>
        private bool isContinue { get { return _waitCounter > 0; } }

        private AutoResetEvent [] m_arSyncStates;
        /// <summary>
        /// ������ �� ������ ����
        ///  ��� ���������� �������� ������ � ������ ������ ������� � �������� ����������
        /// </summary>
        private static FormWait _this;
        private Point _location;
        //private bool _focused;
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

            //BackgroundWorker threadShow = new BackgroundWorker ();
            //threadShow.

            m_threadShowDialog = new BackgroundWorker (); //new Thread(new ParameterizedThreadStart(fThreadProcShowDialog));
            //m_threadShow.IsBackground = true;
            //m_threadShow.Name = @"FormWait.Thread - SHOWDIALOG";
            //m_threadShow.IsBackground = true;
            //m_threadShow.Start(null);
            m_threadShowDialog.DoWork += new DoWorkEventHandler(fThreadProcShowDialog_DoWork);
            m_threadShowDialog.RunWorkerCompleted += new RunWorkerCompletedEventHandler(fThreadShowDialog_RunWorkerCompleted);
            m_threadShowDialog.RunWorkerAsync();

            m_threadHide = new BackgroundWorker(); //new Thread(new ParameterizedThreadStart(fThreadProcClose));
            //m_threadHide.IsBackground = true;
            //m_threadHide.Name = @"FormWait.Thread - CLOSE";
            //m_threadHide.IsBackground = true;
            //m_threadHide.Start(null);
            m_threadHide.DoWork += new DoWorkEventHandler(fThreadProcClose);
            m_threadHide.RunWorkerAsync();

            m_threadState = new BackgroundWorker(); //new Thread(new ParameterizedThreadStart(fThreadProcState));
            //m_threadState.IsBackground = true;
            //m_threadState.Name = @"FormWait.Thread - STATE";
            //m_threadState.IsBackground = true;
            //m_threadState.Start(null);
            m_threadState.DoWork += new DoWorkEventHandler(fThreadProcState);
            m_threadState.RunWorkerAsync();

            delegateFuncShowDialog = new DelegateFunc (showDialog);
            delegateFuncClose = new DelegateFunc (close);

            //Shown += new EventHandler(FormWait_Shown);
            this.HandleCreated += new EventHandler(FormWait_Shown);
            FormClosed +=new FormClosedEventHandler(FormWait_FormClosed);
            //this.HandleDestroyed += new EventHandler(FormWait_HandleDestroyed);
        }
        /// <summary>
        /// ������� �� ����������� ����
        /// </summary>
        /// <param name="ptLocationParent">������� ����������� ������������� ����</param>
        /// <param name="szParent">������ ������������� ����</param>
        public void StartWaitForm(Point ptParent, Size szParent)
        {
            lock (lockState)
            {
                ////������������� ���� � 'FormWait::StartWaitForm'
                //Logging.Logg().Warning(@"FormWait::StartWaitForm (_state=" + _state.ToString () + @", _waitCounter=" + _waitCounter + @") - ����...", Logging.INDEX_MESSAGE.NOT_SET);
                //Console.WriteLine(@"FormWait::START; _state=" + _state.ToString () + @", waitCounter=" + waitCounter);

                _waitCounter++;

                if (_state == STATE.UNVISIBLED)
                {
                    //���������� ���������� ��� �����������
                    setLocation(ptParent, szParent);
                    //��������� �����������
                    m_arSyncManaged[(int)INDEX_SYNCSTATE.SHOWDIALOG].Set();

                    _state = STATE.SHOWING;
                }
                else
                    //if (_state == STATE.CLOUSING)
                    //    _waitCounter++;
                    //else
                        ;
            }
        }
        /// <summary>
        /// ����� � ����������� ����
        /// </summary>
        /// <param name="bStopped"></param>
        public void StopWaitForm(bool bExit = false)
        {
            lock (lockState)
            {
                ////������������� ���� � 'FormWait::StartWaitForm'
                //Logging.Logg().Warning(@"FormWait::StopWaitForm (_state=" + _state.ToString() + @", _waitCounter=" + _waitCounter + @") - ����...", Logging.INDEX_MESSAGE.NOT_SET);

                if (_waitCounter > 0)
                    _waitCounter--;
                else
                    ;

                if (_state == STATE.VISIBLED)
                {
                    m_arSyncManaged[(int)INDEX_SYNCSTATE.CLOSE].Set();

                    _state = STATE.CLOUSING;
                }
                else
                    //if (_state == STATE.SHOWING)
                    //    _waitCounter--;
                    //else
                    //
                        ;

                //Console.WriteLine(@"FormWait::STOP; waitCounter=" + waitCounter);

                if (bExit == true)
                {
                    //bool bClosed = false;
                    //if (!(_state == STATE.UNVISIBLED))
                    //    //������� �������� ����
                    //    bClosed = m_arSyncStates[(int)INDEX_SYNCSTATE.CLOSE - 1].WaitOne()
                    //    ;
                    //else
                    //    ;
                    
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

            //Logging.Logg().Warning(@"FormWait::StopWaitForm (_state=" + _state.ToString() + @", _waitCounter=" + _waitCounter + @") - �����...", Logging.INDEX_MESSAGE.NOT_SET);
        }

        private void showDialog()
        {
            //Logging.Logg().Debug(@"FormWait::showDialog () - !!!!!!!!!!!!!", Logging.INDEX_MESSAGE.NOT_SET);

            Location = _location;
            ShowDialog();
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
        private void setLocation(Point ptParent, Size szParent)
        {
            //_parent = parent;
            //_focused = focused; //_parent.Focused;
            //_location = new Point(_parent.Location.X + (_parent.Size.Width - this.Width) / 2, _parent.Location.Y + (_parent.Size.Height - this.Height) / 2);
            _location = new Point(ptParent.X + (szParent.Width - this.Width) / 2, ptParent.Y + (szParent.Height - this.Height) / 2);
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

        //private void FormWait_HandleDestroyed(object sender, EventArgs e)
        //{
        //    m_arSyncStates[(int)INDEX_SYNCSTATE.CLOSE - 1].Set();
        //}
        ///// <summary>
        ///// ��������� ������� ����������� ����
        ///// </summary>
        ///// <param name="data">�������� ��� ������� ������</param>
        //private void fThreadProcShowDialog(object data)
        private void fThreadProcShowDialog_DoWork(object obj, DoWorkEventArgs ev)
        {
            INDEX_SYNCSTATE indx = INDEX_SYNCSTATE.UNKNOWN;

            while (!(indx == INDEX_SYNCSTATE.EXIT))
            {
                //������� ���������� �� ���������� ��������
                indx = (INDEX_SYNCSTATE)WaitHandle.WaitAny(new WaitHandle [] { m_arSyncManaged[(int)INDEX_SYNCSTATE.EXIT]
                                                                                , m_arSyncManaged[(int)INDEX_SYNCSTATE.SHOWDIALOG] });
                ////������������� �������
                //Logging.Logg().Debug(@"FormWait::fThreadProcShowDialog () - indx=" + ((INDEX_SYNCSTATE)indx).ToString() + @"...", Logging.INDEX_MESSAGE.NOT_SET);
                //Console.WriteLine(@"FormMainBase::fThreadProcShowDialog () - indx=" + indx.ToString() + @" - ...");

                switch (indx)
                {
                    case INDEX_SYNCSTATE.EXIT: // ���������� ��������� �������
                        break;
                    case INDEX_SYNCSTATE.SHOWDIALOG: // ���������� ����
                        //if (InvokeRequired == true)
                        //    BeginInvoke(delegateFuncShowDialog);
                        //else
                            showDialog();
                        break;
                    default:
                        break;
                }
            }

            //Logging.Logg().Debug(@"FormMainBase::fThreadProcShowDialog () - indx=" + indx.ToString() + @" - ...", Logging.INDEX_MESSAGE.NOT_SET);
        }

        private void fThreadProcShowDialog_RunWorkerCompleted(object obj, RunWorkerCompletedEventArgs ev)
        {
        }
        ///// <summary>
        ///// ��������� ������� ������ � ����������� �����
        ///// </summary>
        ///// <param name="data">�������� ��� ������� ������</param>
        //private void fThreadProcClose(object data)
        private void fThreadProcClose(object obj, DoWorkEventArgs ev)
        {
            INDEX_SYNCSTATE indx = INDEX_SYNCSTATE.UNKNOWN;

            while (!(indx == INDEX_SYNCSTATE.EXIT))
            {
                indx = (INDEX_SYNCSTATE)WaitHandle.WaitAny(new WaitHandle[] { m_arSyncManaged[(int)INDEX_SYNCSTATE.EXIT]
                                                                                , m_arSyncManaged[(int)INDEX_SYNCSTATE.CLOSE] });
                indx = indx == INDEX_SYNCSTATE.EXIT ? INDEX_SYNCSTATE.EXIT : indx + 1;
                ////������������� �������
                //Logging.Logg().Debug(@"FormWait::fThreadProcClose () - indx=" + ((INDEX_SYNCSTATE)indx).ToString() + @"...", Logging.INDEX_MESSAGE.NOT_SET);
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

        //private void fThreadProcState(object data)
        private void fThreadProcState(object obj, DoWorkEventArgs ev)
        {
            INDEX_SYNCSTATE indx = INDEX_SYNCSTATE.UNKNOWN;

            while (!(indx == INDEX_SYNCSTATE.EXIT))
            {
                indx = (INDEX_SYNCSTATE)WaitHandle.WaitAny(new WaitHandle[] { m_arSyncManaged[(int)INDEX_SYNCSTATE.EXIT]
                                                                                , m_arSyncStates[(int)INDEX_SYNCSTATE.SHOWDIALOG - 1]
                                                                                , m_arSyncStates[(int)INDEX_SYNCSTATE.CLOSE - 1]});
                ////������������� �������
                //Logging.Logg().Debug(@"FormWait::fThreadProcState () - indx=" + ((INDEX_SYNCSTATE)indx).ToString() + @"...", Logging.INDEX_MESSAGE.NOT_SET);
                //Console.WriteLine(@"FormMainBase::fThreadProcState () - indx=" + indx.ToString() + @" - ...");

                switch (indx)
                {
                    case INDEX_SYNCSTATE.EXIT: // ���������� ��������� �������
                        break;
                    case INDEX_SYNCSTATE.SHOWDIALOG: // ���������� ����
                        lock (lockState)
                        {
                            _state = STATE.VISIBLED;

                            if (isContinue == false)
                            {
                                m_arSyncManaged[(int)INDEX_SYNCSTATE.CLOSE].Set ();

                                _state = STATE.CLOUSING;
                            }
                            else
                                ;
                        }
                        break;
                    case INDEX_SYNCSTATE.CLOSE: // ����� � ����������� ����
                        lock (lockState)
                        {
                            _state = STATE.UNVISIBLED;

                            if (isContinue == true)
                            {
                                _waitCounter --;

                                m_arSyncManaged[(int)INDEX_SYNCSTATE.SHOWDIALOG].Set();

                                _state = STATE.SHOWING;
                            }
                            else
                                ;
                        }
                        break;
                    default:
                        break;
                }
            }
        }
    }
}