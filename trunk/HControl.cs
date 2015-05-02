using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace HClassLibrary
{
    partial class HLabel
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
        }

        #endregion
    }

    public partial class HLabel : System.Windows.Forms.Label
    {
        public enum TYPE_HLABEL { UNKNOWN = -1, TG, TOTAL, TOTAL_ZOOM, COUNT_TYPE_HLABEL };
        public TYPE_HLABEL m_type;

        public HLabel(Point pt, Size sz, Color foreColor, Color backColor, Single szFont, ContentAlignment align)
        {
            InitializeComponent();

            this.BorderStyle = BorderStyle.Fixed3D;

            if (((pt.X < 0) || (pt.Y < 0)) ||
                ((sz.Width < 0) || (sz.Height < 0)))
                this.Dock = DockStyle.Fill;
            else
            {
                this.Location = pt;
                this.Size = sz;
            }

            this.ForeColor = foreColor;
            this.BackColor = backColor;

            this.TextAlign = align;

            this.Font = new System.Drawing.Font("Microsoft Sans Serif", szFont, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));

            //this.Text = text;

            m_type = TYPE_HLABEL.UNKNOWN;
        }

        public HLabel(HLabelStyles prop)
            : this(new Point(-1, -1), new Size(-1, -1), prop.m_foreColor, prop.m_backColor, prop.m_szFont, prop.m_align)
        {
        }

        public HLabel(IContainer container, Point pt, Size sz, Color foreColor, Color backColor, Single szFont, ContentAlignment align)
            : this(pt, sz, foreColor, backColor, szFont, align)
        {
            container.Add(this);
        }

        public HLabel(IContainer container, HLabelStyles prop)
            : this(new Point(-1, -1), new Size(-1, -1), prop.m_foreColor, prop.m_backColor, prop.m_szFont, prop.m_align)
        {
            container.Add(this);
        }

        public static Label createLabel(string name, HLabelStyles prop)
        {
            Label lblRes = new Label();
            lblRes.Text = name;
            if (((prop.m_pt.X < 0) && (prop.m_pt.Y < 0)) &&
                ((prop.m_sz.Width < 0) && (prop.m_sz.Height < 0)))
                lblRes.Dock = DockStyle.Fill;
            else
            {
                lblRes.Location = prop.m_pt;
                if ((prop.m_sz.Width < 0) && (prop.m_sz.Height < 0))
                    lblRes.AutoSize = true;
                else
                    lblRes.Size = prop.m_sz;
            }
            lblRes.BorderStyle = BorderStyle.Fixed3D;
            lblRes.Font = new System.Drawing.Font("Microsoft Sans Serif", prop.m_szFont, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            lblRes.TextAlign = prop.m_align;
            lblRes.ForeColor = prop.m_foreColor;
            lblRes.BackColor = prop.m_backColor;

            return lblRes;
        }
    }

    public class HLabelStyles
    {
        public Color m_foreColor,
                    m_backColor;
        public ContentAlignment m_align;
        public Single m_szFont;
        public Point m_pt;
        public Size m_sz;

        public HLabelStyles(Color foreColor, Color backColor, Single szFont, ContentAlignment align)
            : this(new Point(-1, -1), new Size(-1, -1), foreColor, backColor, szFont, align)
        {
        }

        public HLabelStyles(Point pt, Size sz, Color foreColor, Color backColor, Single szFont, ContentAlignment align)
        {
            m_pt = pt;
            m_sz = sz;

            m_foreColor = foreColor;
            m_backColor = backColor;
            m_szFont = szFont;
            m_align = align;
        }
    };

    public class CalendarColumn : DataGridViewColumn
    {
        public CalendarColumn()
            : base()
        {
        }

        public override DataGridViewCell CellTemplate
        {
            get { return base.CellTemplate; }

            set {
                // Ensure that the cell used for the template is a CalendarCell.
                if (value != null
                    && !value.GetType().IsAssignableFrom(typeof(CalendarCell))
                    )
                {
                    throw new InvalidCastException("Must be a CalendarCell");
                }

                base.CellTemplate = value;
            }
        }
    }

    public class CalendarCell : DataGridViewTextBoxCell
    {
        private Button btn = new Button();
        private static int counter = 0;
        private int thisButtonCount;

        public CalendarCell()
            : base()
        {
            // Use the short date format.
            this.Style.Format = "d";
            counter++;
            thisButtonCount = counter;

            this.btn.Text = thisButtonCount.ToString();
        }

        public override void InitializeEditingControl(int rowIndex, object initialFormattedValue, DataGridViewCellStyle dataGridViewCellStyle)
        {
            // Set the value of the editing control to the current cell value.
            base.InitializeEditingControl(rowIndex, initialFormattedValue, dataGridViewCellStyle);
            CalendarEditingControl ctl = DataGridView.EditingControl as CalendarEditingControl;
            ctl.Value = (DateTime)this.Value;
        }

        protected override void Paint(System.Drawing.Graphics graphics
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
            base.Paint(graphics, clipBounds, cellBounds, rowIndex, cellState, value, formattedValue, errorText, cellStyle, advancedBorderStyle, paintParts);

            btn.Size = cellBounds.Size;
            btn.Location = cellBounds.Location;
            this.DataGridView.Controls.Add(btn);
        }

        public Button GetControl
        {
            get { return btn; }
        }

        public override Type EditType
        {
            get
            {
                // Return the type of the editing contol that CalendarCell uses.
                return typeof(CalendarEditingControl);
            }
        }

        public override Type ValueType
        {
            get
            {
                // Return the type of the value that CalendarCell contains.
                return typeof(DateTime);
            }
        }

        public override object DefaultNewRowValue
        {
            get
            {
                // Use the current date and time as the default value.
                return DateTime.Now;
            }
        }
    }

    class CalendarEditingControl : DateTimePicker, IDataGridViewEditingControl
    {
        DataGridView dataGridView;
        private bool valueChanged = false;
        int rowIndex;

        public CalendarEditingControl()
        {
            this.Format = DateTimePickerFormat.Short;
        }

        // Implements the
        // IDataGridViewEditingControl.EditingControlFormattedValue
        // property.
        public object EditingControlFormattedValue
        {
            get { return this.Value.ToShortDateString(); }

            set
            {
                String newValue = value as String;
                if (newValue != null)
                {
                    this.Value = DateTime.Parse(newValue);
                }
                else
                    ;
            }
        }

        // Implements the
        // IDataGridViewEditingControl.GetEditingControlFormattedValue method.
        public object GetEditingControlFormattedValue(DataGridViewDataErrorContexts context)
        {
            return EditingControlFormattedValue;
        }

        // Implements the
        // IDataGridViewEditingControl.ApplyCellStyleToEditingControl method.
        public void ApplyCellStyleToEditingControl(DataGridViewCellStyle dataGridViewCellStyle)
        {
            this.Font = dataGridViewCellStyle.Font;
            this.CalendarForeColor = dataGridViewCellStyle.ForeColor;
            this.CalendarMonthBackground = dataGridViewCellStyle.BackColor;
        }

        // Implements the IDataGridViewEditingControl.EditingControlRowIndex
        // property.
        public int EditingControlRowIndex
        {
            get { return rowIndex; }

            set { rowIndex = value; }
        }

        // Implements the IDataGridViewEditingControl.EditingControlWantsInputKey
        // method.
        public bool EditingControlWantsInputKey(Keys key, bool dataGridViewWantsInputKey)
        {
            // Let the DateTimePicker handle the keys listed.
            switch (key & Keys.KeyCode)
            {
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
        public void PrepareEditingControlForEdit(bool selectAll)
        {
            // No preparation needs to be done.
        }

        // Implements the IDataGridViewEditingControl
        // .RepositionEditingControlOnValueChange property.
        public bool RepositionEditingControlOnValueChange
        {
            get { return false; }
        }

        // Implements the IDataGridViewEditingControl
        // .EditingControlDataGridView property.
        public DataGridView EditingControlDataGridView
        {
            get { return dataGridView; }

            set { dataGridView = value; }
        }

        // Implements the IDataGridViewEditingControl
        // .EditingControlValueChanged property.
        public bool EditingControlValueChanged
        {
            get { return valueChanged; }

            set { valueChanged = value; }
        }

        // Implements the IDataGridViewEditingControl
        // .EditingPanelCursor property.
        public Cursor EditingPanelCursor
        {
            get { return base.Cursor; }
        }

        protected override void OnValueChanged(EventArgs eventargs)
        {
            // Notify the DataGridView that the contents of the cell
            // have changed.
            valueChanged = true;
            this.EditingControlDataGridView.NotifyCurrentCellDirty(true);
            base.OnValueChanged(eventargs);
        }
    }

    public class DataGridViewPasswordTextBoxCell : DataGridViewTextBoxCell
    {
        private TextBox _textBox;

        public DataGridViewPasswordTextBoxCell()
            : base()
        {
            _textBox = new TextBox();
            _textBox.PasswordChar = '#';
            _textBox.BorderStyle = BorderStyle.None;
        }

        public override void InitializeEditingControl(int rowIndex, object initialFormattedValue, DataGridViewCellStyle dataGridViewCellStyle)
        {
            // Set the value of the editing control to the current cell value.
            base.InitializeEditingControl(rowIndex, initialFormattedValue, dataGridViewCellStyle);
            //TextBoxEditingControl ctrl = DataGridView.EditingControl as TextBoxEditingControl;
            //ctrl.Text = this.Value as string;
        }

        protected override void Paint(System.Drawing.Graphics graphics
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
            base.Paint(graphics, clipBounds, cellBounds, rowIndex, cellState, value, formattedValue, errorText, cellStyle, advancedBorderStyle, paintParts);

            //_textBox.Size = cellBounds.Size;
            _textBox.Size = new Size(cellBounds.Size.Width, cellBounds.Size.Height + 4);
            //_textBox.Location = cellBounds.Location;
            _textBox.Location = new Point(cellBounds.Location.X, cellBounds.Location.Y + 4);
            this.DataGridView.Controls.Add(_textBox);
        }

        public TextBox GetControl
        {
            get { return _textBox; }
        }

        public override Type EditType
        {
            get
            {
                // Return the type of the editing contol that CalendarCell uses.
                return
                    typeof(
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
                return typeof(DateTime);
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

        public void SetValue()
        {
            _textBox.Text = Value.ToString();
        }
    }
}
