using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection;

using System.Threading;
using System.Diagnostics; //Process

namespace HClassLibrary
{
    #region Типы делегатов ???заменить на Action<>, Func
    /// <summary>
    /// Тип для делегата без аргументов и без возвращаемого значения
    /// </summary>
    public delegate void DelegateFunc();
    /// <summary>
    /// Тип для делегата с аргументом типа 'int' и без возвращаемого значения
    /// </summary>
    /// <param name="param">Аргумент 1</param>
    public delegate void DelegateIntFunc(int param);
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

    public static class ProgramBase
    {
        public enum ID_APP { STATISTIC = 1, TRANS_GTP, TRANS_GTP_TO_NE22, TRANS_GTP_FROM_NE22, TRANS_BYISK_GTP_TO_NE22, TRANS_MODES_CENTRE, TRANS_MODES_CENTRE_GUI, TRANS_MODES_CENTRE_CMD, TRANS_MODES_TERMINALE, TRANS_TG }

        public static System.Globalization.CultureInfo ss_MainCultureInfo = new System.Globalization.CultureInfo(@"ru-Ru");

        public static string MessageWellcome = "***************Старт приложения...***************"
            , MessageExit = "***************Выход из приложения...***************"
            , MessageAppAbort = @"Приложение будет закрыто...";

        public const Int32 TIMER_START_INTERVAL = 666;

        /// <summary>
        /// Журналирование старта приложения
        /// </summary>
        /// <param name="bGUI">Признак наличия интерфйса с пользователем</param>
        public static void Start(bool bGUI = true)
        {
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

            if (Logging.s_mode == Logging.LOG_MODE.UNKNOWN)
                Logging.s_mode = Logging.LOG_MODE.FILE_EXE;
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
                if (f is FormMainBase)
                    (f as FormMainBase).Close(true);
                else
                    if (f is FormWait) {
                    //Здесь м. возникнуть ошибка -
                    // вызов формы из потока в котором форма не была создана ???
                    (f as FormWait).StopWaitForm();
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
                wait_allowingEvents(6666);
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
                wait_allowingEvents(6666);
            } catch (ArgumentException ex) {
                throw;
            }
            Process.Start(exePath, commandLineArgs);
        }

        /// <summary>
        /// Формируек командную строку для запуска приложения
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

    /// <summary>
    /// Класс обработки камандной строки
    /// </summary>
    public class HCmd_Arg : IDisposable
    {
        /// <summary>
        /// значения командной строки
        /// </summary>
        protected static Dictionary<string, string> m_dictCmdArgs;
        ///// <summary>
        ///// параметр командной строки
        ///// </summary>
        //static public string param;

        /// <summary>
        /// Основной конструктор класса
        /// </summary>
        /// <param name="args">параметры командной строки</param>
        public HCmd_Arg(string[] args)
        {
            handlerArgs(args);

            bool bIsOnlyInstance = SingleInstance.IsOnlyInstance;

            if (m_dictCmdArgs.ContainsKey("stop") == true)
                execCmdLine(false, bIsOnlyInstance);
            else
                if (bIsOnlyInstance == false)
                    execCmdLine(true, bIsOnlyInstance);
                else
                    ;
        }

        /// <summary>
        /// обработка CommandLine
        /// </summary>
        /// <param name="cmdLine">командная строка</param>
        protected static void handlerArgs(string[] cmdLine)
        {
            string[] args = null
                , arCmdPair = null;
            string key = string.Empty
                , value = string.Empty
                , cmd = string.Empty
                , cmdPair = string.Empty;

            m_dictCmdArgs = new Dictionary<string, string>();

            if (cmdLine.Length > 1) {
                args = new string[cmdLine.Length - 1];

                for (int i = 1; i < cmdLine.Length; i++) {
                    cmd = cmdLine[i];

                    if (cmd.IndexOf('/') == 0) {
                        cmdPair = cmd.Substring(1);
                        arCmdPair = cmdPair.Split('=');
                        if (arCmdPair.Length > 0) {
                            key = arCmdPair[0];

                            if (arCmdPair.Length > 1)
                                value = arCmdPair[1];
                            else
                                value = string.Empty;
                        } else {
                            key = string.Empty;
                            value = string.Empty;
                        }

                        m_dictCmdArgs.Add(key, value);
                    } else {//параметр не учитывается - ошибка
                        m_dictCmdArgs.Add(@"stop", string.Empty);

                        break;
                    }
                }
            } else
                ; //нет ни одного аргумента
        }

