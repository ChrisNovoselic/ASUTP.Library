using ASUTP.Helper;
using System;
using System.Collections.Generic;
//using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace ASUTP.Forms {
    /// <summary>
    /// Класс формы базовый для ввода/изменения паролей
    /// </summary>
    public abstract partial class FormPasswordBase : FormMainBase
    {
        /// <summary>
        /// Идентификтор - в общем смысле признак, что действие значения свойства распространяется на группу(роль) пользователей
        /// , в этом конкретном случае свойство=пароль всегда устанавлививается для группы(роли), поэтому всегда = "1"
        /// </summary>
        protected int m_idExtPassword;
        /// <summary>
        /// Индекс группы(роли) пользователей для которой вводится/изменяется пароль
        /// </summary>
        protected Passwords.INDEX_ROLES m_indexRolePassword;
        /// <summary>
        /// Объект для (де)шифрации паролей
        /// </summary>
        protected Passwords m_pass;
        /// <summary>
        /// Признак ??? отображения окна ввода/изменения пароля
        /// </summary>
        protected bool closing;

        /// <summary>
        /// Конструктор - основной (с параметром)
        /// </summary>
        /// <param name="p">Объект для (де)шифрации паролей</param>
        public FormPasswordBase (Passwords p)
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