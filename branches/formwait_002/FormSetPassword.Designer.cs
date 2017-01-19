namespace HClassLibrary
{
    partial class FormSetPassword
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
            this.lblNewPassword = new System.Windows.Forms.Label();
            this.tbxNewPassword = new System.Windows.Forms.TextBox();
            this.lblNewPasswordAgain = new System.Windows.Forms.Label();
            this.tbxNewPasswordAgain = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // lblNewPassword
            // 
            this.lblNewPassword.AutoSize = true;
            this.lblNewPassword.Location = new System.Drawing.Point(85, 0);
            this.lblNewPassword.Name = "lblNewPassword";
            this.lblNewPassword.Size = new System.Drawing.Size(123, 13);
            this.lblNewPassword.TabIndex = 4;
            this.lblNewPassword.Text = "Введите новый пароль";
            // 
            // tbxNewPassword
            // 
            this.tbxNewPassword.Location = new System.Drawing.Point(3, 20);
            this.tbxNewPassword.MaxLength = 20;
            this.tbxNewPassword.Name = "tbxNewPassword";
            this.tbxNewPassword.PasswordChar = '#';
            this.tbxNewPassword.Size = new System.Drawing.Size(286, 20);
            this.tbxNewPassword.TabIndex = 0;
            // 
            // lblNewPasswordAgain
            // 
            this.lblNewPasswordAgain.AutoSize = true;
            this.lblNewPasswordAgain.Location = new System.Drawing.Point(79, 42);
            this.lblNewPasswordAgain.Name = "lblNewPasswordAgain";
            this.lblNewPasswordAgain.Size = new System.Drawing.Size(135, 13);
            this.lblNewPasswordAgain.TabIndex = 5;
            this.lblNewPasswordAgain.Text = "Повторите новый пароль";
            // 
            // tbxNewPasswordAgain
            // 
            this.tbxNewPasswordAgain.Location = new System.Drawing.Point(3, 62);
            this.tbxNewPasswordAgain.MaxLength = 20;
            this.tbxNewPasswordAgain.Name = "tbxNewPasswordAgain";
            this.tbxNewPasswordAgain.PasswordChar = '#';
            this.tbxNewPasswordAgain.Size = new System.Drawing.Size(286, 20);
            this.tbxNewPasswordAgain.TabIndex = 1;
            this.tbxNewPasswordAgain.KeyDown += new System.Windows.Forms.KeyEventHandler(this.tbxNewPasswordAgain_KeyDown);
            // 
            // btnOk
            // 
            this.btnOk.Location = new System.Drawing.Point(68, 91);
            // 
            // параметрыПриложения
            // 
            this.параметрыПриложения.Location = new System.Drawing.Point(149, 91);
            // 
            // FormSetPassword
            // 
            //this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            //this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            //this.CancelButton = this.параметрыПриложения;
            this.ClientSize = new System.Drawing.Size(292, 117);
            //this.Controls.Add(this.параметрыПриложения);
            //this.Controls.Add(this.btnOk);
            this.Controls.Add(this.lblNewPasswordAgain);
            this.Controls.Add(this.tbxNewPasswordAgain);
            this.Controls.Add(this.lblNewPassword);
            this.Controls.Add(this.tbxNewPassword);
            //this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            //this.MaximizeBox = false;
            this.Name = "FormSetPassword";
            //this.ShowIcon = false;
            //this.ShowInTaskbar = false;
            //this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            //this.Shown += new System.EventHandler(this.SetPassword_Shown);
            //this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SetPassword_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblNewPassword;
        private System.Windows.Forms.TextBox tbxNewPassword;
        private System.Windows.Forms.Label lblNewPasswordAgain;
        private System.Windows.Forms.TextBox tbxNewPasswordAgain;
    }
}