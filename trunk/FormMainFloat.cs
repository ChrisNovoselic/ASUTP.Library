using System;
using System.Windows.Forms;
using System.Threading;
using System.Drawing;

namespace HClassLibrary
{    
    public class FormMainFloatBase : FormMainBaseWithStatusStrip
    {
        private const int ROW_COUNT = 22;
        
        public DelegateObjectFunc delegateFormClosing
            , delegateFormLoad;
        private TableLayoutPanel m_container;
        private Label m_label;

        public FormMainFloatBase(string text, Panel child, bool bLabel = true)
        {
            Text = text;
            
            int iRowPos = 0;
            if (bLabel == true)
            {
                iRowPos = 1;
                m_label = new Label();
                m_label.Text = this.Text;
            }
            else
                ;
            InitializeComponent();

            this.Width = child.Width + 1; this.Height = child.Height + 1;

            this.m_container.SuspendLayout();

            this.m_container.Controls.Add(child, 0, iRowPos);
            this.m_container.SetColumnSpan(child, 1); this.m_container.SetRowSpan(child, ROW_COUNT - iRowPos);

            this.m_container.ResumeLayout(false);
            this.m_container.PerformLayout();

            this.m_container.SizeChanged += new EventHandler(container_onSizeChanged);
        }

        private void InitializeComponent()
        {
            int i = -1;
            m_container = new TableLayoutPanel();
            m_container.Dock = DockStyle.Fill;

            this.SuspendLayout ();

            m_container.SuspendLayout();

            m_container.ColumnCount = 1;
            m_container.RowCount = ROW_COUNT;

            for (i = 0; i < m_container.ColumnCount; i ++)
                m_container.ColumnStyles.Add (new ColumnStyle (SizeType.AutoSize));

            for (i = 0; i < m_container.RowCount; i++)
                m_container.RowStyles.Add(new RowStyle(SizeType.Percent, 100 / ROW_COUNT));

            if (!(m_label == null))
            {
                m_label.ForeColor = Color.Red;
                m_label.TextAlign = ContentAlignment.MiddleCenter;
                m_label.Padding = new System.Windows.Forms.Padding(1, 1, 1, 1);
                m_label.Dock = DockStyle.Fill;

                m_container.Controls.Add(m_label, 0, 0);
                this.m_container.SetColumnSpan(m_label, 1); this.m_container.SetRowSpan(m_label, 1);
            }
            else
                ;

            m_container.ResumeLayout (false);
            m_container.PerformLayout ();

            m_container.Location = new Point(1, 1);
            this.m_container.Size = new System.Drawing.Size(this.Width - 6, this.Height - this.m_statusStripMain.Height - 26); //26 - высота главного меню
            m_container.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.Controls.Add(m_container);           

            this.ResumeLayout (false);
            this.PerformLayout ();

            this.Load += new EventHandler(FormMainFloat_Load);
            this.FormClosing += new FormClosingEventHandler(FormMainFloat_FormClosing);
        }

        protected override void UpdateActiveGui(int type) { }

        protected override void HideGraphicsSettings() { }

        protected override void timer_Start()
        {
        }

        protected override int UpdateStatusString()
        {
            int have_msg = -1;

            if (Controls.Count > 0) // == 1
            {
                have_msg = (m_report.errored_state == true) ? -1 : (m_report.warninged_state == true) ? 1 : 0;

                //Console.WriteLine(@"FormMainFloat::UpdateStatusString () - have_msg=" + have_msg + @", actioned_state=" + m_report.actioned_state);

                if (((!(have_msg == 0)) || (m_report.actioned_state == true))
                    //&& (!(Controls[0] < 0))
                )
                {
                    if (m_report.actioned_state == true)
                    {
                        m_lblDescMessage.Text = m_report.last_action;
                        m_lblDateMessage.Text = m_report.last_time_action.ToString();
                    }
                    else
                        ;

                    if (have_msg == 1)
                    {
                        m_lblDescMessage.Text = m_report.last_warning;
                        m_lblDateMessage.Text = m_report.last_time_warning.ToString();
                    }
                    else
                        ;

                    if (have_msg == -1)
                    {
                        m_lblDescMessage.Text = m_report.last_error;
                        m_lblDateMessage.Text = m_report.last_time_error.ToString();
                    }
                    else
                        ;
                }
                else
                {
                    m_lblDescMessage.Text = string.Empty;
                    m_lblDateMessage.Text = string.Empty;
                }
            }
            else
                ;

            return have_msg;
        }

        // Перехват нажатия на кнопку свернуть
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x112)
            {
                if (m.WParam.ToInt32() == 0xF020)
                {
                    Close ();

                    return;
                }
                else
                    ;
            }
            else
                ;

            base.WndProc(ref m);
        }

        private void FormMainFloat_Load(object sender, EventArgs e)
        {
            Start(); //Старт 1-сек-го таймера для строки стостояния
            
            delegateFormLoad(new object[] { sender, e, new DelegateStringFunc[] { ErrorReport, WarningReport, ActionReport }, new DelegateBoolFunc(ReportClear) });

            labelFitFont();
        }

        private void FormMainFloat_FormClosing(object sender, FormClosingEventArgs e)
        {
            Stop ();            

            delegateFormClosing (new object [] { sender, e});

            //this.m_container.Controls.RemoveAt(0); //Дочернмй элемент д.б. ЕДИНственный
        }
        /// <summary>
        /// Обработчик события - изменение размера панели
        /// </summary>
        /// <param name="obj">Объект, инициировавший событие (контейнер панели)</param>
        /// <param name="ev">Аргумент события</param>
        private void container_onSizeChanged(object obj, EventArgs ev)
        {
            labelFitFont();
        }

        private void labelFitFont()
        {
            Font font = null;
            
            if (!(m_label == null))
            {
                font = HLabel.FitFont(m_label.CreateGraphics(), m_label.Text, m_label.ClientSize, new SizeF (0.95F, 0.95F), 0.05F);
                m_label.Font = new Font(
                        font.Name
                        , font.Size
                        , FontStyle.Bold
                        , font.Unit
                        , font.GdiCharSet
                    );
            }
            else
                ;
        }

        public Panel GetPanel ()
        {
            Panel panelRes = null;

            foreach (Control ctrl in m_container.Controls)
                if (ctrl is Panel)
                {
                    panelRes = ctrl as Panel;

                    break;
                }
                else
                    ;

            return panelRes;
        }
    }
}