using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text; //StringBuilder
using System.Reflection;
using System.Runtime.InteropServices;

using Excel = Microsoft.Office.Interop.Excel;

namespace HClassLibrary
{
    interface IMSExcelIO
    {
        bool CloseExcelDoc();
        void Dispose();
        void NewDocument();
        void OpenDocument(string name);
        string ReadValue(int col, int row);
        bool SaveExcel(string name);
        void SelectWorksheet(int AIndex);
        bool Visible { get; set; }
        bool WriteValue(int col, int row, double value);
    }

    public class MSExcelIO : IDisposable, IMSExcelIO
    {
        public const string UID = "Excel.Application";
        object oExcel = null;
        object WorkBooks, WorkBook, WorkSheets, WorkSheet, Range;
        private Dictionary<string, int> sheets;

        //КОНСТРУКТОР КЛАССА
        public MSExcelIO()
        {
            oExcel = Activator.CreateInstance(Type.GetTypeFromProgID(UID));
        }

        //ВИДИМОСТЬ EXCEL
        public bool Visible
        {
            set
            {
                oExcel.GetType().InvokeMember("Visible", BindingFlags.SetProperty, null, oExcel, new object[] { value });
            }

            get
            {
                return Convert.ToBoolean(oExcel.GetType().InvokeMember("Visible", BindingFlags.GetProperty, null, oExcel, null));
            }
        }

        //ОТКРЫТЬ ДОКУМЕНТ
        public void OpenDocument(string name)
        {
            WorkBooks = oExcel.GetType().InvokeMember("Workbooks", BindingFlags.GetProperty, null, oExcel, null);
            WorkBook = WorkBooks.GetType().InvokeMember("Open", BindingFlags.InvokeMethod, null, WorkBooks, new object[] { name, true });
            WorkSheets = WorkBook.GetType().InvokeMember("Worksheets", BindingFlags.GetProperty, null, WorkBook, null);
            WorkSheet = WorkSheets.GetType().InvokeMember("Item", BindingFlags.GetProperty, null, WorkSheets, new object[] { 1 });
            //Range = WorkSheet.GetType().InvokeMember("Range", BindingFlags.GetProperty, null, WorkSheet, new object[1] { "A1" });
        }

        //СОЗДАТЬ НОВЫЙ ДОКУМЕНТ
        public void NewDocument()
        {
            WorkBooks = oExcel.GetType().InvokeMember("Workbooks", BindingFlags.GetProperty, null, oExcel, null);
            WorkBook = WorkBooks.GetType().InvokeMember("Add", BindingFlags.InvokeMethod, null, WorkBooks, null);
            WorkSheets = WorkBook.GetType().InvokeMember("Worksheets", BindingFlags.GetProperty, null, WorkBook, null);
            WorkSheet = WorkSheets.GetType().InvokeMember("Item", BindingFlags.GetProperty, null, WorkSheets, new object[] { 1 });
            Range = WorkSheet.GetType().InvokeMember("Range", BindingFlags.GetProperty, null, WorkSheet, new object[1] { "A1" });
        }

        //ЗАКРЫТЬ ДОКУМЕНТ
        public bool CloseExcelDoc()
        {
            try
            {
                WorkBook.GetType().InvokeMember("Close", BindingFlags.InvokeMethod, null, WorkBook, new object[] { true });
                return true;
            }
            catch
            {
                return false;
            }
        }

