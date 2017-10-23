using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection;

using System.Threading;
using System.Diagnostics; //Process
using ASUTP.Core;
using ASUTP.Database;

namespace ASUTP.Helper
{
    #region Типы делегатов ???заменить на Action<>, Func
    ///// <summary>
    ///// Тип для делегата без аргументов и без возвращаемого значения
    ///// </summary>
    //public delegate void DelegateFunc();
    ///// <summary>
    ///// Тип для делегата с аргументом типа 'int' и без возвращаемого значения
    ///// </summary>
    ///// <param name="param">Аргумент 1</param>
    //public delegate void DelegateIntFunc(int param);
    /// <summary>
    /// Тип для делегата с аргументами типа 'int', 'int' и без возвращаемого значения
    /// </summary>
    /// <param name="param1">Аргумент 1</param>
    /// <param name="param2">Аргумент 2</param>
    public delegate void DelegateIntIntFunc(int param1, int param2);
    /// <summary>
    /// Тип для делегата с аргументами типа 'int', 'int' с возвращаемым значением типа 'int'
    /// </summary>
    /// <param name="param1">Аргумент 1</param>
    /// <param name="param2">Аргумент 2</param>
    /// <returns>Результат выполнения</returns>
    public delegate int IntDelegateIntIntFunc(int param1, int param2);
    /// <summary>
    /// Тип для делегата с аргументом типа 'string' и без возвращаемого значения
    /// </summary>
    /// <param name="param">Аргумент 1</param>
    public delegate void DelegateStringFunc(string param);
    /// <summary>
    /// Тип для делегата с аргументом типа 'bool' и без возвращаемого значения
    /// </summary>
    /// <param name="param">Аргумент 1</param>
    public delegate void DelegateBoolFunc(bool param);
    /// <summary>
    /// Тип для делегата с аргументом типа 'object' и без возвращаемого значения
    /// </summary>
    /// <param name="obj">Аргумент 1</param>
    public delegate void DelegateObjectFunc(object obj);
    /// <summary>
    /// Тип для делегата с аргументом типа 'ссылка на object' с и без возвращаемого значения
    /// </summary>
    /// <param name="obj">Аргумент 1</param>
    public delegate void DelegateRefObjectFunc(ref object obj);
    /// <summary>
    /// Тип для делегата с аргументом типа 'DateTime' с и без возвращаемого значения
    /// </summary>
    /// <param name="date">Аргумент 1</param>
    public delegate void DelegateDateFunc(DateTime date);
    /// <summary>
    /// Тип для делегата с аргументом типа 'DateTime' и без возвращаемого значения
    /// </summary>
    /// <returns>Результат выполнения</returns>
    public delegate int IntDelegateFunc();
    /// <summary>
    /// Тип для делегата с аргументом типа 'int' с возвращаемым значения типа 'int'
    /// </summary>
    /// <param name="param">>Аргумент 1</param>
    /// <returns>Результат выполнения</returns>
    public delegate int IntDelegateIntFunc(int param);
    /// <summary>
    /// Тип для делегата без аргументов с возвращаемым значения типа 'string'
    /// </summary>
    /// <returns>Результат выполнения</returns>
    public delegate string StringDelegateFunc();
    /// <summary>
    /// Тип для делегата с аргументом типа 'int' с возвращаемым значения типа 'string'
    /// </summary>
    /// <param name="param">Аргумент 1</param>
    /// <returns>Результат выполнения</returns>
    public delegate string StringDelegateIntFunc(int param);
    /// <summary>
    /// Тип для делегата с аргументом типа 'string' с возвращаемым значения типа 'string'
    /// </summary>
    /// <param name="keyParam">Аргумент 1</param>
    /// <returns>Результат выполнения</returns>
    public delegate string StringDelegateStringFunc(string keyParam);
    #endregion

