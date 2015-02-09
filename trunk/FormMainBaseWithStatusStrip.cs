using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Threading;
using System.Drawing;

namespace HClassLibrary
{
    public class HReports
    {
        private volatile string m_last_error;
        private DateTime m_last_time_error;
        private volatile bool m_errored_state;

        private volatile string m_last_warning;
        private DateTime m_last_time_warning;
        private volatile bool m_warninged_state;

        private volatile string m_last_action;
        private DateTime m_last_time_action;
        private volatile bool m_actioned_state;

        public string last_error { get { return m_last_error; } set { m_last_error = value; } }
        public DateTime last_time_error { get { return m_last_time_error; } set { m_last_time_error = value; } }
        public bool errored_state { get { return m_errored_state; } set { m_errored_state = value; } }

        public string last_warning { get { return m_last_warning; } set { m_last_warning = value; } }
        public DateTime last_time_warning { get { return m_last_time_warning; } set { m_last_time_warning = value; } }
        public bool warninged_state { get { return m_warninged_state; } set { m_warninged_state = value; } }

        public string last_action { get { return m_last_action; } set { m_last_action = value; } }
        public DateTime last_time_action { get { return m_last_time_action; } set { m_last_time_action = value; } }
        public bool actioned_state { get { return m_actioned_state; } set { m_actioned_state = value; } }

        //private event EventHandler errored_stateChanged;
        //private event EventHandler warninged_stateChanged;
        //private event EventHandler actioned_stateChanged;

        public HReports () {
            ClearStates (true);

            //this.errored_stateChanged += new EventHandler(OnErrored_stateChanged);
            //this.errored_stateChanged += new EventHandler(OnWarninged_stateChanged);
            //this.actioned_stateChanged += new EventHandler(OnActioned_stateChanged);            
        }

        private void OnErrored_stateChanged (object obj, EventArgs ev) {
        }

        private void OnWarninged_stateChanged(object obj, EventArgs ev)
        {
        }

        private void OnActioned_stateChanged(object obj, EventArgs ev)
        {
        }

        public void ClearStates (bool bForce) {
            if (bForce == true)
                errored_state = warninged_state = actioned_state = false;
            else {
                if (errored_state == false)
                    if (warninged_state == false)
                        actioned_state = false;
                    else
                        ;
                else
                    ;
            }
        }

        public void ErrorReport(string msg)
        {
            last_error = msg;
            last_time_error = DateTime.Now;
            errored_state = true;
        }

        public void WarningReport(string msg)
        {
            last_warning = msg;
            last_time_warning = DateTime.Now;
            warninged_state = true;
        }

        public void ActionReport (string msg) {
            last_action = msg;
            last_time_action = DateTime.Now;
            actioned_state = true;
        }
    };

    public abstract class FormMainBaseWithStatusStrip : FormMainBase
    {
        public static List<FormConnectionSettings> s_listFormConnectionSettings;
        
        public static System.Windows.Forms.StatusStrip m_statusStripMain;
        protected System.Windows.Forms.ToolStripStatusLabel m_lblMainState;
        protected System.Windows.Forms.ToolStripStatusLabel m_lblDescMessage;
        protected System.Windows.Forms.ToolStripStatusLabel m_lblDateMessage;

        protected System.Windows.Forms.Timer m_timer;

        public static HReports m_report;

        protected FormMainBaseWithStatusStrip()
        {
            InitializeComponent();

            // 
            // timer
            // 
           start ();

            delegateEvent = new DelegateFunc(EventRaised);

            m_report = new HReports();
            //MessageBox.Show((IWin32Window)null, @"FormMain::FormMain () - new HReports ()", @"Отладка!");

            // m_statusStripMain
            FormMainBaseWithStatusStrip.m_statusStripMain.Location = new System.Drawing.Point(0, 762);
            FormMainBaseWithStatusStrip.m_statusStripMain.Size = new System.Drawing.Size(982, 22);
            // m_lblMainState
            this.m_lblMainState.Size = new System.Drawing.Size(150, 17);
            // m_lblDateMessage
            this.m_lblDateMessage.Size = new System.Drawing.Size(150, 17);
            // m_lblDescMessage
            this.m_lblDescMessage.Size = new System.Drawing.Size(667, 17);

            delegateUpdateActiveGui = new DelegateIntFunc(UpdateActiveGui);
            delegateHideGraphicsSettings = new DelegateFunc(HideGraphicsSettings);
        }

        protected abstract void UpdateActiveGui (int type);
        protected abstract void HideGraphicsSettings();

