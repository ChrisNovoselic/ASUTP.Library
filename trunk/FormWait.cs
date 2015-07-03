using System;
using System.Threading;
using System.Windows.Forms;

namespace HClassLibrary
{
    /// <summary>
    /// Класс для описания окна визуализации длительного выполнения операции
    /// </summary>
    public partial class FormWait : Form
    {
        /// <summary>
        /// Признак запуска окна
        /// </summary>
        private bool started;
        /// <summary>
        /// Объект синхронизации - блокирует использующие потоки до момента создания дескриптора окна
        /// </summary>
        public Semaphore m_semaHandleCreated;

        public FormWait()
        {
            InitializeComponent();
            started = false;

            m_semaHandleCreated = new Semaphore(1, 1);
            m_semaHandleCreated.WaitOne ();

            this.HandleCreated += new EventHandler(FormWait_HandleCreated);
        }

        public void StartWaitForm()
        {
            if (started == false)
            {
                started = true;

                //if (IsHandleCreated == true)
                    if (InvokeRequired == true)
                        BeginInvoke(new DelegateFunc (startWaitForm));
                    else
                        startWaitForm ();
                //else ;
            }
            else
                ;
        }

        public void StopWaitForm()
        {
            if (started == true)
            {
                started = false;

                if (IsHandleCreated == true)
                    if (InvokeRequired == true)
                        BeginInvoke(new DelegateFunc (stopWaitForm));
                    else
                        stopWaitForm ();
                else
                    ;
            }
            else
                ;
        }

        private void startWaitForm()
        {
            this.ShowDialog ();
        }

        private void stopWaitForm()
        {
            this.Close();
        }

        private void WaitForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (started == true)
                e.Cancel = true;
            else
                ;
        }

        private void FormWait_HandleCreated(object sender, EventArgs e)
        {
            m_semaHandleCreated.Release (1);
        }
    }
}