    /// <summary>
    /// Класс исключения для хранения дополнительно 
    /// </summary>
    public class HException : Exception
    {
        /// <summary>
        /// Целочисленное значение детализирующее исключение пользователя
        /// </summary>
        public int m_code;
        /// <summary>
        /// Конструктор - основной (с параметрами)
        /// </summary>
        /// <param name="code">Дополнительный код исключения</param>
        /// <param name="msg">Сообщение для создания объекта базового класса</param>
        public HException(int code, string msg)
            : base(msg)
        {
            m_code = code;
        }
    }
    /// <summary>
    /// Перечисление - идентификатор ошибки
    /// </summary>
    public enum Errors
    {
        NoError,
        InvalidValue,
        NoAccess,
        NoSet,
        ParseError,
    }

    //public enum TYPE_DATABASE_CFG { CFG_190, CFG_200, COUNT };

    /// <summary>
    /// Базовый класс для класса приложения
    /// </summary>
    public static class ProgramBase
    {
        /// <summary>
        /// Перечисление - идентификаторы приложений из состава ИС Статистика
        /// </summary>
        public enum ID_APP { STATISTIC = 1, TRANS_GTP, TRANS_GTP_TO_NE22, TRANS_GTP_FROM_NE22, TRANS_BYISK_GTP_TO_NE22, TRANS_MODES_CENTRE, TRANS_MODES_CENTRE_GUI, TRANS_MODES_CENTRE_CMD, TRANS_MODES_TERMINALE, TRANS_TG }

        public static System.Globalization.CultureInfo ss_MainCultureInfo = new System.Globalization.CultureInfo(@"ru-Ru");

        public static string MessageWellcome = "***************Старт приложения...***************"
            , MessageExit = "***************Выход из приложения...***************"
            , MessageAppAbort = @"Приложение будет закрыто...";

        public const Int32 TIMER_START_INTERVAL = 666;

        /// <summary>
        /// Журналирование старта приложения
        /// </summary>
        /// <param name="bGUI">Признак наличия графического интерфейса с пользователем</param>
        public static void Start(Logging.LOG_MODE log_mode, bool bGUI)
        {
            int iSetMode = -1;

            if (bGUI == true) {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
            } else
                ;

            //Установка разделителя целой и дробной части
            ss_MainCultureInfo.NumberFormat.NumberDecimalSeparator = @",";

            Thread.CurrentThread.CurrentCulture =
            Thread.CurrentThread.CurrentUICulture =
                ProgramBase.ss_MainCultureInfo;

            s_iMessageShowUnhandledException = 1;
            s_iMessageShowUnhandledExceptionDetail = 1;

            Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            Logging.DelegateProgramAbort = Abort;
            Logging.SetMode (log_mode);            
            if (Logging.s_mode == Logging.LOG_MODE.DB)
                Logging.DbWriter = new ASUTP.Database.Writer ();
            else
                ;

            Logging.Logg().PostStart(MessageWellcome);
        }
        /// <summary>
        /// Журналирование завершения приложения
        /// </summary>
        public static void Exit()
        {
            List<Form> listApplicationOpenForms = new List<Form>();
            foreach (Form f in Application.OpenForms)
                listApplicationOpenForms.Add(f);

            foreach (Form f in listApplicationOpenForms) {
                if (f is IFormMainBase)
                    (f as IFormMainBase).Close(true);
                else
                    if (f is IFormWait) {
                    //Здесь м. возникнуть ошибка -
                    // вызов формы из потока в котором форма не была создана ???
                    (f as IFormWait).StopWaitForm();
                } else
                    f.Close();
            }

            Logging.Logg().PostStop(MessageExit);
            Logging.Logg().Stop();

            DbSources.Sources().UnRegister();

            Application.Exit(new System.ComponentModel.CancelEventArgs(true));

            HCmd_Arg.SingleInstance.ReleaseMtx();
        }

        /// <summary>
        /// Оброботчик исключений в потоках и запись их в лог
        /// </summary>
        static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            string strHeader = "ProgramBase::Application_ThreadException () - ...";
            if (s_iMessageShowUnhandledException > 0)
                MessageBox.Show((IWin32Window)null, e.Exception.Message + Environment.NewLine + MessageAppAbort, strHeader);
            else
                ;

