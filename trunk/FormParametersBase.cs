using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace StatisticCommon
{
    public abstract partial class FormParametersBase : Form
    {
        public System.Windows.Forms.Button btnOk;
        public System.Windows.Forms.Button btnReset;
        public System.Windows.Forms.Button btnCancel;

        public bool mayClose;
        //private DelegateFunc delegateParamsApply;

        public Int16 m_State;

        public FormParametersBase () {
            this.btnOk = new System.Windows.Forms.Button();
            this.btnReset = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnOk
            // 
            //this.btnOk.Location = new System.Drawing.Point(61, 320);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(75, 23);
            this.btnOk.TabIndex = 39;
            this.btnOk.Text = "Применить";
            this.btnOk.UseVisualStyleBackColor = true;
            //this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // btnReset
            // 
            //this.btnReset.Location = new System.Drawing.Point(154, 320);
            this.btnReset.Name = "btnReset";
            this.btnReset.Size = new System.Drawing.Size(75, 23);
            this.btnReset.TabIndex = 40;
            this.btnReset.Text = "Сброс";
            this.btnReset.UseVisualStyleBackColor = true;
            //this.btnReset.Click += new System.EventHandler(this.btnReset_Click);
            // 
            // btnCancel
            // 
            //this.btnCancel.Location = new System.Drawing.Point(247, 320);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 41;
            this.btnCancel.Text = "Отмена";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.buttonCancel_Click);

            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnReset);
            this.Controls.Add(this.btnOk);

            this.KeyUp +=new KeyEventHandler(FormParametersBase_KeyUp);

            m_State = 0;
        }

        public abstract void loadParam();

        public abstract void saveParam();
        
        public void Parameters_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!mayClose)
                e.Cancel = true;
            else
                mayClose = false;
        }

        //protected abstract void btnOk_Click(object sender, EventArgs e);

        public virtual void buttonCancel_Click(object sender, EventArgs e)
        {
            mayClose = true;
            Close();
        }

        private void FormParametersBase_KeyUp(object obj, KeyEventArgs ev)
        {
            if (ev.KeyCode == Keys.Escape) {
                btnCancel.PerformClick ();
            }
            else
                ;
        }
    }
}
