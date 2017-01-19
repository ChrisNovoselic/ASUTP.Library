namespace HClassLibrary
{
    partial class FormConnectionSettings
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
            this.cbxConnFor = new System.Windows.Forms.ComboBox();
            this.tbxServer = new System.Windows.Forms.TextBox();
            //this.tbxInstance = new System.Windows.Forms.TextBox();
            this.tbxDataBase = new System.Windows.Forms.TextBox();
            this.tbxUserId = new System.Windows.Forms.TextBox();
            this.mtbxPass = new System.Windows.Forms.MaskedTextBox();
            this.lblConnFor = new System.Windows.Forms.Label();
            this.lblServer = new System.Windows.Forms.Label();
            //this.lblInstance = new System.Windows.Forms.Label();
            this.lblDataBase = new System.Windows.Forms.Label();
            this.lblUserId = new System.Windows.Forms.Label();
            this.lblPass = new System.Windows.Forms.Label();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.nudnPort = new System.Windows.Forms.NumericUpDown();
            this.lblPort = new System.Windows.Forms.Label();
            //this.cbxIgnore = new System.Windows.Forms.CheckBox();
            //this.lblIgnore = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.nudnPort)).BeginInit();
            this.SuspendLayout();
            // 
            // cbxConnFor
            // 
            this.cbxConnFor.Enabled = false;
            this.cbxConnFor.FormattingEnabled = true;
            this.cbxConnFor.Location = new System.Drawing.Point(130, 16);
            this.cbxConnFor.Name = "cbxConnFor";
            this.cbxConnFor.Size = new System.Drawing.Size(189, 21);
            this.cbxConnFor.TabIndex = 0;
            this.cbxConnFor.SelectedIndexChanged += new System.EventHandler(this.cbxConnFor_SelectedIndexChanged);
            // 
            // tbxServer
            // 
            this.tbxServer.Location = new System.Drawing.Point(130, 43);
            this.tbxServer.Name = "tbxServer";
            this.tbxServer.Size = new System.Drawing.Size(189, 20);
            this.tbxServer.TabIndex = 1;
            this.tbxServer.TextChanged += new System.EventHandler(this.component_Changed);
            // 
            // tbxDataBase
            // 
            this.tbxDataBase.Location = new System.Drawing.Point(130, 97);
            this.tbxDataBase.Name = "tbxDataBase";
            this.tbxDataBase.Size = new System.Drawing.Size(189, 20);
            this.tbxDataBase.TabIndex = 3;
            this.tbxDataBase.TextChanged += new System.EventHandler(this.component_Changed);
            // 
            // tbxUserId
            // 
            this.tbxUserId.Location = new System.Drawing.Point(130, 123);
            this.tbxUserId.Name = "tbxUserId";
            this.tbxUserId.Size = new System.Drawing.Size(189, 20);
            this.tbxUserId.TabIndex = 4;
            this.tbxUserId.TextChanged += new System.EventHandler(this.component_Changed);
            // 
            // mtbxPass
            // 
            this.mtbxPass.Location = new System.Drawing.Point(130, 149);
            this.mtbxPass.Name = "mtbxPass";
            this.mtbxPass.PasswordChar = '#';
            this.mtbxPass.Size = new System.Drawing.Size(189, 20);
            this.mtbxPass.TabIndex = 5;
            this.mtbxPass.TextChanged += new System.EventHandler(this.component_Changed);
            // 
            // lblConnFor
            // 
            this.lblConnFor.AutoSize = true;
            this.lblConnFor.Enabled = false;
            this.lblConnFor.Location = new System.Drawing.Point(12, 19);
            this.lblConnFor.Name = "lblConnFor";
            this.lblConnFor.Size = new System.Drawing.Size(89, 13);
            this.lblConnFor.TabIndex = 9;
            this.lblConnFor.Text = "Соединение для";
            // 
            // lblServer
            // 
            this.lblServer.AutoSize = true;
            this.lblServer.Location = new System.Drawing.Point(12, 46);
            this.lblServer.Name = "lblServer";
            this.lblServer.Size = new System.Drawing.Size(95, 13);
            this.lblServer.TabIndex = 10;
            this.lblServer.Text = "IP адрес сервера";
            // 
            // lblDataBase
            // 
            this.lblDataBase.AutoSize = true;
            this.lblDataBase.Location = new System.Drawing.Point(12, 100);
            this.lblDataBase.Name = "lblDataBase";
            this.lblDataBase.Size = new System.Drawing.Size(98, 13);
            this.lblDataBase.TabIndex = 12;
            this.lblDataBase.Text = "Имя базы данных";
            // 
            // lblUserId
            // 
            this.lblUserId.AutoSize = true;
            this.lblUserId.Location = new System.Drawing.Point(12, 126);
            this.lblUserId.Name = "lblUserId";
            this.lblUserId.Size = new System.Drawing.Size(103, 13);
            this.lblUserId.TabIndex = 13;
            this.lblUserId.Text = "Имя пользователя";
            // 
            // lblPass
            // 
            this.lblPass.AutoSize = true;
            this.lblPass.Location = new System.Drawing.Point(12, 152);
            this.lblPass.Name = "lblPass";
            this.lblPass.Size = new System.Drawing.Size(45, 13);
            this.lblPass.TabIndex = 14;
            this.lblPass.Text = "Пароль";
            // 
            // btnOk
            // 
            this.btnOk.Location = new System.Drawing.Point(89, 178);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(75, 23);
            this.btnOk.TabIndex = 7;
            this.btnOk.Text = "Ок";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // параметрыПриложения
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(170, 178);
            this.btnCancel.Name = "параметрыПриложения";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 8;
            this.btnCancel.Text = "Отмена";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // nudnPort
            // 
            this.nudnPort.Location = new System.Drawing.Point(130, 71);
            this.nudnPort.Maximum = 65365; // new decimal(new int[] { 65365, 0, 0, 0 });
            this.nudnPort.Minimum = -65364; // new decimal(new int[] { -65365, 0, 0, 0 });
            this.nudnPort.Name = "nudnPort";
            this.nudnPort.Size = new System.Drawing.Size(69, 20);
            this.nudnPort.TabIndex = 2;
            this.nudnPort.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.nudnPort.Value = new decimal(new int[] {
            1433,
            0,
            0,
            0});
            this.nudnPort.ValueChanged += new System.EventHandler(this.component_Changed);
            // 
            // lblPort
            // 
            this.lblPort.AutoSize = true;
            this.lblPort.Location = new System.Drawing.Point(13, 73);
            this.lblPort.Name = "lblPort";
            this.lblPort.Size = new System.Drawing.Size(32, 13);
            this.lblPort.TabIndex = 11;
            this.lblPort.Text = "Порт";
            //// 
            //// cbxIgnore
            //// 
            //this.cbxIgnore.AutoSize = true;
            //this.cbxIgnore.Location = new System.Drawing.Point(130, 180);
            //this.cbxIgnore.Name = "cbxIgnore";
            //this.cbxIgnore.Size = new System.Drawing.Size(15, 14);
            //this.cbxIgnore.TabIndex = 6;
            //this.cbxIgnore.UseVisualStyleBackColor = true;
            //// 
            //// lblIgnore
            //// 
            //this.lblIgnore.AutoSize = true;
            //this.lblIgnore.Location = new System.Drawing.Point(12, 180);
            //this.lblIgnore.Name = "lblIgnore";
            //this.lblIgnore.Size = new System.Drawing.Size(79, 13);
            //this.lblIgnore.TabIndex = 15;
            //this.lblIgnore.Text = "Игнорировать";
            // 
            // FormConnectionSettings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(334, 208);
            //this.Controls.Add(this.lblIgnore);
            //this.Controls.Add(this.cbxIgnore);
            this.Controls.Add(this.lblPort);
            this.Controls.Add(this.nudnPort);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.lblPass);
            this.Controls.Add(this.lblUserId);
            this.Controls.Add(this.lblDataBase);
            this.Controls.Add(this.lblServer);
            this.Controls.Add(this.lblConnFor);
            this.Controls.Add(this.mtbxPass);
            this.Controls.Add(this.tbxUserId);
            this.Controls.Add(this.tbxDataBase);
            this.Controls.Add(this.tbxServer);
            this.Controls.Add(this.cbxConnFor);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "FormConnectionSettings";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ConnectionSettings_FormClosing);
            this.Shown += new System.EventHandler(this.FormConnectionSettings_Shown);
            ((System.ComponentModel.ISupportInitialize)(this.nudnPort)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ComboBox cbxConnFor;
        private System.Windows.Forms.TextBox tbxServer;
        //private System.Windows.Forms.TextBox tbxInstance;
        private System.Windows.Forms.TextBox tbxDataBase;
        private System.Windows.Forms.TextBox tbxUserId;
        private System.Windows.Forms.MaskedTextBox mtbxPass;
        private System.Windows.Forms.Label lblConnFor;
        private System.Windows.Forms.Label lblServer;
        //private System.Windows.Forms.Label lblInstance;
        private System.Windows.Forms.Label lblDataBase;
        private System.Windows.Forms.Label lblUserId;
        private System.Windows.Forms.Label lblPass;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.NumericUpDown nudnPort;
        private System.Windows.Forms.Label lblPort;
        //private System.Windows.Forms.CheckBox cbxIgnore;
        //private System.Windows.Forms.Label lblIgnore;
    }
}