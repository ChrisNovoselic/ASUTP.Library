using ASUTP.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace ASUTP.Helper {
    /// <summary>
    /// Класс обработки камандной строки
    /// </summary>
    public class HCmd_Arg : IDisposable {
        /// <summary>
        /// Значения командной строки
        /// </summary>
        protected static Dictionary<string, string> m_dictCmdArgs;
        ///// <summary>
        ///// параметр командной строки
        ///// </summary>
        //static public string param;

        /// <summary>
        /// Основной конструктор класса
        /// </summary>
        /// <param name="args">Параметры командной строки</param>
        public HCmd_Arg (string [] args)
        {
            handlerArgs (args);

            bool bIsOnlyInstance = SingleInstance.IsOnlyInstance;

            if (m_dictCmdArgs.ContainsKey ("stop") == true)
                execCmdLine (false, bIsOnlyInstance);
            else
                if (bIsOnlyInstance == false)
                execCmdLine (true, bIsOnlyInstance);
            else
                ;
        }

        /// <summary>
        /// Обработка CommandLine - формирование словаря со значениями
        /// </summary>
        /// <param name="cmdLine">Параметры командной строки</param>
        protected static void handlerArgs (string [] cmdLine)
        {
            string [] args = null
                , arCmdPair = null;
            string key = string.Empty
                , value = string.Empty
                , cmd = string.Empty
                , cmdPair = string.Empty;

            m_dictCmdArgs = new Dictionary<string, string> ();

            if (cmdLine.Length > 1) {
                args = new string [cmdLine.Length - 1];

                for (int i = 1; i < cmdLine.Length; i++) {
                    cmd = cmdLine [i];

                    if (cmd.IndexOf ('/') == 0) {
                        cmdPair = cmd.Substring (1);
                        arCmdPair = cmdPair.Split ('=');
                        if (arCmdPair.Length > 0) {
                            key = arCmdPair [0];

                            if (arCmdPair.Length > 1)
                                value = arCmdPair [1];
                            else
                                value = string.Empty;
                        } else {
                            key = string.Empty;
                            value = string.Empty;
                        }

                        m_dictCmdArgs.Add (key, value);
                    } else {//параметр не учитывается - ошибка
                        m_dictCmdArgs.Add (@"stop", string.Empty);

                        break;
                    }
                }
            } else
                ; //нет ни одного аргумента
        }

        /// <summary>
        /// Класс для создания спец имени для мьютекса
        /// </summary>
        static public class ProgramInfo {
            /// <summary>
            /// Создание GUID для приложения
            /// </summary>
            /// <returns></returns>
            static public string NewGuid ()
            {
                Guid guid = Guid.NewGuid ();
                return guid.ToString ();
            }

            /// <summary>
            /// Строковя уникальная строка для наименования мьютекса
            ///  (из пути запускаемого приложения)
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
                    nameGUID = Assembly.GetEntryAssembly ().CodeBase;
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
                    object [] attributes = Assembly.GetEntryAssembly ().GetCustomAttributes (typeof (AssemblyTitleAttribute), false);

                    if (attributes.Length > 0) {
                        AssemblyTitleAttribute assTitleAttr = (AssemblyTitleAttribute)attributes [0];
                        if (string.IsNullOrEmpty (assTitleAttr.Title) == false)
                            return assTitleAttr.Title;
                        else
                            ;
                    } else
                        ;

                    return System.IO.Path.GetFileNameWithoutExtension (Assembly.GetEntryAssembly ().CodeBase);
                }
            }
        }

        /// <summary>
        /// Класс по работе с запущенным приложением
        /// </summary>
        static public class SingleInstance {
            static private string s_NameMutex = ProgramInfo.NameMtx.ToString ();
            static Mutex s_mutex;

            /// <summary>
            /// Проверка на повторный запуск
            /// </summary>
            static public bool IsOnlyInstance
            {
                get
                {
                    bool bRes;

                    Logging.Logg ().Debug (@"SingleInstance::IsOnlyInstance - s_NameMutex = " + s_NameMutex, Logging.INDEX_MESSAGE.NOT_SET);

                    try {
                        s_mutex = new Mutex (true, s_NameMutex, out bRes);
                    } catch { bRes = false; }

                    return bRes;
                }
            }

            /// <summary>
            /// Отправка сообщения приложению
            /// для его активации
            /// </summary>
            /// <param name="hWnd">дескриптор окна</param>
            static private void sendMsg (IntPtr hWnd, int iMsg, IntPtr wParam)
            {
                //Logging.Logg().Debug(@"SingleInstance::sendMsg () - to Ptr=" + hWnd + @"; iMsg=" + iMsg + @" ...", Logging.INDEX_MESSAGE.NOT_SET);

                WinApi.SendMessage (hWnd, iMsg, wParam, IntPtr.Zero);
            }

            static private void postMsg (IntPtr hWnd, uint iMsg, IntPtr wParam)
            {
                //Logging.Logg().Debug(@"SingleInstance::sendMsg () - to Ptr=" + hWnd + @"; iMsg=" + iMsg + @" ...", Logging.INDEX_MESSAGE.NOT_SET);

                WinApi.PostMessage (hWnd, iMsg, wParam, IntPtr.Zero);
            }

            /// <summary>
            /// освобождение мьютекса
            /// </summary>
            static public void ReleaseMtx ()
            {
                try {
                    s_mutex?.ReleaseMutex ();
                } catch { }
            }

            /// <summary>
            /// Остановка работы дублирующего приложения
            /// </summary>
            static public void StopApp ()
            {
                sendMsg (HandleWnd, WinApi.WM_CLOSE, (IntPtr)WinApi.SC_CLOSE);
            }

            /// <summary>
            /// Прерывание запуска основного(текущего) приложения
            /// </summary>
            static public void InterruptReApp ()
            {
                Environment.Exit (0);
            }

            /// <summary>
            /// Дескриптор окна дублирующего процесса
            /// </summary>
            static public IntPtr HandleWnd
            {
                get
                {
                    IntPtr hWndRes = IntPtr.Zero;
                    Process cur_process = Process.GetCurrentProcess ();
                    Process [] processes = Process.GetProcessesByName (cur_process.ProcessName);
                    foreach (Process process in processes)
                        // Get the first instance that is not this instance, has the
                        // same process name and was started from the same file name
                        // and location. Also check that the process has a valid
                        // window handle in this session to filter out other user's
                        // processes.
                        try {
                            if ((!(process.Id == cur_process.Id))
                                && (process.MainModule.FileName == cur_process.MainModule.FileName)
                                && (process.Handle.Equals (IntPtr.Zero) == false)) {
                                hWndRes = process.MainWindowHandle;

                                if (hWndRes == IntPtr.Zero)
                                    hWndRes = getWindowThreadProcessId (process.Id);
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
            /// Возвратить дескриптор окна по идентификатору процесса
            /// </summary>
            /// <param name="id">Идентификатор процесса</param>
            /// <returns>Дескриптор (главного) окна приложения</returns>
            static private IntPtr getWindowThreadProcessId (int id)
            {
                IntPtr hWndRes = IntPtr.Zero;
                bool bIsSuccess = false;

                // Функция для перечисления всех окон
                // , при этом при выполнении набора условий
                //  происходит проверка: принадлежит ли процессу(по идентификатору) окно(по дескриптору) 
                WinApi.EnumWindowsProcDel winEnumerateProc = (wParam, lParam) => {
                    if ((WinApi.IsWindowVisible (wParam) == true)
                        && (!(WinApi.GetWindowTextLength (wParam) == 0))) {
                        if ((!(WinApi.IsIconic (wParam) == 0))
                            && (WinApi.GetPlacement (wParam).showCmd == WinApi.ShowWindowCommands.Minimized)
                            && (bIsSuccess == false))
                            hWndRes = getWindowThreadProcessId (id, wParam, out bIsSuccess);
                        else
                            ;
                    } else
                        ;

                    return true;
                };

                WinApi.EnumWindows (winEnumerateProc, IntPtr.Zero);

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
            static public void SwitchToCurrentInstance ()
            {
                IntPtr hWnd = HandleWnd;
                sendMsg (hWnd, WinApi.SW_RESTORE, IntPtr.Zero);

                if (hWnd.Equals (IntPtr.Zero) == false) {
                    // Restore window if minimised. Do not restore if already in
                    // normal or maximised window state, since we don't want to
                    // change the current state of the window.
                    if (!(WinApi.IsIconic (hWnd) == 0))
                        WinApi.ShowWindow (hWnd, WinApi.SW_RESTORE);
                    else
                        ;
                    // Set foreground window.
                    WinApi.SetForegroundWindow (hWnd);
                } else
                    ;
            }

            /// <summary>
            /// Возвратить дескриптор окна, если процесс является его владельцем, иначе null
            /// </summary>
            /// <param name="id">Идентификатор приложения</param>
            /// <param name="hWnd">Дескриптор окна для проверки</param>
            ///  <param name="bIsOwner">Признак совпадения идентификаторов главных потоков (???процессов) переданного в 1-ом аргументе и владельца окна, переданного во 2-ом аргументе</param>
            /// <returns>Дескриптор окна</returns>
            static private IntPtr getWindowThreadProcessId (int id, IntPtr hWnd, out bool bIsOwner)
            {
                IntPtr hWndRes = IntPtr.Zero;

                int owner_id;
                WinApi.GetWindowThreadProcessId (hWnd, out owner_id);

                bIsOwner = id == owner_id;

                if (bIsOwner == true)
                    hWndRes = hWnd;
                else
                    ;

                return hWndRes;
            }
        }

        /// <summary>
        /// Обработка команды старт/стоп
        /// </summary>
        /// <param name="bIsExecute">Признак продолжения выполнения текущего экземпляра</param>
        /// <param name="bIsOnlyInstance">Признак уникальности текущего экземпляра</param>
        static public void execCmdLine (bool bIsExecute, bool bIsOnlyInstance)
        {
            if (bIsExecute == true) {
                if (bIsOnlyInstance == false) {
                    SingleInstance.SwitchToCurrentInstance ();
                    SingleInstance.InterruptReApp ();
                } else
                    ;
            } else
                if (bIsExecute == false) {
                if (bIsOnlyInstance == false)
                    SingleInstance.StopApp ();
                else
                    ;

                SingleInstance.InterruptReApp ();
            } else
                ;
        }

        public void Dispose ()
        {
        }
    }
}
