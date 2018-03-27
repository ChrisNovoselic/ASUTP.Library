using ASUTP.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles; //PushButtonState

namespace ASUTP.Control {
    /// <summary>
    /// Ячейка представления "текстовое поле" с возможностью маскирования(скрытия) вводимой информации
    /// </summary>
    public class DataGridViewPasswordTextBoxCell : DataGridViewTextBoxCell {
        private TextBox _textBox;

        /// <summary>
        /// Конструктор - основной (без аргументов)
        /// </summary>
        public DataGridViewPasswordTextBoxCell ()
            : base ()
        {
            _textBox = new TextBox ();
            _textBox.PasswordChar = '#';
            _textBox.BorderStyle = BorderStyle.None;
        }

        public override void InitializeEditingControl (int rowIndex, object initialFormattedValue, DataGridViewCellStyle dataGridViewCellStyle)
        {
            // Set the value of the editing control to the current cell value.
            base.InitializeEditingControl (rowIndex, initialFormattedValue, dataGridViewCellStyle);
            //TextBoxEditingControl ctrl = DataGridView.EditingControl as TextBoxEditingControl;
            //ctrl.Text = this.Value as string;
        }

        /// <summary>
        /// Обработчик события(переопределенный) - "прорисовка" объекта
        /// </summary>
        /// <param name="graphics">Контекст устройства для рисования</param>
        /// <param name="clipBounds">Область для рисования</param>
        /// <param name="cellBounds">Область для рисования</param>
        /// <param name="rowIndex">Индекс строки</param>
        /// <param name="cellState">Состояние ячейки</param>
        /// <param name="value">Значение ячейки</param>
        /// <param name="formattedValue">Правила для форматирования ???текста</param>
        /// <param name="errorText">Строка сообщения при ошибке ввода</param>
        /// <param name="cellStyle">Стиль для отображения ячейки</param>
        /// <param name="advancedBorderStyle">Дополнительные стили для отображения границ ячейки</param>
        /// <param name="paintParts">Дополн. стили для отдельных частей элемента</param>
        protected override void Paint (System.Drawing.Graphics graphics
            , System.Drawing.Rectangle clipBounds
            , System.Drawing.Rectangle cellBounds, int rowIndex
            , DataGridViewElementStates cellState
            , object value
            , object formattedValue
            , string errorText
            , DataGridViewCellStyle cellStyle
            , DataGridViewAdvancedBorderStyle advancedBorderStyle
            , DataGridViewPaintParts paintParts)
        {
            //throw new Exception("The method or operation is not implemented.");
            base.Paint (graphics, clipBounds, cellBounds, rowIndex, cellState, value, formattedValue, errorText, cellStyle, advancedBorderStyle, paintParts);

            //_textBox.Size = cellBounds.Size;
            _textBox.Size = new Size (cellBounds.Size.Width, cellBounds.Size.Height + 4);
            //_textBox.Location = cellBounds.Location;
            _textBox.Location = new Point (cellBounds.Location.X, cellBounds.Location.Y + 4);
            this.DataGridView.Controls.Add (_textBox);
        }

        /// <summary>
        /// ??? Свойство - объект ячейки
        /// </summary>
        public TextBox GetControl
        {
            get
            {
                return _textBox;
            }
        }

        /// <summary>
        /// Свойство(переопределенное) - тип объекта в ячейке представления
        /// </summary>
        public override Type EditType
        {
            get
            {
                // Return the type of the editing contol that CalendarCell uses.
                return
                    typeof (
                        //TextBoxEditingControl
                        TextBox
                    );
            }
        }

        /// <summary>
        /// Свойство(переопределенное) - тип значения в ячейке представления
        /// </summary>
        public override Type ValueType
        {
            get
            {
                // Return the type of the value that CalendarCell contains.
                return typeof (DateTime);
            }
        }

        /// <summary>
        /// Свойство(переопределенное) - значение по умолчанию в ячейке представления
        /// </summary>
        public override object DefaultNewRowValue
        {
            get
            {
                // Use the default value.
                return string.Empty;
            }
        }

        /// <summary>
        /// Установить значение, связанное с ячейкой
        /// </summary>
        public void SetValue ()
        {
            _textBox.Text = Value.ToString ();
        }
    }
    /// <summary>
    /// Ячейка представления "кнопка" с возможностью включения/отключения
    /// </summary>
    public class DataGridViewDisableButtonCell : DataGridViewButtonCell {
        private bool enabledValue;
        /// <summary>
        /// Признак вкл./выкл. элемент граф./интерфейса
        /// </summary>
        public bool Enabled
        {
            get
            {
                return enabledValue;
            }

            set
            {
                enabledValue = value;
            }
        }