        /// <summary>
        /// Класс для создания спец имени для мьютекса
        /// </summary>
        static public class ProgramInfo
        {
            /// <summary>
            /// Создание GUID для приложения
            /// </summary>
            /// <returns></returns>
            static public string NewGuid()
            {
                Guid guid = Guid.NewGuid();
                return guid.ToString();
            }

            /// <summary>
            /// создвет имя мьютекса по пути запускаемого приложения
            /// </summary>
            static public string NameMtx
            {
                get
                {
                    string nameGUID;

                    //object[] attributes = Assembly.GetEntryAssembly().GetCustomAttributes((typeof(System.Runtime.InteropServices.GuidAttribute)), false);
                    //if (attributes.Length == 0)
                    //    return String.Empty;

                    //nameGUID = ((System.Runtime.InteropServices.GuidAttribute)attributes[0]).Value;

                    //object[] attributesTitle = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                    //if (attributesTitle.Length > 0)
                    //{
                    //    AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributesTitle[0];
                    //    if (titleAttribute.Title != "")
                    //        nameGUID = nameGUID + " " + titleAttribute.Title;
                    //}
                    //else
                    nameGUID = Assembly.GetEntryAssembly().CodeBase;
                    ////Вариант №2
                    //nameGUID = Application.ExecutablePath;

                    return nameGUID;
                }
            }

            /// <summary>
            /// Получение имени запускаемого файла
            /// </summary>
            static public string AssemblyTitle
            {
                get
                {
                    object[] attributes = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                    if (attributes.Length > 0) {
                        AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
                        if (titleAttribute.Title != "")
                            return titleAttribute.Title;
                    }
                    return System.IO.Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().CodeBase);
                }
            }
        }

        /// <summary>
        /// класс по работе с запущенным приложением
        /// </summary>
        static public class SingleInstance
        {
            static private string s_NameMutex = ProgramInfo.NameMtx.ToString();
            static Mutex s_mutex;

            /// <summary>
            /// Проверка на повторный запуск
            /// </summary>
            static public bool IsOnlyInstance
            {
                get
                {
                    bool bRes;
                    Logging.Logg().Debug(@"SingleInstance::IsOnlyInstance - m_mtxName = " + s_NameMutex, Logging.INDEX_MESSAGE.NOT_SET);
                    s_mutex = new Mutex(true, s_NameMutex, out bRes);
                    return bRes;
                }
            }

            /// <summary>
            /// Отправка сообщения приложению
            /// для его активации
            /// </summary>
            /// <param name="hWnd">дескриптор окна</param>
            static private void sendMsg(IntPtr hWnd, int iMsg, IntPtr wParam)
            {
                //Logging.Logg().Debug(@"SingleInstance::sendMsg () - to Ptr=" + hWnd + @"; iMsg=" + iMsg + @" ...", Logging.INDEX_MESSAGE.NOT_SET);

                WinApi.SendMessage(hWnd, iMsg, wParam, IntPtr.Zero);
            }

            static private void postMsg(IntPtr hWnd, uint iMsg, IntPtr wParam)
            {
                //Logging.Logg().Debug(@"SingleInstance::sendMsg () - to Ptr=" + hWnd + @"; iMsg=" + iMsg + @" ...", Logging.INDEX_MESSAGE.NOT_SET);

                WinApi.PostMessage(hWnd, iMsg, wParam, IntPtr.Zero);
            }

            /// <summary>
            /// освобождение мьютекса
            /// </summary>
            static public void ReleaseMtx()
            {
                s_mutex.ReleaseMutex();
            }

            /// <summary>
            /// Остановка работы основного приложения
            /// </summary>
            static public void StopApp()
            {
                sendMsg(mainhWnd, WinApi.WM_CLOSE, (IntPtr)WinApi.SC_CLOSE);
            }

            /// <summary>
            /// Прерывание запуска дублирующего приложения
            /// </summary>
            static public void InterruptReApp()
            {
                Environment.Exit(0);
            }

