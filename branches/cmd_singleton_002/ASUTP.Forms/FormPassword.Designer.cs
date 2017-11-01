namespace ASUTP.Forms {
    partial class FormPassword
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.tbxPassword = new System.Windows.Forms.TextBox();
            this.lblPassword = new System.Windows.Forms.Label();
            //this.btnOk = new System.Windows.Forms.Button();
            //this.параметрыѕриложени€ = new System.Windows.Forms.Button();
            this.labelOwnerPassword = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // tbxPassword
            // 
            this.tbxPassword.Location = new System.Drawing.Point(3, 23);
            this.tbxPassword.MaxLength = 20;
            this.tbxPassword.Name = "tbxPassword";
            this.tbxPassword.PasswordChar = '#';
            this.tbxPassword.Size = new System.Drawing.Size(286, 20);
            this.tbxPassword.TabIndex = 0;
            this.tbxPassword.KeyDown += new System.Windows.Forms.KeyEventHandler(this.tbxPassword_KeyDown);
            // 
            // lblPassword
            // 
            this.lblPassword.AutoSize = true;
            this.lblPassword.Location = new System.Drawing.Point(30, 3);
            this.lblPassword.Name = "lblPassword";
            this.lblPassword.Size = new System.Drawing.Size(94, 13);
            this.lblPassword.TabIndex = 3;
            this.lblPassword.Text = "¬ведите пароль: ";
            // 
            // btnOk
            // 
            this.btnOk.Location = new System.Drawing.Point(68, 49);
            // 
            // параметрыѕриложени€
            // 
            this.параметрыѕриложени€.Location = new System.Drawing.Point(149, 49);
            // 
            // labelOwnerPassword
            // 
            this.labelOwnerPassword.AutoSize = true;
            this.labelOwnerPassword.Location = new System.Drawing.Point(125, 3);
            this.labelOwnerPassword.Name = "labelOwnerPassword";
            this.labelOwnerPassword.Size = new System.Drawing.Size(0, 13);
            this.labelOwnerPassword.TabIndex = 4;
            // 
            // FormPassword
            // 
            //this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            //this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(292, 77);
            this.Controls.Add(this.labelOwnerPassword);
            //this.Controls.Add(this.параметрыѕриложени€);
            //this.Controls.Add(this.btnOk);
            this.Controls.Add(this.lblPassword);
            this.Controls.Add(this.tbxPassword);
            //this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            //this.MaximizeBox = false;
            this.Name = "FormPassword";
            //this.ShowIcon = false;
            //this.ShowInTaskbar = false;
            //this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            //this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Password_FormClosing);
            //this.Shown += new System.EventHandler(this.Password_Shown);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox tbxPassword;
        private System.Windows.Forms.Label lblPassword;
        private System.Windows.Forms.Label labelOwnerPassword;
    }
}