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
            ;
        private enum INDEX_SYNCSTATE { UNKNOWN = -1, EXIT, SHOWDIALOG, CLOSE, COUNT_INDEX_SYNCSTATE }

        private DelegateFunc delegateFuncClose
            //, delegateFuncShowDialog
            ;

        /// <summary>
        /// ������� ������� ����
        /// </summary>
        private bool isContinue { get { return _waitCounter > 0; } }

        private Semaphore m_semaRunWorkerCompleted;
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
        public static FormWait This { get { if (_this == null) _this = new FormWait(); else ; return _this; } }
        /// <summary>
        /// ����������� - �������� (��� ����������)
        /// </summary>
        private FormWait()
            : base()
        {
            InitializeComponent();
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            //������������� �������� �������� ���-�� ������� �� ����������� �����
            lockState = new object();
            _state = STATE.UNVISIBLED;
            _waitCounter = 0;
            //m_dtStartShow = DateTime.MinValue;

            m_semaRunWorkerCompleted = new Semaphore(0, 1);

            m_threadShowDialog = new BackgroundWorker();
            m_threadShowDialog.DoWork += new DoWorkEventHandler(fThreadProcShowDialog_DoWork);
            m_threadShowDialog.RunWorkerCompleted += new RunWorkerCompletedEventHandler(fThreadProcShowDialog_RunWorkerCompleted);

            //delegateFuncShowDialog = new DelegateFunc(showDialog);
            delegateFuncClose = new DelegateFunc(close);

            this.Shown += new EventHandler(FormWait_Shown);
            //this.HandleCreated += new EventHandler(FormWait_Shown);
            //FormClosed += new FormClosedEventHandler(FormWait_FormClosed);
            //this.HandleDestroyed += new EventHandler(FormWait_HandleDestroyed);
        }
        /// <summary>
        /// ������� �� ����������� ����
        /// </summary>
        /// <param name="ptLocationParent">������� ����������� ������������� ����</param>
        /// <param name="szParent">������ ������������� ����</param>
        public void StartWaitForm(Point ptParent, Size szParent)
        {
            //������������� ���� � 'FormWait::StartWaitForm'
            //Logging.Logg().Warning(@"FormWait::StartWaitForm () - ����...", Logging.INDEX_MESSAGE.NOT_SET);
            //Logging.Logg().Warning(@"FormWait::StartWaitForm (_state=" + _state.ToString() + @", _waitCounter=" + _waitCounter + @") - ����...", Logging.INDEX_MESSAGE.NOT_SET);

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
                    m_threadShowDialog.RunWorkerAsync();

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
                    if (InvokeRequired == true)
                        BeginInvoke(delegateFuncClose);
                    else
                        close();

                    _state = STATE.CLOUSING;
                }
                else
                    //if (_state == STATE.SHOWING)
                    //    _waitCounter--;
                    //else
                    //
                    ;

                //Console.WriteLine(@"FormWait::STOP; waitCounter=" + waitCounter);
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
            Close();
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
            lock (lockState)
            {
                _state = STATE.VISIBLED;

                if (isContinue == false)
                {
                    _state = STATE.CLOUSING;

                    if (InvokeRequired == true)
                        BeginInvoke(delegateFuncClose);
                    else
                        close();
                }
                else
                    ;
            }
        }
        ///// <summary>
        ///// ���������� ������� - 
        ///// </summary>
        ///// <param name="sender">������, �������������� ������� - this</param>
        ///// <param name="e">�������� �������</param>
        //private void FormWait_FormClosed(object sender, FormClosedEventArgs e)
        //{
        //}

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
            ////������������� �������
            //Logging.Logg().Debug(@"FormMainBase::fThreadProcShowDialog_DoWork () - _state=" + _state.ToString() + @" - ...", Logging.INDEX_MESSAGE.NOT_SET);
            //Console.WriteLine(@"FormMainBase::fThreadProcShowDialog_DoWork () - indx=" + indx.ToString() + @" - ...");

            //if (InvokeRequired == true)
            //    BeginInvoke(delegateFuncShowDialog);
            //else
            showDialog();

            //Logging.Logg().Debug(@"FormMainBase::fThreadProcShowDialog () - indx=" + indx.ToString() + @" - ...", Logging.INDEX_MESSAGE.NOT_SET);
        }

        private void fThreadProcShowDialog_RunWorkerCompleted(object obj, RunWorkerCompletedEventArgs ev)
        {
            //Logging.Logg().Debug(@"FormMainBase::fThreadProcShowDialog_RunWorkerCompleted () - _state=" + _state.ToString() + @" - ...", Logging.INDEX_MESSAGE.NOT_SET);

            lock (lockState)
            {
                _state = STATE.UNVISIBLED;

                if (isContinue == true)
                {
                    _state = STATE.SHOWING;

                    m_threadShowDialog.RunWorkerAsync();
                }
                else
                    ;
            }
        }
    }
}