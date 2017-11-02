using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ASUTP.Control {
    /// <summary>
    /// Подпись, наследуется от 'Label'
    /// </summary>
    partial class HLabel {
        /// <summary>
        /// Требуется переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose (bool disposing)
        {
            if (disposing && (components != null)) {
                components.Dispose ();
            }
            base.Dispose (disposing);
        }

        #region Код, автоматически созданный конструктором компонентов

        /// <summary>
        /// Обязательный метод для поддержки конструктора - не изменяйте
        /// содержимое данного метода при помощи редактора кода.
        /// </summary>
        private void InitializeComponent ()
        {
            components = new System.ComponentModel.Container ();
        }

        #endregion
    }
    /// <summary>
    /// Подпись, наследуется от 'Label'
    /// </summary>
    public partial class HLabel : System.Windows.Forms.Label {
        /// <summary>
        /// Перечисление - типы элементов управления (подписей)
        /// </summary>
        public enum TYPE_HLABEL {
            /// <summary>
            /// Не известный
            /// </summary>
            UNKNOWN = -1
            /// <summary>
            /// Подпись для ТГ
            /// </summary>
            , TG
            /// <summary>
            /// Подпись для итогового значения за компонент станции
            /// </summary>
            , TOTAL
            /// <summary>
            /// Подпись для дублирования итогового значения за компонент станции
            /// </summary>
            , TOTAL_ZOOM
                , COUNT_TYPE_HLABEL };
        /// <summary>
        /// Тип подписи (из перечисления 'TYPE_HLABEL')
        /// </summary>
        public TYPE_HLABEL m_type;

        /// <summary>
        /// Конструктор - основной (с аргументами)
        /// </summary>
        /// <param name="pt">Позиция для размещения</param>
        /// <param name="sz">Размер подписи</param>
        /// <param name="foreColor">Цвет шрифта текста</param>
        /// <param name="backColor">Цвет фона</param>
        /// <param name="szFont">Размер шрифта</param>
        /// <param name="align">Признак для выравнивания текста</param>
        public HLabel (Point pt, Size sz, Color foreColor, Color backColor, Single szFont, System.Drawing.ContentAlignment align)
        {
            InitializeComponent ();

            this.BorderStyle = BorderStyle.Fixed3D;

            if (((pt.X < 0) || (pt.Y < 0)) ||
                ((sz.Width < 0) || (sz.Height < 0)))
                this.Dock = DockStyle.Fill;
            else {
                this.Location = pt;
                this.Size = sz;
            }

            this.ForeColor = foreColor;
            this.BackColor = backColor;

            this.TextAlign = align;

            this.Font = new System.Drawing.Font ("Microsoft Sans Serif", szFont, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));

            //this.Text = text;

            m_type = TYPE_HLABEL.UNKNOWN;
        }

        /// <summary>
        /// Конструктор - дополнительный (с аргументом)
        /// </summary>
        /// <param name="prop">Стиль подписи</param>
        public HLabel (HLabelStyles prop)
            : this (new Point (-1, -1), new Size (-1, -1), prop.m_foreColor, prop.m_backColor, prop.m_szFont, prop.m_align)
        {
        }

        /// <summary>
        /// Конструктор - дополнительный (с аргументами)
        /// </summary>
        /// <param name="container">Родительский контейнер для подписи</param>
        /// <param name="pt">Позиция для размещения</param>
        /// <param name="sz">Размер подписи</param>
        /// <param name="foreColor">Цвет шрифта текста</param>
        /// <param name="backColor">Цвет фона</param>
        /// <param name="szFont">Размер шрифта</param>
        /// <param name="align">Признак для выравнивания текста</param>
        public HLabel (IContainer container, Point pt, Size sz, Color foreColor, Color backColor, Single szFont, System.Drawing.ContentAlignment align)
            : this (pt, sz, foreColor, backColor, szFont, align)
        {
            container.Add (this);
        }

        /// <summary>
        /// Конструктор - основной ()
        /// </summary>
        /// <param name="container">Контейнер родительский для элемента</param>
        /// <param name="prop"></param>
        public HLabel (IContainer container, HLabelStyles prop)
            : this (new Point (-1, -1), new Size (-1, -1), prop.m_foreColor, prop.m_backColor, prop.m_szFont, prop.m_align)
        {
            container.Add (this);
        }

        /// <summary>
        /// Создать объект "Подпись"
        /// </summary>
        /// <param name="name">Текст подписи</param>
        /// <param name="prop">Стиль подриси</param>
        /// <returns>Объект "Подпись"</returns>
        public static System.Windows.Forms.Label createLabel (string name, HLabelStyles prop)
        {
            System.Windows.Forms.Label lblRes = new Label ();
            lblRes.Text = name;
            if (((prop.m_pt.X < 0) && (prop.m_pt.Y < 0)) &&
                ((prop.m_sz.Width < 0) && (prop.m_sz.Height < 0)))
                lblRes.Dock = DockStyle.Fill;
            else {
                lblRes.Location = prop.m_pt;
                if ((prop.m_sz.Width < 0) && (prop.m_sz.Height < 0))
                    lblRes.AutoSize = true;
                else
                    lblRes.Size = prop.m_sz;
            }
            lblRes.BorderStyle = BorderStyle.Fixed3D;
            lblRes.Font = new System.Drawing.Font ("Microsoft Sans Serif", prop.m_szFont, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            lblRes.TextAlign = prop.m_align;
            lblRes.ForeColor = prop.m_foreColor;
            lblRes.BackColor = prop.m_backColor;

            return lblRes;
        }

        /// <summary>
        /// Подобрать размер шрифта для подписи в ~ от ее размеров
        /// </summary>
        /// <param name="g">Контекст для рисованяи</param>
        /// <param name="text">Текст подписи</param>
        /// <param name="szCtrl">Размер элемента управления (??? подписи)</param>
        /// <param name="szMargin">Расстояние от границ элемента до текста</param>
        /// <param name="fSzStep">Шаг при подборе</param>
        /// <returns></returns>
        public static Font FitFont (Graphics g, string text, SizeF szCtrl, SizeF szMargin, float fSzStep)
        {
            Font fontRes = null;
            ;
            SizeF szLimit = new SizeF (szCtrl.Width *= szMargin.Width, szCtrl.Height *= szMargin.Height)
                , szAttempt;
            float fHeight
                , fHeightMin = szLimit.Height * 0.25F
                , fHeightMax = szLimit.Height;

            //ctrl.Height * 0.29F
            //for (fSz = fSzMin; fSz < fSzMax; fSz += fSzStep)
            for (fHeight = fHeightMax; fHeight > fHeightMin; fHeight -= fSzStep) {
                fontRes = new System.Drawing.Font ("Microsoft Sans Serif", fHeight, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
                szAttempt = g.MeasureString (text, fontRes);

                if ((!(szAttempt.Height > szLimit.Height))
                    && (!(szAttempt.Width > szLimit.Width)))
                    break;
                else
                    ;
            }

            return fontRes;
        }
    }
    /// <summary>
    /// Стиль для подписи
    /// </summary>
    public class HLabelStyles {
        /// <summary>
        /// Цвета для шрифта(текст), фона
        /// </summary>
        public Color m_foreColor,
                    m_backColor;
        /// <summary>
        /// Признак размещения(выравнивания текста) содержания подписи
        /// </summary>
        public System.Drawing.ContentAlignment m_align;
        /// <summary>
        /// Размер шрифта
        /// </summary>
        public Single m_szFont;
        /// <summary>
        /// Точка/позиция для размещения
        /// </summary>
        public Point m_pt;
        /// <summary>
        /// Размер элемента
        /// </summary>
        public Size m_sz;

        /// <summary>
        /// Конструктор - основной (с аргументами)
        /// </summary>
        /// <param name="foreColor">Цвет шрифта текста</param>
        /// <param name="backColor">Цвет фона</param>
        /// <param name="szFont">Размер шрифта</param>
        /// <param name="align">Признак размещения(выравнивания текста) содержания подписи</param>
        public HLabelStyles (Color foreColor, Color backColor, Single szFont, System.Drawing.ContentAlignment align)
            : this (new Point (-1, -1), new Size (-1, -1), foreColor, backColor, szFont, align)
        {
        }

        /// <summary>
        /// Конструктор - дополнительный (с аргументами)
        /// </summary>
        /// <param name="pt">Точка/позиция для размещения</param>
        /// <param name="sz">Размер элемента</param>
        /// <param name="foreColor">Цвет шрифта текста</param>
        /// <param name="backColor">Цвет фона</param>
        /// <param name="szFont">Размер шрифта</param>
        /// <param name="align">Признак размещения(выравнивания текста) содержания подписи</param>
        public HLabelStyles (Point pt, Size sz, Color foreColor, Color backColor, Single szFont, System.Drawing.ContentAlignment align)
        {
            m_pt = pt;
            m_sz = sz;

            m_foreColor = foreColor;
            m_backColor = backColor;
            m_szFont = szFont;
            m_align = align;
        }
    };
}