        private void InitializeComponent()
        {
            FormMainBaseWithStatusStrip.m_statusStripMain = new System.Windows.Forms.StatusStrip();
            this.m_lblMainState = new System.Windows.Forms.ToolStripStatusLabel();
            this.m_lblDateMessage = new System.Windows.Forms.ToolStripStatusLabel();
            this.m_lblDescMessage = new System.Windows.Forms.ToolStripStatusLabel();

            FormMainBaseWithStatusStrip.m_statusStripMain.SuspendLayout();

            // 
            // m_statusStripMain
            // 
            FormMainBaseWithStatusStrip.m_statusStripMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_lblMainState,
            this.m_lblDateMessage,
            this.m_lblDescMessage});
            //this.m_statusStripMain.Location = new System.Drawing.Point(0, 762);
            FormMainBaseWithStatusStrip.m_statusStripMain.Name = "m_statusStripMain";
            //this.m_statusStripMain.Size = new System.Drawing.Size(982, 22);
            FormMainBaseWithStatusStrip.m_statusStripMain.TabIndex = 4;
            // 
            // m_lblMainState
            // 
            this.m_lblMainState.AutoSize = false;
            this.m_lblMainState.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Top)
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right)
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom)));
            this.m_lblMainState.BorderStyle = System.Windows.Forms.Border3DStyle.SunkenOuter;
            this.m_lblMainState.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold);
            //this.m_lblMainState.ForeColor = System.Drawing.Color.Red;
            this.m_lblMainState.Name = "m_lblMainState";
            //this.m_lblMainState.Size = new System.Drawing.Size(150, 17);
            // 
            // m_lblDateMessage
            // 
            this.m_lblDateMessage.AutoSize = false;
            this.m_lblDateMessage.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Top)
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right)
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom)));
            this.m_lblDateMessage.BorderStyle = System.Windows.Forms.Border3DStyle.SunkenOuter;
            this.m_lblDateMessage.Name = "m_lblDateMessage";
            //this.m_lblDateMessage.Size = new System.Drawing.Size(150, 17);
            // 
            // m_lblDescMessage
            // 
            this.m_lblDescMessage.AutoSize = false;
            this.m_lblDescMessage.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Top)
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right)
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom)));
            this.m_lblDescMessage.BorderStyle = System.Windows.Forms.Border3DStyle.SunkenOuter;
            this.m_lblDescMessage.Name = "m_lblDescMessage";
            //this.m_lblDescMessage.Size = new System.Drawing.Size(667, 17);
            this.m_lblDescMessage.Spring = true;

            this.Controls.Add(FormMainBaseWithStatusStrip.m_statusStripMain);

            FormMainBaseWithStatusStrip.m_statusStripMain.ResumeLayout(false);
            FormMainBaseWithStatusStrip.m_statusStripMain.PerformLayout();
        }

        protected void EventRaised()
        {
            lock (lockEvent)
            {
                int type = UpdateStatusString();
                switch (type)
                {
                    case -1:
                        this.m_lblMainState.ForeColor = System.Drawing.Color.Red;
                        break;
                    case 1:
                        this.m_lblMainState.ForeColor = System.Drawing.Color.Yellow;
                        break;
                    case 0:
                    default:
                        this.m_lblMainState.ForeColor = System.Drawing.Color.Black;
                        break;
                }
                m_lblDescMessage.Invalidate();
                m_lblDateMessage.Invalidate();
            }
        }

        protected virtual void ErrorReport()
        {
            //if (IsHandleCreated/*InvokeRequired*/ == true)
            try
            {
                m_statusStripMain.BeginInvoke(delegateEvent);
            } catch (Exception e) {
                    Logging.Logg().Exception(e, Logging.INDEX_MESSAGE.D_001, @"FormMainBaseWithStatusStrip::ErrorReport () - ... BeginInvoke (delegateEvent) - ...");
            }
            //else
            //    Logging.Logg().Error(@"FormMainBaseWithStatusStrip::ErrorReport () - ... BeginInvoke (delegateEvent) - ...");
        }

        protected virtual void WarningReport()
        {
            if (IsHandleCreated/*InvokeRequired*/ == true) {
                m_statusStripMain.BeginInvoke(delegateEvent);
            }
            else
                Logging.Logg().Error(@"FormMainBaseWithStatusStrip::WarningReport () - ... BeginInvoke (delegateEvent) - ...", Logging.INDEX_MESSAGE.D_001);
        }

        protected virtual void ActionReport()
        {
            if (IsHandleCreated/*InvokeRequired*/ == true)
                m_statusStripMain.BeginInvoke(delegateEvent);
            else
                Logging.Logg().Error(@"FormMainBaseWithStatusStrip::ActionReport () - ... BeginInvoke (delegateEvent) - ...", Logging.INDEX_MESSAGE.D_001);
        }

        protected abstract int UpdateStatusString();

        protected abstract void timer_Start ();

        private void timer_Tick(object sender, EventArgs e)
        {
            if (m_timer.Interval == ProgramBase.TIMER_START_INTERVAL)
            {
                timer_Start ();

                m_timer.Interval = 1000;
            }

            lock (lockEvent)
            {
                int have_msg = UpdateStatusString();

                if (have_msg == -1)
                    m_lblMainState.Text = "ОШИБКА";
                else
                    if (have_msg == 1)
                        m_lblMainState.Text = "Предупреждение";
                    else
                        ;

                if ((have_msg == 0) || (show_error_alert == false))
                    m_lblMainState.Text = "";
                else
                    ;

                show_error_alert = !show_error_alert;
                m_lblDescMessage.Invalidate();
                m_lblDateMessage.Invalidate();
            }
        }

        private void start () {
        }
        
        protected virtual void Start () {
            if (m_timer == null) m_timer = new System.Windows.Forms.Timer(); else ;
            m_timer.Interval = ProgramBase.TIMER_START_INTERVAL; //Признак первой итерации
            m_timer.Start();
            m_timer.Tick += new System.EventHandler(this.timer_Tick);
            start ();
        }

        protected virtual void Stop()
        {
            if (! (m_timer == null)) {
                m_timer.Stop();
                m_timer.Dispose ();
                m_timer = null;
            } else
                ;
        }
    }
}
