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

        public TextBox GetControl
        {
            get
            {
                return _textBox;
            }
        }

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

        public override Type ValueType
        {
            get
            {
                // Return the type of the value that CalendarCell contains.
                return typeof (DateTime);
            }
        }

        public override object DefaultNewRowValue
        {
            get
            {
                // Use the default value.
                return string.Empty;
            }
        }

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

        // Override the Clone method so that the Enabled property is copied.
        public override object Clone ()
        {
            DataGridViewDisableButtonCell cell = (DataGridViewDisableButtonCell)base.Clone ();

            cell.Enabled = this.Enabled;

            return cell;
        }

        // By default, enable the button cell.
        public DataGridViewDisableButtonCell ()
        {
            this.enabledValue = true;
        }

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
