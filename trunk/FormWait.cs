using System;
using System.Collections.Generic;
//using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace HClassLibrary
{
    public partial class FormWait : Form
    {
        private bool started;
        public FormWait()
        {
            InitializeComponent();
            started = false;
        }

        public void StartWaitForm()
        {
            if (!started)
            {
                started = true;
                this.ShowDialog();
            }
        }

        public void StopWaitForm()
        {
            if (started)
            {
                started = false;
                this.Close();
            }
        }

        private void WaitForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (started)
                e.Cancel = true;
        }
    }
}