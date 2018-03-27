using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ASUTP.Control {
    /// <summary>
    /// Тип объекта - календарь для размещения в один из столбцов 'DataGridView'
    /// </summary>
    public class CalendarColumn : DataGridViewColumn {
        /// <summary>
        /// Конструктор - основной (без параметров)
        /// </summary>
        public CalendarColumn ()
            : base ()
        {
        }
        /// <summary>
        /// Шаблон для ячейки в столбце-каледаре
        /// </summary>
        public override DataGridViewCell CellTemplate
        {
            get
            {
                return base.CellTemplate;
            }

            set
            {
                // Ensure that the cell used for the template is a CalendarCell.
                if (value != null
                    && !value.GetType ().IsAssignableFrom (typeof (CalendarCell))
                    ) {
                    throw new InvalidCastException ("Must be a CalendarCell");
                }

                base.CellTemplate = value;
            }
        }
    }

    /// <summary>
    /// Класс для ячейки представления с внедренным календарем
    /// </summary>
    public class CalendarCell : DataGridViewTextBoxCell {
        private Button btn = new Button ();
        private static int counter = 0;
        private int thisButtonCount;

        /// <summary>
        /// Конструктор - основной (без параметров)
        /// </summary>
        public CalendarCell ()
            : base ()
        {
            // Use the short date format.
            this.Style.Format = "d";
            counter++;
            thisButtonCount = counter;

            this.btn.Text = thisButtonCount.ToString ();
        }

        /// <summary>
        /// Инициализация элемента интерфейса
        /// </summary>
        /// <param name="rowIndex">Номер/индекс строки</param>
        /// <param name="initialFormattedValue">Объект правила для форматирования</param>
        /// <param name="dataGridViewCellStyle">Стиль для оформления ячейки</param>
        public override void InitializeEditingControl (int rowIndex, object initialFormattedValue, DataGridViewCellStyle dataGridViewCellStyle)
        {
            // Set the value of the editing control to the current cell value.
            base.InitializeEditingControl (rowIndex, initialFormattedValue, dataGridViewCellStyle);
            CalendarEditingControl ctl = DataGridView.EditingControl as CalendarEditingControl;
            ctl.Value = (DateTime)this.Value;
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
                                        , System.Drawing.Rectangle cellBounds
                                        , int rowIndex
                                        , DataGridViewElementStates cellState
                                        , object value
                                        , object formattedValue
                                        , string errorText
                                        , DataGridViewCellStyle cellStyle
                                        , DataGridViewAdvancedBorderStyle advancedBorderStyle
                                        , DataGridViewPaintParts paintParts
                                )
        {
            //throw new Exception("The method or operation is not implemented.");
            base.Paint (graphics, clipBounds, cellBounds, rowIndex, cellState, value, formattedValue, errorText, cellStyle, advancedBorderStyle, paintParts);

            btn.Size = cellBounds.Size;
            btn.Location = cellBounds.Location;
            this.DataGridView.Controls.Add (btn);
        }

        /// <summary>
        /// ??? Свойство - объект "кнопка"
        /// </summary>
        public Button GetControl
        {
            get
            {
                return btn;
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
                return typeof (CalendarEditingControl);
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
                // Use the current date and time as the default value.
                return DateTime.Now;
            }
        }
    }

    class CalendarEditingControl : DateTimePicker, IDataGridViewEditingControl {
        DataGridView dataGridView;
        private bool valueChanged = false;
        int rowIndex;

        public CalendarEditingControl ()
        {
            this.Format = DateTimePickerFormat.Short;
        }

        // Implements the
        // IDataGridViewEditingControl.EditingControlFormattedValue
        // property.
        public object EditingControlFormattedValue
        {
            get
            {
                return this.Value.ToShortDateString ();
            }

            set
            {
                String newValue = value as String;
                if (newValue != null) {
                    this.Value = DateTime.Parse (newValue);
                } else
                    ;
            }
        }

        // Implements the
        // IDataGridViewEditingControl.GetEditingControlFormattedValue method.
        public object GetEditingControlFormattedValue (DataGridViewDataErrorContexts context)
        {
            return EditingControlFormattedValue;
        }

        // Implements the
        // IDataGridViewEditingControl.ApplyCellStyleToEditingControl method.
        public void ApplyCellStyleToEditingControl (DataGridViewCellStyle dataGridViewCellStyle)
        {
            this.Font = dataGridViewCellStyle.Font;
            this.CalendarForeColor = dataGridViewCellStyle.ForeColor;
            this.CalendarMonthBackground = dataGridViewCellStyle.BackColor;
        }

        // Implements the IDataGridViewEditingControl.EditingControlRowIndex
        // property.
        public int EditingControlRowIndex
        {
            get
            {
                return rowIndex;
            }

            set
            {
                rowIndex = value;
            }
        }

        // Implements the IDataGridViewEditingControl.EditingControlWantsInputKey
        // method.
        public bool EditingControlWantsInputKey (Keys key, bool dataGridViewWantsInputKey)
        {
            // Let the DateTimePicker handle the keys listed.
            switch (key & Keys.KeyCode) {
                case Keys.Left:
                case Keys.Up:
                case Keys.Down:
                case Keys.Right:
                case Keys.Home:
                case Keys.End:
                case Keys.PageDown:
                case Keys.PageUp:
                    return true;
                default:
                    return false;
            }
        }

        // Implements the
        // IDataGridViewEditingControl.PrepareEditingControlForEdit
        // method.
        public void PrepareEditingControlForEdit (bool selectAll)
        {
            // No preparation needs to be done.
        }

        // Implements the IDataGridViewEditingControl
        // .RepositionEditingControlOnValueChange property.
        public bool RepositionEditingControlOnValueChange
        {
            get
            {
                return false;
            }
        }

        // Implements the IDataGridViewEditingControl
        // .EditingControlDataGridView property.
        public DataGridView EditingControlDataGridView
        {
            get
            {
                return dataGridView;
            }

            set
            {
                dataGridView = value;
            }
        }

        // Implements the IDataGridViewEditingControl
        // .EditingControlValueChanged property.
        public bool EditingControlValueChanged
        {
            get
            {
                return valueChanged;
            }

            set
            {
                valueChanged = value;
            }
        }

        // Implements the IDataGridViewEditingControl
        // .EditingPanelCursor property.
        public Cursor EditingPanelCursor
        {
            get
            {
                return base.Cursor;
            }
        }

        protected override void OnValueChanged (EventArgs eventargs)
        {
            // Notify the DataGridView that the contents of the cell
            // have changed.
            valueChanged = true;
            this.EditingControlDataGridView.NotifyCurrentCellDirty (true);
            base.OnValueChanged (eventargs);
        }
    }
}
