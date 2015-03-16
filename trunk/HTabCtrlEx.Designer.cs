using System.ComponentModel;
using System.Collections.Generic;
using System.Drawing;

namespace HClassLibrary
{
    partial class HTabCtrlEx
    {
        /// <summary>
        /// Требуется переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором компонентов

        /// <summary>
        /// Обязательный метод для поддержки конструктора - не изменяйте
        /// содержимое данного метода при помощи редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            SetStyle(System.Windows.Forms.ControlStyles.DoubleBuffer, true);
            this.TabStop = false;
            this.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            //this.tclTecViews.ItemSize = new System.Drawing.Size(230, 24);

            this.TabStop = false;

            m_listTypeTabs = new List<TYPE_TAB>();

            m_arBitmap = new Icon[] { HClassLibrary.Properties.Resources.floatNonActive, HClassLibrary.Properties.Resources.floatInActive
                                    , HClassLibrary.Properties.Resources.closeNonActive, HClassLibrary.Properties.Resources.closeInActive };
        }

        #endregion

        private class HTabPageEx : System.Windows.Forms.TabPage
        {
            public HTabPageEx()
            {
                InitializeComponent();
            }

            public HTabPageEx(IContainer container)
            {
                container.Add(this);

                InitializeComponent();
            }

            /// <summary>
            /// Требуется переменная конструктора.
            /// </summary>
            private System.ComponentModel.IContainer components = null;

            /// <summary> 
            /// Освободить все используемые ресурсы.
            /// </summary>
            /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
            protected override void Dispose(bool disposing)
            {
                if (disposing && (components != null))
                {
                    components.Dispose();
                }
                base.Dispose(disposing);
            }

            #region Код, автоматически созданный конструктором компонентов

            /// <summary>
            /// Обязательный метод для поддержки конструктора - не изменяйте
            /// содержимое данного метода при помощи редактора кода.
            /// </summary>
            private void InitializeComponent()
            {
                components = new System.ComponentModel.Container();
            }

            #endregion
        }
    }
}
