using System;
using System.Windows.Forms;
using System.Threading;
using System.Drawing;

namespace HClassLibrary
{    
    public class FormMainFloat : FormMainBaseWithStatusStrip
    {
        public DelegateObjectFunc delegateFormClosing
            , delegateFormLoad;
        
        public FormMainFloat(Panel child)
        {
            this.Width = child.Width; this.Height = child.Height;
            this.Controls.Add(child);
            
            InitializeComponent();
        }

        private void InitializeComponent()
        {
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

        // �������� ������� �� ������ ��������
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
            Start(); //����� 1-���-�� ������� ��� ������ ����������
            
            delegateFormLoad(new object[] { sender, e, new DelegateStringFunc[] { ErrorReport, WarningReport, ActionReport }, new DelegateBoolFunc(ReportClear) });
        }

        private void FormMainFloat_FormClosing(object sender, FormClosingEventArgs e)
        {
            Stop ();

            this.Controls.RemoveAt(1); //�������� ������� �.�. ������������ (0 - ������ ��� �����������)

            delegateFormClosing (new object [] { sender, e});
        }
    }
}