using System;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;

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
        ///// <summary>
        ///// Объект синхронизации - блокирует использующие потоки до момента создания дескриптора окна
        ///// </summary>
        //public Semaphore m_semaHandleCreated;

        //public Semaphore m_semaFormClosed;

        private static FormWait _this;
        
        public static FormWait This { get { if (_this == null) _this = create (); else ; return _this; } }

        private FormWait () : base ()
        {
        }

        private static FormWait create()
        {
            FormWait objRes = new FormWait ();

            objRes.InitializeComponent();
            objRes.started = false;

            //m_semaHandleCreated = new Semaphore(1, 1);
            //m_semaHandleCreated.WaitOne ();

            //objRes.m_semaFormClosed = new Semaphore(1, 1);

            objRes.HandleCreated += new EventHandler(objRes.FormWait_HandleCreated);
            objRes.FormClosing += new System.Windows.Forms.FormClosingEventHandler(objRes.WaitForm_FormClosing);
            objRes.FormClosed += new System.Windows.Forms.FormClosedEventHandler(objRes.WaitForm_FormClosed);

            return objRes;
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
            _this.ShowDialog ();
            Console.WriteLine(@"FormWait::startWaitForm () - ...");
        }

        private void hide()
        {
            _this.Close();
            Console.WriteLine(@"FormWait::stopWaitForm () - ...");
        }

        public void SetLocation ()
        {
            //Location = new Point(Parent.Location.X + (Parent.Width - this.Width) / 2, Parent.Location.Y + (Parent.Height - this.Height) / 2);
        }

        private void FormWait_HandleCreated(object sender, EventArgs e)
        {
            //m_semaHandleCreated.Release(1);
        }

        private void WaitForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_this.started == true)
                //Отменить закрытие, если установлен признак отображения
                e.Cancel = true;
            else
                ;
            Console.WriteLine(@"FormWait::WaitForm_FormClosing (started=" + started.ToString() + @") - ...");
        }

        private void WaitForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            //m_semaFormClosed.Release(1);
        }
    }
}