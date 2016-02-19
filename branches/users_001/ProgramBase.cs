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
    public delegate void DelegateFunc();
    public delegate void DelegateIntFunc(int param);
    public delegate void DelegateIntIntFunc(int param1, int param2);
    public delegate int IntDelegateIntIntFunc(int param1, int param2);
    public delegate void DelegateStringFunc(string param);
    public delegate void DelegateBoolFunc(bool param);
    public delegate void DelegateObjectFunc(object obj);
    public delegate void DelegateRefObjectFunc(ref object obj);
    public delegate void DelegateDateFunc(DateTime date);

    public delegate int IntDelegateFunc();
    public delegate int IntDelegateIntFunc(int param);

    public delegate string StringDelegateFunc();
    public delegate string StringDelegateIntFunc(int param);
    public delegate string StringDelegateStringFunc(string keyParam);

    public class HException : Exception
    {
        public int m_code;

        public HException(int code, string msg)
            : base(msg)
        {
            m_code = code;
        }
    }

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

        //Журналирование старта приложения
        public static void Start(bool bGUI = true)
        {
            if (bGUI == true)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
            }
            else
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
            else ;
            Logging.Logg().PostStart(MessageWellcome);
        }

        //Журналирование завершения приложения
        public static void Exit()
        {
            List<Form> listApplicationOpenForms = new List<Form>();
            foreach (Form f in Application.OpenForms)
                listApplicationOpenForms.Add(f);

            foreach (Form f in listApplicationOpenForms)
            {
                if (f is FormMainBase)
                    (f as FormMainBase).Close(true);
                else
                    if (f is FormWait)
                    {
                        //Здесь м. возникнуть ошибка -
                        // вызов формы из потока в котором форма не была создана ???
                        (f as FormWait).StopWaitForm();
                    }
                    else
                        f.Close();
            }

            Logging.Logg().PostStop(MessageExit);
            Logging.Logg().Stop();

            DbSources.Sources().UnRegister();

            System.ComponentModel.CancelEventArgs cancelEvtArgs = new System.ComponentModel.CancelEventArgs(true);
            Application.Exit(cancelEvtArgs);
        }

        /// <summary>
        /// Оброботчик исключений в потоках и запись их в лог
        /// </summary>
        static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            string strHeader = "ProgramBase::Application_ThreadException () - ...";
            if (s_iMessageShowUnhandledException > 0) MessageBox.Show((IWin32Window)null, e.Exception.Message + Environment.NewLine + MessageAppAbort, strHeader); else ;

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
            string strHeader = "ProgramBase::CurrentDomain_UnhandledException () - ..."
                , strBody = string.Empty;
            if (s_iMessageShowUnhandledException > 0)
                if (s_iMessageShowUnhandledExceptionDetail > 0)
                    strBody = (e.ExceptionObject as Exception).ToString();
                else
                    strBody = (e.ExceptionObject as Exception).Message;
            else ;

            MessageBox.Show((IWin32Window)null, strBody + Environment.NewLine + MessageAppAbort, strHeader);

            // here you can log the exception ...
            Logging.Logg().Exception(e.ExceptionObject as Exception, strHeader, Logging.INDEX_MESSAGE.NOT_SET);

            Exit();
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
            try
            {
                Application.Exit();
                wait_allowingEvents(6666);
            }
            catch (ArgumentException ex)
            {
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
            try
            {
                Application.Exit();
                wait_allowingEvents(6666);
            }
            catch (ArgumentException ex)
            {
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
            do
            {
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
        static public string cmd;
        /// <summary>
        /// параметр командной строки
        /// </summary>
        static public string param;

        /// <summary>
        /// Основной конструктор класса
        /// </summary>
        /// <param name="args">параметры командной строки</param>
        public HCmd_Arg(string[] args)
        {
            handlerArgs(args);
            if (!SingleInstance.IsOnlyInstance)
                execCmdLine(cmd);
            else
                if (cmd == "stop")
                    execCmdLine(cmd);
                else ;
        }

        /// <summary>
        /// обработка CommandLine
        /// </summary>
        /// <param name="cmdLine">командная строка</param>
        static private void handlerArgs(string[] cmdLine)
        {
            string[] m_cmd = new string[cmdLine.Length];

            if (m_cmd.Length > 1)
            {
                m_cmd = cmdLine[1].Split('/', '=');

                if (m_cmd.Length > 2)
                {
                    cmd = m_cmd[1];
                    param = m_cmd[2];
                }
                else
                    cmd = m_cmd[1];
            }
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

                    object[] attributesTitle = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                    //if (attributesTitle.Length > 0)
                    //{
                    //    AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributesTitle[0];
                    //    if (titleAttribute.Title != "")
                    //        nameGUID = nameGUID + " " + titleAttribute.Title;
                    //}
                    //else
                    nameGUID = Assembly.GetEntryAssembly().CodeBase;

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
                    if (attributes.Length > 0)
                    {
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
            static private string m_mtxName = ProgramInfo.NameMtx.ToString();
            static Mutex m_mtx;

            /// <summary>
            /// Проверка на повторный запуск
            /// </summary>
            static public bool IsOnlyInstance
            {
                get
                {
                    bool onlyInstance;
                    m_mtx = new Mutex(true, m_mtxName, out onlyInstance);
                    return onlyInstance;
                }
            }

            /// <summary>
            /// Отправка сообщения приложению
            /// для его активации
            /// </summary>
            /// <param name="hWnd">дескриптор окна</param>
            static private void sendMsg(IntPtr hWnd)
            {
                WinApi.SendMessage(hWnd, WinApi.SW_RESTORE, IntPtr.Zero, IntPtr.Zero);
            }

            /// <summary>
            /// освобождение мьютекса
            /// </summary>
            static public void ReleaseMtx()
            {
                m_mtx.ReleaseMutex();
            }

            /// <summary>
            /// Остановка работы основного приложения
            /// </summary>
            static public void StopApp()
            {
                WinApi.SendMessage(mainhWnd, WinApi.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
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
                    IntPtr m_hWnd = IntPtr.Zero;
                    Process process = Process.GetCurrentProcess();
                    Process[] processes = Process.GetProcessesByName(process.ProcessName);
                    foreach (Process _process in processes)
                    {
                        // Get the first instance that is not this instance, has the
                        // same process name and was started from the same file name
                        // and location. Also check that the process has a valid
                        // window handle in this session to filter out other user's
                        // processes.
                        if (_process.Id != process.Id &&
                            _process.MainModule.FileName == process.MainModule.FileName &&
                            _process.Handle != IntPtr.Zero)
                        {
                            m_hWnd = _process.MainWindowHandle;

                            if (m_hWnd == IntPtr.Zero)
                                m_hWnd = enumID(_process.Id);
                            else ;
                            break;
                        }
                    }
                    return m_hWnd;
                }
            }

            /// <summary>
            /// выборка всех запущенных приложений
            /// </summary>
            /// <param name="id">ид процесса приложения</param>
            /// <returns>дескриптор окна</returns>
            static private IntPtr enumID(int id)
            {
                IntPtr hwnd = IntPtr.Zero;
                bool flg = true;
                WinApi.EnumWindows((hWnd, lParam) =>
                {
                    if (WinApi.IsWindowVisible(hWnd) && (WinApi.GetWindowTextLength(hWnd) != 0))
                    {
                        if (WinApi.IsIconic(hWnd) != 0 &&
                            WinApi.GetPlacement(hWnd).showCmd == WinApi.ShowWindowCommands.Minimized
                            && flg)
                            hwnd = findCurProc(id, hWnd, out flg);
                        else ;
                    }
                    return true;
                }, IntPtr.Zero);

                return hwnd;
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
                sendMsg(hWnd);

                if (hWnd != IntPtr.Zero)
                {
                    // Restore window if minimised. Do not restore if already in
                    // normal or maximised window state, since we don't want to
                    // change the current state of the window.
                    if (WinApi.IsIconic(hWnd) != 0)
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
            /// <param name="hwd">дескриптор окна</param>
            ///  <param name="flg">флаг остановки посика хандлера</param>
            /// <returns>дескриптор окна</returns>
            static private IntPtr findCurProc(int id, IntPtr hwd, out bool flg)
            {
                int _ProcessId;
                WinApi.GetWindowThreadProcessId(hwd, out _ProcessId);

                if (id == _ProcessId)
                {
                    flg = false;
                    return hwd;
                }
                else
                {
                    flg = true;
                    return IntPtr.Zero;
                }
            }
        }

        /// <summary>
        /// Обработка команды старт/стоп
        /// </summary>
        /// <param name="CmdStr">команда приложению</param>
        static public void execCmdLine(string CmdStr)
        {
            switch (CmdStr)
            {
                case "start":
                    if (!(SingleInstance.IsOnlyInstance))
                    {
                        SingleInstance.SwitchToCurrentInstance();
                        SingleInstance.InterruptReApp();
                    }
                    break;
                case "stop":
                    if (!(SingleInstance.IsOnlyInstance))
                    {
                        SingleInstance.StopApp();
                        SingleInstance.InterruptReApp();
                    }
                    else
                        SingleInstance.InterruptReApp();
                    break;
                default:
                    if (!(SingleInstance.IsOnlyInstance))
                    {
                        SingleInstance.SwitchToCurrentInstance();
                        SingleInstance.InterruptReApp();
                    }
                    break;
            }
        }

        public void Dispose()
        {
        }
    }
}