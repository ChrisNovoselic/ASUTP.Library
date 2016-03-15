using System;
using System.Collections.Generic;
//using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace HClassLibrary
{
    public abstract partial class FormPasswordBase : FormMainBase
    {
        protected int m_idExtPassword;
        protected Passwords.INDEX_ROLES m_indexRolePassword;

        protected Passwords m_pass;
        protected bool closing;

        public FormPasswordBase(Passwords p)
        {
            InitializeComponent();

            m_pass = p;
            closing = false;
        }

        //public void SetDelegateWait (DelegateFunc start, DelegateFunc stop, DelegateFunc ev)
        //{
        //    delegateStartWait = start;
        //    delegateStopWait = stop;
        //}

        protected void SetIdPass(int id_ext, Passwords.INDEX_ROLES indx_role)
        {
            m_idExtPassword = id_ext;
            m_indexRolePassword = indx_role;
        }

        public Passwords.INDEX_ROLES GetIdRolePassword() { return m_indexRolePassword; }

        protected virtual void FormPasswordBase_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!closing)
                e.Cancel = true;
            else
                closing = false;
        }

        protected abstract void bntOk_Click(object sender, EventArgs e);

        protected virtual void btnCancel_Click(object sender, EventArgs e)
        {
            closing = true;
            Close();
        }

        protected abstract void FormPasswordBase_Shown(object sender, EventArgs e);
    }
}