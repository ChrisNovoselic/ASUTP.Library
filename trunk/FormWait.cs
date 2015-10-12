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
        public Semaphore [] m_arSemaStop;

        public FormWait()
        {
            Logging.Logg().Debug(@"FormWait::ctor () - ...", Logging.INDEX_MESSAGE.NOT_SET);
            
            InitializeComponent();
            started = false;

            m_semaHandleCreated = new Semaphore(0, 1);
            m_arSemaStop = new Semaphore [] {
                new Semaphore(1, 1)
                , new Semaphore(1, 1)
            };

            this.FormClosing += new FormClosingEventHandler(FormWait_FormClosing);
            this.FormClosed += new FormClosedEventHandler(FormWait_FormClosed);
            this.HandleCreated += new EventHandler(FormWait_HandleCreated);
            this.HandleDestroyed += new EventHandler(FormWait_HandleDestroyed);
        }

        public void StartWaitForm()
        {
            if (started == false)
            {
                started = true;

                //if (IsHandleCreated == true)
                if (InvokeRequired == true)
                    BeginInvoke(new DelegateFunc(startWaitForm));
                else
                    startWaitForm();
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
                        BeginInvoke(new DelegateFunc(stopWaitForm));
                    else
                        stopWaitForm();
                else
                    ;
            }
            else
                ;
        }

        private void startWaitForm()
        {
            this.ShowDialog();
        }

        private void stopWaitForm()
        {
            this.Hide ();
            //this.Close();
        }

        private void FormWait_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (started == true)
                e.Cancel = true;
            else
                ;
        }

        private void FormWait_FormClosed(object sender, FormClosedEventArgs e)
        {
            m_arSemaStop[0].Release(1);
        }

        private void FormWait_HandleCreated(object sender, EventArgs e)
        {
            m_semaHandleCreated.Release(1);
        }

        private void FormWait_HandleDestroyed(object sender, EventArgs e)
        {
            m_arSemaStop[1].Release(1);
        }
    }
}