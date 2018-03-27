using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace ASUTP.Forms {
    /// <summary>
    /// Класс базовый для форм с отображением значений параметров
    /// </summary>
    public abstract partial class FormParametersBase : Form
    {
        /// <summary>
        /// Элемент граф./интерфейса - кнопка "Применить"
        /// </summary>
        public System.Windows.Forms.Button btnOk;
        /// <summary>
        /// Элемент граф./интерфейса - кнопка "Сборс"
        /// </summary>
        public System.Windows.Forms.Button btnReset;
        /// <summary>
        /// Элемент граф./интерфейса - кнопка "Отмена"
        /// </summary>
        public System.Windows.Forms.Button btnCancel;
        /// <summary>
        /// Признак возможности снятия с отображения окна
        /// </summary>
        public bool mayClose;
        
        //private DelegateFunc delegateParamsApply;
        /// <summary>
        /// Признак 
        /// </summary>
        public Int16 m_State;

        /// <summary>
        /// Конструктор - основной (без арнументов)
        /// </summary>
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

        /// <summary>
        /// Обновить/прочитать значения
        /// </summary>
        /// <param name="err"></param>
        public abstract void Update (out int err);

        /// <summary>
        /// Загрузить значения
        /// </summary>
        /// <param name="bInit"></param>
        protected abstract void loadParam(bool bInit);

        /// <summary>
        /// Сохранить значения
        /// </summary>
        protected abstract void saveParam();

        /// <summary>
        /// Обработчик события - перед закрытие формы
        /// </summary>
        /// <param name="sender">Объект, инициировавший событие</param>
        /// <param name="e">Аргумент события</param>
        protected void Parameters_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!mayClose)
                e.Cancel = true;
            else
                mayClose = false;
        }

        //protected abstract void btnOk_Click(object sender, EventArgs e);

        /// <summary>
        /// Обработчик события - нажатие кнопки "Отмена"
        /// </summary>
        /// <param name="sender">Объект, инициировавший событие</param>
        /// <param name="e">Аргумент события</param>
        protected virtual void buttonCancel_Click(object sender, EventArgs e)
        {
            mayClose = true;
            Close();
        }

        /// <summary>
        /// Обработчик события - освобождение после нажатия клавиши на клавиатуре
        /// </summary>
        /// <param name="obj">Объект, инициировавший событие</param>
        /// <param name="ev">Аргумент события(информация о клавише на клавиатуре)</param>
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