            // here you can log the exception ...
            Logging.Logg().Exception(e.Exception, strHeader, Logging.INDEX_MESSAGE.NOT_SET);

            Exit();
        }

        static void Application_ExceptionSingleInstance(object sender)
        {
        }

        /// <summary>
        /// Оборботчик не перехваченного исключения в текущем домене и запись их в лог
        /// </summary>
        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            fAppDomain_UnhandledException(sender, e, true, true);
        }

#if _SEPARATE_APPDOMAIN
        /// <summary>
        /// Оборботчик не перехваченного исключения в текущем домене и запись их в лог
        /// </summary>
        public static void SeparateAppDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            fAppDomain_UnhandledException(sender, e, false, false);
        }
#else
#endif

        /// <summary>
        /// Оборботчик не перехваченного исключения в текущем домене и запись их в лог
        /// </summary>
        static void fAppDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e, bool bMessageShow, bool bExit)
        {
            string strHeader = "ProgramBase::fAppDomain_UnhandledException (AppDomain = " + (sender as AppDomain).FriendlyName + @") - ..."
                , strBody = string.Empty;
            if (s_iMessageShowUnhandledException > 0)
                if (s_iMessageShowUnhandledExceptionDetail > 0)
                    strBody = (e.ExceptionObject as Exception).ToString();
                else
                    strBody = (e.ExceptionObject as Exception).Message;
            else
                ;

            if (bMessageShow == true)
                MessageBox.Show((IWin32Window)null, strBody + Environment.NewLine + MessageAppAbort, strHeader);
            else
                ;

            // here you can log the exception ...
            Logging.Logg().Exception(e.ExceptionObject as Exception, strHeader, Logging.INDEX_MESSAGE.NOT_SET);

            if (bExit == true)
                Exit();
            else
                ;
        }

        //???
        public static void Abort() { }

        public static int s_iAppID = -1;
        public static int s_iMessageShowUnhandledException = -1;
        public static int s_iMessageShowUnhandledExceptionDetail = -1;

        /// <summary>
        /// Запись в  лог имени проложения
        /// </summary>
        public static string AppName
        {
            get
            {
                return Logging.AppName + ".exe";
            }
        }

        /// <summary>
        /// Возвращает версию продукта с датой
        /// </summary>
        public static string AppProductVersion
        {
            get
            {
                return Application.ProductVersion.ToString()/*StatisticCommon.Properties.Resources.TradeMarkVersion*/
                    + @" (" + File.GetLastWriteTime(Application.ExecutablePath).ToString(@"dd.MM.yyyy HH:mm:ss") + @")";
            }
        }

        /// <summary>
        /// Функция завершения приложения
        /// </summary>
        public static void AppExit()
        {
            string commandLineArgs = getCommandLineArgs();
            string exePath = Application.ExecutablePath;
            try {
                Application.Exit();
                wait_allowingEvents(Constants.MAX_WATING);
            } catch (ArgumentException ex) {
                throw;
            }
        }

        /// <summary>
        /// Функция перезапуска приложения
        /// </summary>
        public static void AppRestart()
        {
            string commandLineArgs = getCommandLineArgs();
            string exePath = Application.ExecutablePath;
            try {
                Application.Exit();
                wait_allowingEvents(Constants.MAX_WATING);
            } catch (ArgumentException ex) {
                throw;
            }

            Process.Start(exePath, commandLineArgs);
        }

        /// <summary>
        /// Формируек командную строку для запуска приложения
        ///   (исключая путь к образу исполняемого файла)
        /// </summary>
        static string getCommandLineArgs()
        {
            Queue<string> args = new Queue<string>(Environment.GetCommandLineArgs());
            args.Dequeue(); // args[0] is always exe path/filename
            return string.Join(" ", args.ToArray());
        }

        /// <summary>
        /// Обработчик сообщений в очереди
        /// </summary>
        static void wait_allowingEvents(int durationMS)
        {
            DateTime start = DateTime.Now;
            do {
                Application.DoEvents();
            } while (start.Subtract(DateTime.Now).TotalMilliseconds > durationMS);
        }
    }
}