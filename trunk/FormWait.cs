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

        public Semaphore m_semaFormClosed;

        public FormWait()
        {
            InitializeComponent();
            started = false;

            m_semaHandleCreated = new Semaphore(1, 1);
            m_semaHandleCreated.WaitOne ();

            m_semaFormClosed = new Semaphore(1, 1);

            this.HandleCreated += new EventHandler(FormWait_HandleCreated);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.WaitForm_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.WaitForm_FormClosed);
        }

        public void StartWaitForm()
        {
            if (started == false)
            {
                started = true;

                //if (IsHandleCreated == true)
                    if (InvokeRequired == true)
                        BeginInvoke(new DelegateFunc (show));
                    else
                        show ();
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
                        BeginInvoke(new DelegateFunc (hide));
                    else
                        hide ();
                else
                    ;
            }
            else
                ;
        }

        private void show()
        {
            this.ShowDialog ();
            Console.WriteLine(@"FormWait::startWaitForm () - ...");
        }

        private void hide()
        {
            this.Close();
            Console.WriteLine(@"FormWait::stopWaitForm () - ...");
        }

        private void FormWait_HandleCreated(object sender, EventArgs e)
        {
            m_semaHandleCreated.Release(1);
        }

        private void WaitForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (started == true)
                //Отменить закрытие, если установлен признак отображения
                e.Cancel = true;
            else
                ;
            Console.WriteLine(@"FormWait::WaitForm_FormClosing (started=" + started.ToString() + @") - ...");
        }

        private void WaitForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            m_semaFormClosed.Release(1);
        }
    }
}