        /// <summary>
        /// Override the Clone method so that the Enabled property is copied.
        /// </summary>
        /// <returns>Копия объекта</returns>
        public override object Clone ()
        {
            DataGridViewDisableButtonCell cell = (DataGridViewDisableButtonCell)base.Clone ();

            cell.Enabled = this.Enabled;

            return cell;
        }

        /// <summary>
        /// By default, enable the button cell.
        /// </summary>
        public DataGridViewDisableButtonCell ()
        {
            this.enabledValue = true;
        }

        /// <summary>
        /// Обработчик события(переопределенный) - "прорисовка" объекта
        /// </summary>
        /// <param name="graphics">Контекст устройства для рисования</param>
        /// <param name="clipBounds">Область для рисования</param>
        /// <param name="cellBounds">Область для рисования</param>
        /// <param name="rowIndex">Индекс строки</param>
        /// <param name="elementState">Состояние ячейки</param>
        /// <param name="value">Значение ячейки</param>
        /// <param name="formattedValue">Правила для форматирования ???текста</param>
        /// <param name="errorText">Строка сообщения при ошибке ввода</param>
        /// <param name="cellStyle">Стиль для отображения ячейки</param>
        /// <param name="advancedBorderStyle">Дополнительные стили для отображения границ ячейки</param>
        /// <param name="paintParts">Дополн. стили для отдельных частей элемента</param>
        protected override void Paint (Graphics graphics,
                                    Rectangle clipBounds, Rectangle cellBounds, int rowIndex,
                                    DataGridViewElementStates elementState, object value,
                                    object formattedValue, string errorText,
                                    DataGridViewCellStyle cellStyle,
                                    DataGridViewAdvancedBorderStyle advancedBorderStyle,
                                    DataGridViewPaintParts paintParts)
        {
            // The button cell is disabled, so paint the border,  
            // background, and disabled button for the cell.
            if (!this.enabledValue) {
                // Draw the cell background, if specified.
                if ((paintParts & DataGridViewPaintParts.Background) == DataGridViewPaintParts.Background) {
                    SolidBrush cellBackground = new SolidBrush (cellStyle.BackColor);

                    graphics.FillRectangle (cellBackground, cellBounds);

                    cellBackground.Dispose ();
                }

                // Draw the cell borders, if specified.
                if ((paintParts & DataGridViewPaintParts.Border) == DataGridViewPaintParts.Border) {
                    PaintBorder (graphics, clipBounds, cellBounds, cellStyle, advancedBorderStyle);
                } else
                    ;

                // Calculate the area in which to draw the button.
                Rectangle buttonArea = cellBounds;
                Rectangle buttonAdjustment = this.BorderWidths (advancedBorderStyle);

                buttonArea.X += buttonAdjustment.X;
                buttonArea.Y += buttonAdjustment.Y;

                buttonArea.Height -= buttonAdjustment.Height;
                buttonArea.Width -= buttonAdjustment.Width;

                // Draw the disabled button.                
                ButtonRenderer.DrawButton (graphics, buttonArea, PushButtonState.Disabled);

                // Draw the disabled button text. 
                if (this.FormattedValue is String) {
                    TextRenderer.DrawText (graphics, (string)this.FormattedValue, this.DataGridView.Font, buttonArea, SystemColors.GrayText);
                } else
                    ;
            } else {
                // The button cell is enabled, so let the base class 
                // handle the painting.
                base.Paint (graphics, clipBounds, cellBounds, rowIndex, elementState, value, formattedValue, errorText, cellStyle, advancedBorderStyle, paintParts);
            }
        }

        //protected override void OnClick(DataGridViewCellEventArgs e)
        //{
        //    if (Enabled == true)
        //        base.OnClick(e);
        //    else
        //        ;
        //}
    }
    /// <summary>
    /// Столбец для представления с типом ячеек "кнопка", с возможностью включения/отключения
    /// </summary>
    public class DataGridViewDisableButtonColumn : DataGridViewButtonColumn {
        /// <summary>
        /// Конструктор - основной (без аргументов)
        /// </summary>
        public DataGridViewDisableButtonColumn ()
        {
            this.CellTemplate = new DataGridViewDisableButtonCell ();
        }
    }
    /// <summary>
    /// Ячейка представления "кнопка" с возможностью фиксации состояния "нажата"
    /// </summary>
    public class DataGridViewPressedButtonCell : DataGridViewButtonCell {
        /// <summary>
        /// Признак состояния нажата/не_нажата
        /// </summary>
        private bool pressedValue;
        /// <summary>
        /// Признак состояния нажата/не_нажата
        /// </summary>
        public bool Pressed
        {
            get
            {
                return pressedValue;
            }

            set
            {
                if (!(pressedValue == value)) {
                    Value = value == true ? @"<-" : @"->";

                    delegatePressChanged?.Invoke (RowIndex);

                    pressedValue = value;
                } else
                    ;
            }
        }
        /// <summary>
        /// Override the Clone method so that the Enabled property is copied.
        /// </summary>
        /// <returns>Копия ячейки</returns>
        public override object Clone ()
        {
            DataGridViewPressedButtonCell cell = (DataGridViewPressedButtonCell)base.Clone ();

            cell.Pressed = this.Pressed;
            cell.delegatePressChanged = this.delegatePressChanged;

            return cell;
        }

