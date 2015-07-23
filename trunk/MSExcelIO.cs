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
    /// <summary>
    /// Класс для описания объекта-приложения MS Excel
    /// </summary>
    public class MSExcelIO : IDisposable
    {
        /// <summary>
        /// Строка - идентификатор приложения MS Excel
        /// </summary>
        public const string UID = "Excel.Application";
        /// <summary>
        /// Объект - приложения MS Excel
        /// </summary>
        object oExcel = null;
        /// <summary>
        /// Объект - массив открытых документов - книг MS Excel
        /// </summary>
        object WorkBooks
        /// <summary>
        /// Объект - текущая книга
        /// </summary>
            , WorkBook
        /// <summary>
        /// Объект - массив листов книги MS Excel
        /// </summary>
            , WorkSheets
        /// <summary>
        /// Объект - текущий лист текущей книги MS Excel
        /// </summary>
            , WorkSheet
        /// <summary>
        /// Объект - текущий диапазон текущего листа текущей книги MS Excel
        /// </summary>
            , Range;
        /// <summary>
        /// Обэект - словарь листов текущей книги (ключ-наименование:значение-порядковый номер листа)
        /// </summary>
        private Dictionary<string, int> sheets;

        /// <summary>
        /// КОНСТРУКТОР КЛАССА
        /// </summary>
        public MSExcelIO()
        {
            oExcel = Activator.CreateInstance(Type.GetTypeFromProgID(UID));
        }

        /// <summary>
        /// ВИДИМОСТЬ MS EXCEL
        /// </summary>
        public bool Visible
        {
            set
            {
                //Вызвать метод 'SetProperty' объекта 'oExcel' для свойства 'Visible' с параметром 'value'
                oExcel.GetType().InvokeMember("Visible", BindingFlags.SetProperty, null, oExcel, new object[] { value });
            }

            get
            {
                //Вызвать метод 'GetProperty' объекта 'oExcel' для свойства 'Visible' без параметров
                return Convert.ToBoolean(oExcel.GetType().InvokeMember("Visible", BindingFlags.GetProperty, null, oExcel, null));
            }
        }

        /// <summary>
        /// Получить массив всех открытых документов - книг
        /// </summary>
        /// <returns>Массив всех открытых документов - книг</returns>
        private object getWorkBooks()
        {
            return oExcel.GetType().InvokeMember("Workbooks", BindingFlags.GetProperty, null, oExcel, null);
        }

        /// <summary>
        /// Добавить к массиву открытых документов элемент - книгу с именем - полным путем к книге
        /// </summary>
        /// <returns></returns>
        private object addWorkBook()
        {
            return WorkBooks.GetType().InvokeMember("Add", BindingFlags.InvokeMethod, null, WorkBooks, null);
        }

        /// <summary>
        /// Добавить к массиву открытых документов элемент - книгу с именем - полным путем к книге
        /// </summary>
        /// <param name="name">Строка - полный путь к книге</param>
        /// <returns>Объект - книга MS Excel</returns>
        private object openWorkBook(string name)
        {
            return WorkBooks.GetType().InvokeMember("Open", BindingFlags.InvokeMethod, null, WorkBooks, new object[] { name, true });
        }

        /// <summary>
        /// Возвратить объект - книгу MS Excel по указанному наименованию
        /// </summary>
        /// <param name="name">Наименование книги (внутренне для приложения)</param>
        /// <returns>Объект - книга MS Excel</returns>
        private object getWorkBook(string name)
        {
            return WorkBooks.GetType().InvokeMember("Item", BindingFlags.GetProperty, null, WorkBooks, new object[] { name });
        }

        /// <summary>
        /// Возвратить объект - книгу MS Excel по указанному индексу
        /// </summary>
        /// <param name="indx">Индекс книги (по умолчанию - 1)</param>
        /// <returns>Объект - книга MS Excel</returns>
        private object getWorkBook(int indx = 1)
        {
            object objRes = null;

            if (Count > 0)
                objRes = WorkBooks.GetType().InvokeMember("Item", BindingFlags.GetProperty, null, WorkBooks, new object[] { indx });
            else
                ;

            return objRes;
        }

        /// <summary>
        /// Получить массив всех листов текущей книги
        /// </summary>
        /// <returns>Массив всех листов книги</returns>
        private object getWorkSheets()
        {
            return WorkBook.GetType().InvokeMember("Worksheets", BindingFlags.GetProperty, null, WorkBook, null);
        }

        /// <summary>
        /// Получить лист книги с указанным номером (1 - по умолчанию)
        /// </summary>
        /// <param name="num">Номер листа (1-ый - по умолчанию)</param>
        /// <returns></returns>
        private object getWorkSheet(int num = 1)
        {
            return WorkSheets.GetType().InvokeMember("Item", BindingFlags.GetProperty, null, WorkSheets, new object[] { num });
        }

        private object getWorkSheet(string sheetName)
        {
            return WorkSheets.GetType().InvokeMember("Item", BindingFlags.GetProperty, null, WorkSheets, new object[] { sheetName });
        }

        /// <summary>
        /// Получить объект - диапазон ячеек с указанным адресом на текущем листе
        /// </summary>
        /// <param name="pos">Строка - адрес диапазона (по умолчанию "A1")</param>
        /// <returns>Объект - диапазон ячеек</returns>
        private object getRange(string pos = "A1")
        {
            return WorkSheet.GetType().InvokeMember("Range", BindingFlags.GetProperty, null, WorkSheet, new object[1] { pos });
        }

        /// <summary>
        /// Получить объект - диапазон ячеек с указанным адресом на текущем листе
        /// </summary>
        /// <param name="col">Номер толбца</param>
        /// <param name="row">Номер строки</param>
        /// <returns>Объект - диапазон ячеек</returns>
        private object getRange(int col, int row)
        {
            return getRange(numtochar(col - 1) + (row).ToString());
        }

        /// <summary>
        /// ОТКРЫТЬ ДОКУМЕНТ
        /// </summary>
        /// <param name="name">Строка - полный путь к документу</param>
        public void OpenDocument(string name)
        {
            //Получить массив всех открытых документов - книг
            WorkBooks = getWorkBooks();
            //Добавить к массиву открытых документов элемент - книгу с именем - полным путем к книге
            WorkBook = openWorkBook(name);
            //Получить массив всех листов книги
            WorkSheets = getWorkSheets ();
            //Получить лист книги с указанным номером (1 - по умолчанию)
            WorkSheet = getWorkSheet();
            //Получить объект - диапазон ячеек с адресом "A1" на новом листе
            Range = getRange();
        }

        /// <summary>
        /// СОЗДАТЬ НОВЫЙ ДОКУМЕНТ
        /// </summary>
        public void NewDocument()
        {
            //Получить массив всех открытых документов - книг
            WorkBooks = getWorkBooks();
            //Добавить к массиву открытых документов элемент - новую книгу
            WorkBook = addWorkBook();
            //Получить массив всех листов текущей книги
            WorkSheets = getWorkSheets();
            //Получить лист книги с указанным номером (1 - по умолчанию)
            WorkSheet = getWorkSheet();
            //Получить объект диапазон ячеек с адресом "A1" на текущем листе
            Range = getRange();
        }

        /// <summary>
        /// Установить значение для ячейки с указанными номерами стобца, строки
        ///  на указанной странице (страница становится активной) текущей книги
        /// </summary>
        /// <param name="indx">Индекс страницы</param>
        /// <param name="col">Номер столбца</param>
        /// <param name="row">Номер строки</param>
        /// <param name="value">Значение ячейки</param>
        /// <param name="bSelectSheet">Признак установки активной указанной страницы</param>
        private void setValue(int indx, int col, int row, double value, bool bSelectSheet = true)
        {
            SelectWorksheet(indx);
            setValue(col, row, value.ToString());
        }

        /// <summary>
        /// Установить значение для ячейки с указанными номерами стобца, строки
        ///  на указанной странице (страница становится активной) текущей книги
        /// </summary>
        /// <param name="sheetName">Наименование страницы</param>
        /// <param name="col">Номер столбца</param>
        /// <param name="row">Номер строки</param>
        /// <param name="value">Значение ячейки</param>
        /// <param name="bSelectSheet">Признак установки активной указанной страницы</param>
        private void setValue(string sheetName, int col, int row, double value, bool bSelectSheet = true)
        {
            SelectWorksheet(sheetName);
            setValue(col, row, value.ToString());
        }

        /// <summary>
        /// Установить значение для ячейки с указанными номерами стобца, строки
        ///  на указанной странице (страница становится активной) текущей книги
        /// </summary>
        /// <param name="indx">Индекс страницы</param>
        /// <param name="col">Номер столбца</param>
        /// <param name="row">Номер строки</param>
        /// <param name="value">Значение ячейки</param>
        /// <param name="bSelectSheet">Признак установки активной указанной страницы</param>
        private void setValue(int indx, int col, int row, string value, bool bSelectSheet = true)
        {
            SelectWorksheet(indx);
            setValue(col, row, value);
        }

        /// <summary>
        /// Установить значение для ячейки с указанными номерами стобца, строки
        ///  на указанной странице (страница становится активной) текущей книги
        /// </summary>
        /// <param name="sheetName">Наименование страницы</param>
        /// <param name="col">Номер столбца</param>
        /// <param name="row">Номер строки</param>
        /// <param name="value">Значение ячейки</param>
        /// <param name="bSelectSheet">Признак установки активной указанной страницы</param>
        private void setValue(string sheetName, int col, int row, string value, bool bSelectSheet = true)
        {
            SelectWorksheet(sheetName);
            setValue(col, row, value);
        }

        /// <summary>
        /// Установить значение для ячейки с указанными номерами стобца, строки
        ///  на текущей странице текущей книги
        /// </summary>
        /// <param name="col">Номер столбца</param>
        /// <param name="row">Номер строки</param>
        /// <param name="value">Значение ячейки</param>
        private void setValue(int col, int row, double value)
        {
            setValue(col, row, value.ToString());
        }

        /// <summary>
        /// Установить значение для ячейки с указанными номерами стобца, строки
        ///  на текущей странице текущей книги
        /// </summary>
        /// <param name="col">Номер столбца</param>
        /// <param name="row">Номер строки</param>
        /// <param name="value">Значение ячейки</param>
        private void setValue(int col, int row, string value)
        {
            object range = getRange(col, row);
            setValue(range, value);
        }

        /// <summary>
        /// Установить значение для ячейки по указанному адресу диапазона ячеек
        ///  на текущей странице текущей книги
        /// </summary>
        /// <param name="pos">Адрес диапазона ячеек</param>
        /// <param name="value">Значение ячейки</param>
        private void setValue(string pos, string value)
        {
            object range = getRange(pos);
            setValue(range, value);
        }

        /// <summary>
        /// Установить значение для ячейки по указанному адресу диапазона ячеек
        ///  на текущей странице текущей книги
        /// </summary>
        /// <param name="pos">Адрес диапазона ячеек</param>
        /// <param name="value">Значение ячейки</param>
        private void setValue(string pos, double value)
        {
            setValue (pos, value.ToString ());
        }

        /// <summary>
        /// Установить значение для ячейки в указанном диапазоне ячеек
        ///  на текущей странице текущей книги
        /// </summary>
        /// <param name="range">Объект - диапазон ячеек</param>
        /// <param name="value">Значение ячейки</param>
        private void setValue(object range, string value)
        {
            range.GetType().InvokeMember("Value", BindingFlags.SetProperty, null, range, new object[] { value });
        }

        /// <summary>
        /// Закрыть текущий документ
        /// </summary>
        /// <returns>Ркезультат выполнения операции</returns>
        public bool CloseExcelDoc()
        {
            bool bRes = true;

            try
            {
                //Вызвать метод 'Close' для текущей книги 'WorkBook' с параметром 'true'
                WorkBook.GetType().InvokeMember("Close", BindingFlags.InvokeMethod, null, WorkBook, new object[] { true });
            }
            catch (Exception e)
            {
                Logging.Logg().Exception(e, Logging.INDEX_MESSAGE.NOT_SET, @"MSExcelIO::CloseExcelDoc () - ...");

                bRes = false;
            }

            return bRes;
        }

        /// <summary>
        /// Количество открытых документов
        /// </summary>
        private int Count
        {
            get
            {
                //Вызвать метод 'Close' для текущей книги 'WorkBook' с параметром 'true'
                return (int)WorkBooks.GetType().InvokeMember("Count", BindingFlags.GetProperty, null, WorkBooks, null);
            }
        }

        /// <summary>
        /// Закрыть все книги текущего объекта приложения MS Excel
        /// </summary>
        /// <returns></returns>
        public bool CloseExcelAllDocs()
        {
            bool bRes = true;

            try
            {
                while ((Count > 0) && (! (WorkBook == null)))
                {
                    CloseExcelDoc();
                    WorkBook = getWorkBook();
                }
            }
            catch (Exception e)
            {
                Logging.Logg().Exception(e, Logging.INDEX_MESSAGE.NOT_SET, @"MSExcelIO::CloseExcelAllDocs () - ...");

                bRes = false;
            }

            return bRes;
        }

        /// <summary>
        /// СОХРАНИТЬ текущий ДОКУМЕНТ
        /// </summary>
        /// <param name="name">Строка - полный путь документа - книги MS Excel</param>
        /// <returns></returns>
        public bool SaveExcel(string name)
        {
            bool bRes = true;

            try
            {
                //Проверить существование файла
                if (File.Exists(name))
                {
                    //Вызвать метод 'Save' для текущей книги 'WorkBook' без параметров
                    WorkBook.GetType().InvokeMember("Save", BindingFlags.InvokeMethod, null, WorkBook, null);
                }
                else
                {
                    //Вызвать метод 'SaveAs' для текущей книги 'WorkBook' с параметром - наименование - полный путь к книге
                    WorkBook.GetType().InvokeMember("SaveAs", BindingFlags.InvokeMethod, null, WorkBook, new object[] { name });
                }
            }
            catch (Exception e)
            {
                Logging.Logg().Exception(e, Logging.INDEX_MESSAGE.NOT_SET, @"MSExcelIO::SaveExcel () - ...");

                bRes = false;
            }

            return bRes;
        }

        /// <summary>
        /// Возвратить строку - наименование столбца на листе книги MS Excel
        ///  по указанному номеру столбца
        /// </summary>
        /// <param name="n">Номер столбца на листе книги MS Excel</param>
        /// <returns>Строка - наименование столбца</returns>
        private String numtochar(int n)
        {
            StringBuilder sbRes = new StringBuilder();
            //Проверить корректность номера столбуа
            if (! (n > 0))
                //При ошибке всегда возвращать A
                sbRes.Append ("A");
            else
            {
                //Проверить количество букв в наименовании
                if (n > 26)
                    //Добавить 1-ю букву
                    sbRes.Append((char)('A' + ((n / 26) - 1)));
                else
                    ;
                //Добавить крайнюю букву
                sbRes.Append((char)('A' + (n % 26)));
            }
            //Вернуть результат
            return sbRes.ToString();
        }

        /// <summary>
        /// ЗАПИСЬ ЗНАЧЕНИЯ В ЯЧЕЙКУ на страницу книги с указанными номерами столбца, строки
        /// </summary>
        /// <param name="sheetName">Наименование страницы</param>
        /// <param name="col">Номер столбца</param>
        /// <param name="row">Номер строки</param>
        /// <param name="value">Значение для записи</param>
        /// <returns>Признак выполнения операции (True - успех)</returns>
        public bool WriteValue(string sheetName, int col, int row, string value)
        {
            bool bRes = true;

            try
            {
                setValue(col, row, value);
            }
            catch (Exception e)
            {
                Logging.Logg().Exception(e, Logging.INDEX_MESSAGE.NOT_SET, @"MSExcelIO::WriteValue () - ...");

                bRes = false;
            }

            return bRes;
        }

        /// <summary>
        /// ЗАПИСЬ ЗНАЧЕНИЯ В ЯЧЕЙКУ
        /// </summary>
        /// <param name="sheetName">Наименование листа</param>
        /// <param name="col">Номер столбца</param>
        /// <param name="row">Номер строки</param>
        /// <param name="value">Значение для записи</param>
        /// <returns>Признак выполнения операции (True - успех)</returns>
        public bool WriteValue(string sheetName, int col, int row, double value)
        {
            bool bRes = true;

            try
            {
                setValue(sheetName, col, row, value);
            }
            catch (Exception e)
            {
                Logging.Logg().Exception(e, Logging.INDEX_MESSAGE.NOT_SET, @"MSExcelIO::WriteValue () - ...");

                bRes = false;
            }

            return bRes;
        }

        /// <summary>
        /// ЗАПИСЬ ЗНАЧЕНИЯ В ЯЧЕЙКУ
        /// </summary>
        /// <param name="col">Номер столбца</param>
        /// <param name="row">Номер строки</param>
        /// <param name="value">Значение ячейки</param>
        /// <returns>Результат записи значения</returns>
        public bool WriteValue(int col, int row, string value)
        {
            bool bRes = true;

            try
            {
                setValue(col, row, value);
            }
            catch (Exception e)
            {
                Logging.Logg().Exception(e, Logging.INDEX_MESSAGE.NOT_SET, @"MSExcelIO::WriteValue () - ...");
                
                bRes = false;
            }

            return bRes;
        }

        /// <summary>
        /// ЗАПИСЬ ЗНАЧЕНИЯ В ЯЧЕЙКУ
        /// </summary>
        /// <param name="col">Номер столбца</param>
        /// <param name="row">Номер строки</param>
        /// <param name="value">Значение ячейки</param>
        /// <returns>Результат записи значения</returns>
        public bool WriteValue(int col, int row, double value)
        {
            bool bRes = true;

            try
            {
                setValue(col, row, value);
            }
            catch (Exception e)
            {
                Logging.Logg().Exception(e, Logging.INDEX_MESSAGE.NOT_SET, @"MSExcelIO::WriteValue () - ...");

                bRes = false;
            }

            return bRes;
        }

        /// <summary>
        /// Получить значение из ячеек диапазона по указанным номерам столбуа, строки
        /// </summary>
        /// <param name="col">Номер столбца</param>
        /// <param name="row">Номер строки</param>
        /// <returns>Значение ячейки</returns>
        private string getValue(int col, int row)
        {
            Range = getRange(col, row);
            return getValue();
        }

        /// <summary>
        /// Получить значение из ячеек диапазона по адресу
        /// </summary>
        /// <param name="pos">Адрес диапазона ячеек</param>
        /// <returns>Значение ячейки</returns>
        private string getValue(string pos)
        {
            Range = getRange(pos);
            return getValue();
        }

        /// <summary>
        /// Получить значение из ячеек указанного диапазона
        /// </summary>
        /// <param name="range">Диапазон ячеек</param>
        /// <returns>Значение ячейки</returns>
        private string getValue(object range)
        {
            Range = range;
            return getValue();
        }

        /// <summary>
        /// Получить значение из ячеек текущего диапазона
        /// </summary>
        /// <returns>Значение ячейки</returns>
        private string getValue()
        {
            return Range.GetType().InvokeMember("Value", BindingFlags.GetProperty, null, Range, null).ToString();
        }

        /// <summary>
        /// ЧТЕНИЕ ЗНАЧЕНИЯ ИЗ ЯЧЕЙКИ
        /// </summary>
        /// <param name="col">Номер столбца</param>
        /// <param name="row">Номер строки</param>
        /// <returns>Значение ячейки</returns>
        public string ReadValue(int col, int row)
        {
            return getValue(col, row);
        }

        /// <summary>
        /// ЧТЕНИЕ ЗНАЧЕНИЯ ИЗ ЯЧЕЙКИ
        /// </summary>
        /// <param name="sheetName">Наименование страницы</param>
        /// <param name="col">Номер столбца</param>
        /// <param name="row">Номер строки</param>
        /// <returns>Значение ячейки</returns>
        public string ReadValue(string sheetName, int col, int row)
        {
            SelectWorksheet(sheetName);
            return getValue (col, row);
        }

        /// <summary>
        /// Выбрать (установить текущую) страницу
        /// </summary>
        /// <param name="indx">Наименование страницы</param>
        public void SelectWorksheet(string sheetName)
        {
            WorkSheet = getWorkSheet(sheetName);
            Range = null;
        }

        /// <summary>
        /// Выбрать (установить текущую) страницу
        /// </summary>
        /// <param name="indx">Индекс страницы</param>
        public void SelectWorksheet(int indx)
        {
            WorkSheet = getWorkSheet (indx);
            Range = null;
        }

        //Cells = WorkSheet.GetType().InvokeMember("Cells", BindingFlags.GetProperty, null, WorkSheet, null);

        /// <summary>
        /// УНИЧТОЖЕНИЕ ОБЪЕКТА EXCEL
        /// </summary>
        public void Dispose()
        {
            CloseExcelAllDocs();

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