        //СОХРАНИТЬ ДОКУМЕНТ
        public bool SaveExcel(string name)
        {
            try
            {
                if (File.Exists(name))
                {
                    WorkBook.GetType().InvokeMember("Save", BindingFlags.InvokeMethod, null, WorkBook, null);
                }

                else
                {
                    WorkBook.GetType().InvokeMember("SaveAs", BindingFlags.InvokeMethod, null, WorkBook, new object[] { name });
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        //ЦИФР В БУКВЫ
        private String numtochar(int n)
        {
            StringBuilder sb = new StringBuilder();

            if (! (n > 0))
            {
                return "A";
            }
            else
            {
                while (! (n == 0))
                {
                    sb.Append((char)('A' + (n % 26)));
                    n /= 26;
                }
                return sb.ToString();
            }
        }

        //ЗАПИСЬ ЗНАЧЕНИЯ В ЯЧЕЙКУ
        public bool WriteValue(string sheetName, int col, int row, string value)
        {
            try
            {
                object range = WorkSheet.GetType().InvokeMember("Range", BindingFlags.GetProperty, null, WorkSheet, new object[] { numtochar(col - 1) + (row).ToString() });
                range.GetType().InvokeMember("Value", BindingFlags.SetProperty, null, range, new object[] { value });
                return true;
            }

            catch
            {
                return false;
            }
        }

        //ЗАПИСЬ ЗНАЧЕНИЯ В ЯЧЕЙКУ
        public bool WriteValue(string sheetName, int col, int row, double value)
        {
            try
            {
                object range = WorkSheet.GetType().InvokeMember("Range", BindingFlags.GetProperty, null, WorkSheet, new object[] { numtochar(col - 1) + (row).ToString() });
                range.GetType().InvokeMember("Value", BindingFlags.SetProperty, null, range, new object[] { value });
                return true;
            }

            catch
            {
                return false;
            }
        }

        //ЗАПИСЬ ЗНАЧЕНИЯ В ЯЧЕЙКУ
        public bool WriteValue(int col, int row, string value)
        {
            try
            {
                object range = WorkSheet.GetType().InvokeMember("Range", BindingFlags.GetProperty, null, WorkSheet, new object[] { numtochar(col - 1) + (row).ToString() });
                range.GetType().InvokeMember("Value", BindingFlags.SetProperty, null, range, new object[] { value });
                return true;
            }

            catch
            {
                return false;
            }
        }

        //ЗАПИСЬ ЗНАЧЕНИЯ В ЯЧЕЙКУ
        public bool WriteValue(int col, int row, double value)
        {
            try
            {
                object range = WorkSheet.GetType().InvokeMember("Range", BindingFlags.GetProperty, null, WorkSheet, new object[] { numtochar(col - 1) + (row).ToString() });
                range.GetType().InvokeMember("Value", BindingFlags.SetProperty, null, range, new object[] { value });
                return true;
            }

            catch
            {
                return false;
            }
        }

        //ЧТЕНИЕ ЗНАЧЕНИЯ ИЗ ЯЧЕЙКИ
        public string ReadValue(int col, int row)
        {
            Range = WorkSheet.GetType().InvokeMember("Range", BindingFlags.GetProperty, null, WorkSheet, new object[] { numtochar(col - 1) + (row) });
            return Range.GetType().InvokeMember("Value", BindingFlags.GetProperty, null, Range, null).ToString();
        }

        //ЧТЕНИЕ ЗНАЧЕНИЯ ИЗ ЯЧЕЙКИ
        public string ReadValue(string sheetName, int col, int row)
        {
            SelectWorksheet(sheetName);

            Range = WorkSheet.GetType().InvokeMember("Range", BindingFlags.GetProperty, null, WorkSheet, new object[] { numtochar(col - 1) + (row) });
            return Range.GetType().InvokeMember("Value", BindingFlags.GetProperty, null, Range, null).ToString();
        }

        // Выбрать страницу
        public void SelectWorksheet(string sheetName)
        {
            WorkSheet = WorkSheets.GetType().InvokeMember("Item", BindingFlags.GetProperty, null, WorkSheets, new object[] { sheetName });
        }

        // Выбрать страницу
        public void SelectWorksheet(int AIndex)
        {
            WorkSheet = WorkSheets.GetType().InvokeMember("Item", BindingFlags.GetProperty, null, WorkSheets, new object[] { AIndex });
        }

        //Cells = WorkSheet.GetType().InvokeMember("Cells", BindingFlags.GetProperty, null, WorkSheet, null);

        //УНИЧТОЖЕНИЕ ОБЪЕКТА EXCEL
        public void Dispose()
        {
            Range = null;
            WorkSheet = null;
            WorkSheets = null;
            WorkBook = null;
            WorkBooks = null;

            Marshal.ReleaseComObject(oExcel);
            GC.GetTotalMemory(true);
        }
    }
}