        /// <summary>
        /// By default, not pressed the button cell
        /// </summary>
        public DataGridViewPressedButtonCell ()
        {
            this.pressedValue = false;
        }

        /// <summary>
        /// Обработчик события(переопределенный) - "прорисовка" объекта
        /// </summary>
        /// <param name="graphics">Контекст устройства для рисования</param>
        /// <param name="clipBounds">Область для рисования</param>
        /// <param name="cellBounds">Область для рисования</param>
        /// <param name="rowIndex">Индекс строки</param>
        /// <param name="elementState">Состояние ячейки</param>
        /// <param name="value">Значение ячейки</param>
        /// <param name="formattedValue">Правила для форматирования ???текста</param>
        /// <param name="errorText">Строка сообщения при ошибке ввода</param>
        /// <param name="cellStyle">Стиль для отображения ячейки</param>
        /// <param name="advancedBorderStyle">Дополнительные стили для отображения границ ячейки</param>
        /// <param name="paintParts">Дополн. стили для отдельных частей элемента</param>
        protected override void Paint (Graphics graphics
            , Rectangle clipBounds
            , Rectangle cellBounds
            , int rowIndex
            , DataGridViewElementStates elementState
            , object value
            , object formattedValue
            , string errorText
            , DataGridViewCellStyle cellStyle
            , DataGridViewAdvancedBorderStyle advancedBorderStyle
            , DataGridViewPaintParts paintParts)
        {
            // The button cell is disabled, so paint the border,  
            // background, and disabled button for the cell.
            if (this.pressedValue) {
                // Draw the cell background, if specified.
                if ((paintParts & DataGridViewPaintParts.Background) == DataGridViewPaintParts.Background) {
                    SolidBrush cellBackground = new SolidBrush (cellStyle.BackColor);

                    graphics.FillRectangle (cellBackground, cellBounds);

                    cellBackground.Dispose ();
                } else
                    ;

                // Draw the cell borders, if specified.
                if ((paintParts & DataGridViewPaintParts.Border) == DataGridViewPaintParts.Border) {
                    PaintBorder (graphics, clipBounds, cellBounds, cellStyle, advancedBorderStyle);
                } else
                    ;

                // Calculate the area in which to draw the button.
                Rectangle buttonArea = cellBounds;
                Rectangle buttonAdjustment = this.BorderWidths (advancedBorderStyle);

                buttonArea.X += buttonAdjustment.X;
                buttonArea.Y += buttonAdjustment.Y;

                buttonArea.Height -= buttonAdjustment.Height;
                buttonArea.Width -= buttonAdjustment.Width;

                // Draw the disabled button.
                ButtonRenderer.DrawButton (graphics, buttonArea, PushButtonState.Pressed);

                // Draw the disabled button text. 
                if (this.FormattedValue is String) {
                    TextRenderer.DrawText (graphics, (string)this.FormattedValue, this.DataGridView.Font, buttonArea, SystemColors.ControlText);
                } else
                    ;
            } else {
                // The button cell is enabled, so let the base class 
                // handle the painting.
                base.Paint (graphics, clipBounds, cellBounds, rowIndex, elementState, value, formattedValue, errorText, cellStyle, advancedBorderStyle, paintParts);
            }
        }

        //protected override void OnClick(DataGridViewCellEventArgs e)
        //{
        //    if (Enabled == true)
        //        base.OnClick(e);
        //    else
        //        ;
        //}

        /// <summary>
        /// Делегат обработки события - изменение состояния
        /// </summary>
        public DelegateIntFunc delegatePressChanged;
    }
    /// <summary>
    /// Столбец для представления с типом ячеек "кнопка", с возможностью фиксации состояния "нажата"
    /// </summary>
    public class DataGridViewPressedButtonColumn : DataGridViewButtonColumn {
        /// <summary>
        /// Конструктор - основной (без параметров)
        /// </summary>
        public DataGridViewPressedButtonColumn ()
        {
            this.CellTemplate = new DataGridViewPressedButtonCell ();
            (this.CellTemplate as DataGridViewPressedButtonCell).delegatePressChanged += cell_PressChanged;
        }

        private void cell_PressChanged (int iRow)
        {
            PressChanged?.Invoke (iRow);
        }

        public event DelegateIntFunc PressChanged;
    }
}
