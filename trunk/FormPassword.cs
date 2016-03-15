using System;
using System.Collections.Generic;
//using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace HClassLibrary
{
    public partial class FormPassword : FormPasswordBase
    {
        public FormPassword(Passwords p)
            : base(p)
        {
            InitializeComponent();
        }

        public void SetIdPass(int idListener, int id_ext, Passwords.INDEX_ROLES indx_role)
        {
            SetIdPass(id_ext, indx_role);

            m_pass.SetIdListener(idListener);

            labelOwnerPassword.Text = Passwords.getOwnerPass((int)m_indexRolePassword);
        }

        private void tbxPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                delegateStartWait();
                //StartWait();
                Errors errRes = m_pass.ComparePassword(tbxPassword.Text, (uint)m_idExtPassword, (uint)m_indexRolePassword);
                delegateStopWait();
                //StopWait ();
                tbxPassword.Clear ();
                //StopWait();
                switch (errRes)
                {
                    case Errors.NoAccess:
                        this.DialogResult = DialogResult.Abort;
                        closing = true;
                        Close();
                        break;
                    case Errors.NoSet:
                        if (MessageBox.Show(this, "Хотите установить?", "Ошибка", MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.Yes)
                        {
                            this.DialogResult = DialogResult.Retry;
                            closing = true;
                            Close();
                        }
                        else
                        {
                        }
                        break;
                    case Errors.InvalidValue:
                        if (MessageBox.Show(this, "Хотите попробовать снова?", "Ошибка", MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.Yes)
                        {
                        }
                        else
                        {
                            this.DialogResult = DialogResult.No;
                            closing = true;
                            Close();
                        }
                        break;
                    case Errors.ParseError:
                    case Errors.NoError:
                        this.DialogResult = DialogResult.Yes;
                        closing = true;
                        Close();
                        break;
                    default:
                        break;
                }
            }
        }

        protected override void FormPasswordBase_FormClosing(object sender, FormClosingEventArgs e)
        {
            base.FormPasswordBase_FormClosing (sender, e);
        }

        protected override void bntOk_Click(object sender, EventArgs e)
        {
            KeyEventArgs ee = new KeyEventArgs(Keys.Enter);
            tbxPassword_KeyDown(sender, ee);
        }

        protected override void btnCancel_Click(object sender, EventArgs e)
        {
            tbxPassword.Text = "";
            this.DialogResult = DialogResult.No;
            
            base.btnCancel_Click (sender, e);
        }

        protected override void FormPasswordBase_Shown(object sender, EventArgs e)
        {
            tbxPassword.Focus();
        }
    }
}