            /// <summary>
            /// поиск дескриптора по процессу
            /// </summary>
            /// <returns>дескриптор окна</returns>
            static public IntPtr mainhWnd
            {
                get
                {
                    IntPtr hWndRes = IntPtr.Zero;
                    Process cur_process = Process.GetCurrentProcess();
                    Process[] processes = Process.GetProcessesByName(cur_process.ProcessName);
                    foreach (Process process in processes)
                        // Get the first instance that is not this instance, has the
                        // same process name and was started from the same file name
                        // and location. Also check that the process has a valid
                        // window handle in this session to filter out other user's
                        // processes.
                        try {
                            if ((!(process.Id == cur_process.Id))
                                && (process.MainModule.FileName == cur_process.MainModule.FileName)
                                && (process.Handle.Equals(IntPtr.Zero) == false)) {
                                hWndRes = process.MainWindowHandle;

                                if (hWndRes == IntPtr.Zero)
                                    hWndRes = getWindowThreadProcessId(process.Id);
                                else
                                    ;

                                break;
                            } else
                                ;
                        } catch { }
                    
                    return hWndRes;
                }
            }

            /// <summary>
            /// выборка всех запущенных приложений
            /// </summary>
            /// <param name="id">ид процесса приложения</param>
            /// <returns>дескриптор окна</returns>
            static private IntPtr getWindowThreadProcessId(int id)
            {
                IntPtr hWndRes = IntPtr.Zero;
                bool bIsSuccess = false;

                WinApi.EnumWindows((wParam, lParam) => {
                    if ((WinApi.IsWindowVisible(wParam) == true)
                        && (!(WinApi.GetWindowTextLength(wParam) == 0))) {
                        if ((!(WinApi.IsIconic(wParam) == 0))
                            && (WinApi.GetPlacement(wParam).showCmd == WinApi.ShowWindowCommands.Minimized)
                            && (bIsSuccess == false))
                            hWndRes = getWindowThreadProcessId(id, wParam, out bIsSuccess);
                        else
                            ;
                    }

                    return true;
                }, IntPtr.Zero);

                return hWndRes;
            }

            ///// <summary>
            ///// Получение заголовка окна
            ///// </summary>
            ///// <param name="hWnd">дескриптор приложения</param>
            ///// <returns>заголовок окна</returns>
            //private static string getWindowText(IntPtr hWnd)
            //{
            //    int len = WinApi.GetWindowTextLength(hWnd) + 1;
            //    StringBuilder sb = new StringBuilder(len);
            //    len = WinApi.GetWindowText(hWnd, sb, len);
            //    return sb.ToString(0, len);
            //}

            /// <summary>
            /// Активация окна
            /// </summary>
            static public void SwitchToCurrentInstance()
            {
                IntPtr hWnd = mainhWnd;
                sendMsg(hWnd, WinApi.SW_RESTORE, IntPtr.Zero);

                if (hWnd.Equals(IntPtr.Zero) == false) {
                    // Restore window if minimised. Do not restore if already in
                    // normal or maximised window state, since we don't want to
                    // change the current state of the window.
                    if (!(WinApi.IsIconic(hWnd) == 0))
                        WinApi.ShowWindow(hWnd, WinApi.SW_RESTORE);
                    else
                        ;
                    // Set foreground window.
                    WinApi.SetForegroundWindow(hWnd);
                }
            }

            /// <summary>
            /// поиск нужного процесса
            /// </summary>
            /// <param name="id">идентификатор приложения</param>
            /// <param name="hWnd">дескриптор окна</param>
            ///  <param name="bIsOwner">флаг остановки посика хандлера</param>
            /// <returns>дескриптор окна</returns>
            static private IntPtr getWindowThreadProcessId(int id, IntPtr hWnd, out bool bIsOwner)
            {
                int owner_id;
                WinApi.GetWindowThreadProcessId(hWnd, out owner_id);

                if (id == owner_id) {
                    bIsOwner = true;
                    return hWnd;
                } else {
                    bIsOwner = false;
                    return IntPtr.Zero;
                }
            }
        }

        /// <summary>
        /// Обработка команды старт/стоп
        /// </summary>
        /// <param name="CmdStr">команда приложению</param>
        static public void execCmdLine(bool bIsExecute, bool bIsOnlyInstance)
        {
            if (bIsExecute == true) {
                if (bIsOnlyInstance == false) {
                    SingleInstance.SwitchToCurrentInstance();
                    SingleInstance.InterruptReApp();
                } else
                    ;
            } else
                if (bIsExecute == false) {
                    if (bIsOnlyInstance == false)
                        SingleInstance.StopApp();
                    else
                        ;

                    SingleInstance.InterruptReApp();
                } else
                    ;
        }

        public void Dispose()
        {
        }
    }
}