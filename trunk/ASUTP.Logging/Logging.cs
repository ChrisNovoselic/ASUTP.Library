using ASUTP.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace ASUTP
{
    public interface ILoggingDbWriter {
        bool IsConnect { get; }

        int Connect();

        void Disconnect ();

        void Exec (string message, out int err);

        object ConnSett
        {
            set;
        }
    }

    /// <summary>
    /// Класс для объекта, обеспечивающего журналирование событий приложения/библиотеки
    /// </summary>
    public class Logging //: Logging
    {
        /// <summary>
        /// Перечисление - возможные режимы журналирования (классификация по признаку конечного устойства для размещения фиксируемымых событий)
        /// </summary>
        public enum LOG_MODE {
            ERROR = -1,
            UNKNOWN,
            FILE_EXE,
            FILE_DESKTOP,
            FILE_LOCALDEV,
            FILE_NETDEV,
            /// <summary>
            /// База данных, должен быть установлен объект _dbWriter
            /// </summary>
            DB
        };
        /// <summary>
        /// Перечисление - возможные типы сообщений
        /// </summary>
        private enum ID_MESSAGE { START = 1, STOP, ACTION, DEBUG, EXCEPTION, EXCEPTION_DB, ERROR, WARNING };
        /// <summary>
        /// Перечисление - предустановленные подтипы сообщений
        /// </summary>
        public enum INDEX_MESSAGE {
            NOT_SET = -1
            , A_001, A_002, A_003 // 3 шт.
            , D_001, D_002, D_003, D_004, D_005, D_006 // 8 шт.
            , EXCPT_001, EXCPT_002, EXCPT_003, EXCPT_004 // 4 шт.
            , EXCPT_DB_001, EXCPT_DB_002, EXCPT_DB_003, EXCPT_DB_004 // 4 шт.
            , ERR_001, ERR_002, ERR_003, ERR_004, ERR_005, ERR_006
            , W_001, W_002, W_003, W_004 // 4 шт.
                    , COUNT_INDEX_MESSAGE
        };

        public static ILoggingDbWriter DbWriter;

        public static Action DelegateProgramAbort; // ProgramBase.Abort

        private int MAX_COUNT_MESSAGE_ONETIME = 66;
        private int MAXCOUNT_LISTQUEUEMESSAGE = 666;

        private static string s_strDatetimeFrmt = @"yyyyMMdd HH:mm:ss.fff";

        private string m_fileNameStart;
        private string m_fileName;
        //private bool externalLog;
        //private DelegateStringFunc delegateUpdateLogText;
        //private DelegateFunc delegateClearLogText;
        private Semaphore sema;
        private LogStreamWriter m_sw;
        private FileInfo m_fi;
        private static LOG_MODE _mode = LOG_MODE.UNKNOWN;
        /// <summary>
        /// Режим работы журналирования
        /// </summary>
        public static LOG_MODE s_mode
        {
            get
            {
                return _mode;
            }

            set
            {
                _mode = value;

                switch (_mode) {
                    case LOG_MODE.DB:
                        s_strDatetimeFrmt = @"yyyyMMdd HH:mm:ss.fff";
                        break;
                    default:
                        s_strDatetimeFrmt = @"dd.MM.yyyy HH:mm:ss.fff";
                        break;
                }
            }
        }
        private bool logRotate = true;
        public const int MAX_ROTATE_COUNT = 6;
        public const int DEFAULT_SIZE_FILE = 5;
        public const int MAX_SIZE_FILE = 10;
        private static int logRotateSizeDefault = (int)Math.Floor ((double)(1024 * 1024 * DEFAULT_SIZE_FILE)); //1024 * 1024 * 5;
        private static int logRotateSizeMax = (int)Math.Floor ((double)(1024 * 1024 * MAX_SIZE_FILE)); //1024 * 1024 * 1024;
        private int logRotateSize;
        /// <summary>
        /// Дата/время крайней проверки размера файла (для окончания записи)
        /// </summary>
        private DateTime logRotateCheckLast;
        /// <summary>
        /// Макс. кол-во сек. между проверки размера файла (для окончания записи)
        /// </summary>
        private int logRotateChekMaxSeconds;
        /// <summary>
        /// Строка - содержание разделителя в журнале между меткой времени и непосредственно сообщением
        /// </summary>
        public static string DatetimeStampSeparator = new string ('-', 49); //"------------------------------------------------";
        /// <summary>
        /// Строка - содержание разделителя в журнале между меткой времени и непосредственно сообщением
        /// </summary>
        public static string MessageSeparator = new string ('=', 49); //"================================================";

        protected static Logging m_this = null;

        private object m_objQueueMessage;
        /// <summary>
        /// Список сообщений, ожидающих(не обработанных) размещения в журнале
        /// </summary>
        private static List<MESSAGE> m_listQueueMessage;

        /// <summary>
        /// Объект с параметрами для соедиения с БД при режиме журналирования "БД"
        /// </summary>
        public object ConnSett
        {
            //get { return s_connSett; }
            set
            {
                DbWriter.ConnSett = value;
            }
        }

        /// <summary>
        /// Установить режим работы объекта
        /// </summary>
        /// <param name="log_mode">Значение для режима работы с низким приоритетом. Устанавливается при отсутствии режима в командной строке</param>
        /// <param name="LOG_KEY">Аргумент из состава командной строки - ключ для поиска режима журналирования</param>        
        /// <returns>Результат установки режима работы</returns>
        public static void SetMode (LOG_MODE log_mode, string LOG_KEY = @"log=")
        {
            int iSetMode = 0;

            LOG_MODE cmd_log_mode = LOG_MODE.UNKNOWN;

            if (LOG_KEY.Equals (string.Empty) == false) {
                var arg_log = from arg in Environment.GetCommandLineArgs () where !(arg.IndexOf (LOG_KEY) < 0) select arg;

                if (arg_log.Count () == 1)
                    if (Enum.IsDefined (typeof (Logging.LOG_MODE), arg_log.ElementAt (0).Substring (arg_log.ElementAt (0).IndexOf (LOG_KEY) + LOG_KEY.Length)) == true)
                        cmd_log_mode = (Logging.LOG_MODE)Enum.Parse (typeof (Logging.LOG_MODE), arg_log.ElementAt (0).Substring (arg_log.ElementAt (0).IndexOf (LOG_KEY) + LOG_KEY.Length));
                    else
                        iSetMode = -1; // режим не распознан
                else
                    if (arg_log.Count () == 0)
                        iSetMode = 2; // режим не указан - значение по умолчанию
                    else
                        iSetMode = -2; // режим указан несколко раз - значение по умолчанию
            } else
                iSetMode = 1; // режим не указан - ключ для поиска аргумента не найден

            //Если назначить неизвестный(UNKNOWN) тип логирования - 1-е сообщения б. утеряны
            Logging.s_mode = cmd_log_mode;

            // попытка разобрать командную строку - найти указание на режим журналирования
            if (iSetMode  < 0) {
            // ошибка разбора
                DelegateProgramAbort?.Invoke ();
            } else if (iSetMode > 0) {
            // разбор выполнен - режим не установлен
                Logging.s_mode = log_mode;
            } else
            // режим установлен из командной строки
                Logging.s_mode = cmd_log_mode;
        }

        private Thread m_threadPost;
        private
            System.Threading.Timer
                //System.Windows.Forms.Timer
                m_timerDbConnect
                ;
        private ManualResetEvent [] m_arEvtThread;
        private ManualResetEvent m_evtIsDbConnect;
        /// <summary>
        /// Массив для хранения пользовательских индексов/идентификаторов подтипов сообщений
        /// , связанных с предустановленными индексами/идентификаторами
        /// </summary>
        private static int [] s_arDebugLogMessageIds = new int [(int)INDEX_MESSAGE.COUNT_INDEX_MESSAGE];
        /// <summary>
        /// Набор признаков для указания признаков необходимости размещения подтипов(INDEX_MESSAGE) сообщений в журнале
        /// </summary>
        private static Core.HMark s_markDebugLog = new Core.HMark (0);
        /// <summary>
        /// Делегат для определения пользовательской конфигурации размещения подтипов сообщений (по целочисленному идентификатору)
        /// </summary>
        public static Core.StringDelegateIntFunc DelegateGetINIParametersOfID;
        /// <summary>
        /// Делегат для определения пользовательской конфигурации размещения подтипов сообщений (по строковому идентификатору)
        /// </summary>
        public static Core.StringDelegateStringFunc DelegateGetINIParametersOfKEY;

        /// <summary>
        /// Установить связь  пользовательским и предустановленным индексами
        /// , для учета пользовательской конфигурации
        /// </summary>
        /// <param name="indx">Предустановленный индекс подтипа сообщений</param>
        /// <param name="id">Пользовательский индекс (подтип) сообщений.
        ///  Пользователь должен определить метод, возвращающий признак необходимости размещения сообщений этого подтипа в журнале.
        ///  Если метод не определен, сообщения подтипа в журнал размещаются
        ///  , если нет, то в соответсвии с возвращаемым значением (пользовательской конфигурацией).</param>
        public static void LinkId (INDEX_MESSAGE indx, int id)
        {
            linked (indx, id);
        }

        /// <summary>
        /// Разорвать связь между пользовательским и предустановленным индексами
        /// , отменить пользовательскую конфигурацию по отображению указанного в аргументе подтипа сообщения.
        ///  Означает безусловное размещение в журнале сообщений этого подтипа.
        /// </summary>
        /// <param name="indx">Индекс подтипа сообщения</param>
        public static void UnLink (INDEX_MESSAGE indx)
        {
            linked (indx, (int)INDEX_MESSAGE.NOT_SET);
        }

        private static void linked (INDEX_MESSAGE indx, int id)
        {
            s_arDebugLogMessageIds [(int)indx] = id;

            updateMarkDebugLog (indx);
        }

        public int AppId
        {// ProgramBase.s_iAppID
            get;

            set;
        }

        /// <summary>
        /// Имя приложения без расширения
        /// </summary>
        public static string AppName
        {
            get
            {
                string appName = string.Empty;
                string [] args = System.Environment.GetCommandLineArgs ();
                int posAppName = -1
                    , posDelim = -1;

                posAppName = args [0].LastIndexOf ('\\') + 1;

                //Отсечь параметры (после пробела)
                posDelim = args [0].IndexOf (' ', posAppName);
                if (!(posDelim < 0))
                    appName = args [0].Substring (posAppName, posDelim - posAppName - 1);
                else
                    appName = args [0].Substring (posAppName);
                //Отсечь расширение
                posDelim = appName.IndexOf ('.');
                if (!(posDelim < 0))
                    appName = appName.Substring (0, posDelim);
                else
                    ;

                return appName;
            }
        }

        public int UserId
        {// HUsers.Id
            get;

            set;
        }

        /// <summary>
        /// Изменить режим работы объекта
        /// </summary>
        /// <param name="mode">Значение нового режима работы</param>
        public static void ReLogg (LOG_MODE mode)
        {
            if (!(s_mode == mode)) {
                m_this = null;
                s_mode = mode;
            } else
                ;
        }

        /// <summary>
        /// Возвратить объект для размещения сообщений в журнале
        /// </summary>
        /// <returns>Объект для выполнения журналирования</returns>
        public static Logging Logg ()
        {
            string pathToFile = string.Empty;

            if (m_this == null) {
                switch (s_mode) {
                    case LOG_MODE.FILE_EXE:
                        //m_this = new Logging(System.Environment.CurrentDirectory + @"\" + AppName + "_" + Environment.MachineName + "_log.txt", false, null, null);
                        pathToFile = System.Environment.CurrentDirectory + @"\logs";
                        try {
                            if (Directory.Exists (pathToFile) == false)
                                Directory.CreateDirectory (pathToFile);
                            else
                                ;
                        } catch { }
                        break;
                    case LOG_MODE.FILE_DESKTOP:
                        pathToFile = System.Environment.GetFolderPath (Environment.SpecialFolder.Desktop);
                        break;
                    case LOG_MODE.FILE_NETDEV:
                        pathToFile = @"\\ne1150\D$\My Project's\Work's\C.Net\Temp";
                        break;
                    case LOG_MODE.FILE_LOCALDEV:
                        pathToFile = @"D:\My Project's\Work's\C.Net\Temp";
                        break;
                    case LOG_MODE.DB:
                    case LOG_MODE.UNKNOWN:
                    default:
                        m_this = new Logging ();
                        break;
                }

                if (string.IsNullOrEmpty (pathToFile) == false)
                    m_this = new Logging (string.Format (@"{0}\{1}_{2}_{3}.{4}", pathToFile, AppName, Environment.MachineName, "log", "txt"));
                else
                    ;
            } else
                ;

            return m_this;
        }

        /// <summary>
        /// Запустить поток обработки событий по приему сообщений для дальнейщего их размещения в журнале
        /// </summary>
        protected virtual void start ()
        {
            m_arEvtThread = new ManualResetEvent [] { new ManualResetEvent (false), new ManualResetEvent (false) };

            if (s_mode == LOG_MODE.DB) {
                m_evtIsDbConnect = new ManualResetEvent (false);
                m_timerDbConnect =
                    new System.Threading.Timer (timerDbConnect, null, 0, 6666)
                    //new System.Windows.Forms.Timer ()
                    ;
                //m_timerConnSett.Tick += new EventHandler(TimerConnSett_Tick);
                //m_timerConnSett.Start ();
                //m_timerConnSett.Interval = MAX_WATING;
            } else
                ;

            m_objQueueMessage = new object ();

            m_threadPost = new Thread (new ParameterizedThreadStart (threadPost));
            m_threadPost.Name = @"Логгирование приложения..." + AppName;
            m_threadPost.IsBackground = true;
            m_threadPost.Start ();

            for (int i = (int)INDEX_MESSAGE.A_001; i < (int)INDEX_MESSAGE.COUNT_INDEX_MESSAGE; i++)
                UnLink ((INDEX_MESSAGE)i);
            // обновление происходит одновременно с 'UnLink'
            //UpdateMarkDebugLog ();
        }

        /// <summary>
        /// Разместить в журнале событие "Запуск" приложения
        /// </summary>
        /// <param name="message">Содержание сообщения "Запуск" приложения</param>
        public void PostStop (string message)
        {
            post (ID_MESSAGE.STOP, message, true, true, true);
        }

        /// <summary>
        /// Остановить поток приема событий для размещения их в журнале
        /// </summary>
        public void Stop ()
        {
            m_arEvtThread [(int)INDEX_SEMATHREAD.STOP].Set ();

            if ((!(m_threadPost == null)) && (m_threadPost.IsAlive == true)) {
                if ((m_threadPost.Join (Constants.MAX_WATING) == false))
                    m_threadPost.Abort ();
                else
                    ;
            } else
                ;

            if ((s_mode == LOG_MODE.DB) && (!(m_timerDbConnect == null))) {
                m_timerDbConnect.Change (System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
                //m_timerConnSett.Stop ();
                m_timerDbConnect.Dispose ();
                m_timerDbConnect = null;

                DbWriter.Disconnect ();
                m_evtIsDbConnect.Reset ();
            } else
                if (((s_mode == LOG_MODE.FILE_EXE) || (s_mode == LOG_MODE.FILE_DESKTOP) || (s_mode == LOG_MODE.FILE_LOCALDEV) || (s_mode == LOG_MODE.FILE_NETDEV))
                    || (s_mode == LOG_MODE.UNKNOWN)) {
                if ((!(m_sw == null)) && (!(m_sw.BaseStream == null)) && (m_sw.BaseStream.CanWrite == true)) {
                    m_sw.Flush ();
                    m_sw.Close ();
                } else
                    ;
            } else
                ;

            //if (! (m_arEvtThread == null)) {
            //    for (int i = 0; i < (int)(INDEX_SEMATHREAD.STOP + 1); i ++)
            //        if (!(m_arEvtThread[i] == null))
            //        {
            //            m_arEvtThread[i].Close();
            //            m_arEvtThread[i] = null;
            //        } else
            //            ;

            //    m_arEvtThread = null;
            //} else
            //    ;

            m_threadPost = null;
        }

        private void threadPost (object par)
        {
            int err = -1
                , indx_queue = -1
                , cnt_running = -1;
            string toPost = string.Empty;
            INDEX_SEMATHREAD indx_semathread = INDEX_SEMATHREAD.STOP;

            if (s_mode == LOG_MODE.DB)
                m_evtIsDbConnect.WaitOne ();
            else
                ;

            while (true) {
                indx_semathread = (INDEX_SEMATHREAD)WaitHandle.WaitAny (m_arEvtThread);

                //Отправление сообщений...
                err = -1;
                toPost = string.Empty;

                if (m_listQueueMessage.Count > 0) {
                    switch (s_mode) {
                        case LOG_MODE.DB:
                            if (m_evtIsDbConnect.WaitOne (0, true) == true) {
                                lock (m_objQueueMessage) {
                                    ////Отладка!!!
                                    //Console.WriteLine(@"Логгирование: сообщений на вХоде=" + m_listQueueMessage.Count);

                                    indx_queue =
                                    cnt_running =
                                        0;
                                    while ((indx_queue < m_listQueueMessage.Count) && (cnt_running < MAX_COUNT_MESSAGE_ONETIME)) {
                                        if (m_listQueueMessage [indx_queue].m_state == STATE_MESSAGE.QUEUE) {
                                            toPost += getInsertQuery (m_listQueueMessage [indx_queue]) + @";";
                                            m_listQueueMessage [indx_queue].m_state = STATE_MESSAGE.RUNNING;

                                            cnt_running++;
                                        } else
                                            ;

                                        indx_queue++;
                                    }
                                }

                                DbWriter.Exec (toPost, out err);

                                if (!(err == 0)) { //Ошибка при записи сообщений...
                                    retryQueueMessage ();

                                    DbWriter.Disconnect ();
                                    m_evtIsDbConnect.Reset ();
                                } else { //Успех при записи сообщений...
                                    clearQueueMessage ();
                                }

                                ////Отладка!!!
                                //lock (m_objQueueMessage)
                                //{
                                //    Console.WriteLine(@"Логгирование: сообщений на вЫходе=" + m_listQueueMessage.Count);
                                //    foreach (MESSAGE msg in m_listQueueMessage)
                                //        Console.WriteLine(@"Тип сообщения=" + msg.m_state.ToString ());
                                //}
                            } else
                                ; //Нет соединения с БД
                            break;
                        case LOG_MODE.FILE_EXE:
                        case LOG_MODE.FILE_DESKTOP:
                        case LOG_MODE.FILE_LOCALDEV:
                        case LOG_MODE.FILE_NETDEV:
                            bool locking = false;
                            if ((!(m_listQueueMessage == null)) && (m_listQueueMessage.Count > 0)) {
                                lock (m_objQueueMessage) {
                                    indx_queue = 0;
                                    while (indx_queue < m_listQueueMessage.Count) {
                                        if (m_listQueueMessage [indx_queue].m_bSeparator == true)
                                            toPost += MessageSeparator + Environment.NewLine;
                                        else
                                            ;

                                        if (m_listQueueMessage [indx_queue].m_bDatetimeStamp == true) {
                                            toPost += m_listQueueMessage [indx_queue].m_strDatetimeReg + Environment.NewLine;
                                            toPost += DatetimeStampSeparator + Environment.NewLine;
                                        } else
                                            ;

                                        toPost += m_listQueueMessage [indx_queue].m_text + Environment.NewLine;

                                        if (m_listQueueMessage.Count == 1)
                                            locking = m_listQueueMessage [indx_queue].m_bLockFile;
                                        else
                                            ;

                                        if (locking == false)
                                            if ((DateTime.Now - logRotateCheckLast).TotalSeconds > logRotateChekMaxSeconds)
                                                //Принудить к проверке размера файла
                                                locking = true;
                                            else
                                                ;
                                        else
                                            ;

                                        m_listQueueMessage [indx_queue].m_state = STATE_MESSAGE.RUNNING;

                                        indx_queue++;
                                    }
                                }

                                if (locking == true) {
                                    LogLock ();
                                    LogCheckRotate ();
                                } else
                                    ;

                                if (File.Exists (m_fileName) == true) {
                                    try {
                                        if ((m_sw == null) || (m_fi == null)) {
                                            //Вариант №1
                                            //FileInfo f = new FileInfo(m_fileName);
                                            //FileStream fs = f.Open(FileMode.Append, FileAccess.Write, FileShare.Write);
                                            //m_sw = new LogStreamWriter(fs, Encoding.GetEncoding("windows-1251"));
                                            //Вариант №2                        
                                            m_sw = new LogStreamWriter (m_fileName, true, Encoding.GetEncoding ("windows-1251"));

                                            m_fi = new FileInfo (m_fileName);
                                        } else
                                            ;

                                        m_sw.Write (toPost);
                                        m_sw.Flush ();
                                    } catch (Exception e) {
                                        retryQueueMessage ();

                                        /*m_sw.Close ();*/
                                        m_sw = null;
                                        m_fi = null;
                                    }
                                } else
                                    retryQueueMessage ();

                                //if (externalLog == true)
                                //{
                                //    if (timeStamp == true)
                                //        delegateUpdateLogText(DateTime.Now.ToString() + ": " + message + Environment.NewLine);
                                //    else
                                //        delegateUpdateLogText(message + Environment.NewLine);
                                //}
                                //else
                                //    ;

                                clearQueueMessage ();

                                if (locking == true)
                                    LogUnlock ();
                                else
                                    ;
                            } else {
                            }
                            break;
                        case LOG_MODE.UNKNOWN:
                        default:
                            break;
                    }
                } else
                    ;

                if (indx_semathread == INDEX_SEMATHREAD.STOP)
                    break;
                else {
                    m_arEvtThread [(int)INDEX_SEMATHREAD.MSG].Reset ();
                }
            }
        }

        private void retryQueueMessage ()
        {
            lock (m_objQueueMessage) {
                //Постановка ПОВТОРно сообщений в очередь
                foreach (MESSAGE msg in m_listQueueMessage)
                    if (msg.m_state == STATE_MESSAGE.RUNNING)
                        msg.m_state = STATE_MESSAGE.QUEUE;
                    else
                        ;
            }
        }

        private void clearQueueMessage ()
        {
            lock (m_objQueueMessage) {
                //Найти обработанные сообщения
                List<int> listIndxMsgRunning = new List<int> ();
                foreach (MESSAGE msg in m_listQueueMessage)
                    if (msg.m_state == STATE_MESSAGE.RUNNING)
                        listIndxMsgRunning.Add (m_listQueueMessage.IndexOf (msg));
                    else
                        ;

                //Сортировать список индексов в ОБРАТном порядке
                // для удаления сообщений из списка по ИНДЕКСу
                listIndxMsgRunning.Sort (delegate (int i1, int i2) {
                    return i1 > i2 ? -1 : 1;
                });

                //Удалить обработанные сообщения
                foreach (int indx in listIndxMsgRunning)
                    m_listQueueMessage.RemoveAt (indx);
            }
        }

        /// <summary>
        /// Метод обратного вызова таймера проверки наличия соединения с БД
        /// </summary>
        /// <param name="par">Аргумент для события</param>
        private void timerDbConnect (object par)
        //private void TimerConnSett_Tick(object par, EventArgs ev)
        {
            if (m_evtIsDbConnect.WaitOne (0, true) == false) {
                if (DbWriter.Connect () == 0) {
                    m_evtIsDbConnect.Set ();
                    m_arEvtThread [(int)INDEX_SEMATHREAD.MSG].Set ();
                } else {
                    DbWriter.Disconnect ();
                    m_evtIsDbConnect.Reset ();
                }
            } else
                ;
        }

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="name">имя лог-файла</param>
        ///// <param name="extLog">признак - внешнее логгирование</param>
        ///// <param name="updateLogText">функция записи во внешний лог-файл</param>
        ///// <param name="clearLogText">функция очистки внешнего лог-файла</param>
        //private Logging(string name, bool extLog, DelegateStringFunc updateLogText, DelegateFunc clearLogText)
        private Logging (string name)
        {
            //externalLog = extLog;
            logRotateSize = logRotateSizeDefault;
            m_fileNameStart = m_fileName = name;
            sema = new Semaphore (1, 1);

            try {
                m_sw = new LogStreamWriter (m_fileName, true, Encoding.GetEncoding ("windows-1251"));
                m_fi = new FileInfo (m_fileName);
            } catch (Exception e) {
                //Нельзя сообщить программе...
                //throw new Exception(@"private Logging::Logging () - ...", e);
                DelegateProgramAbort ();
            }

            logRotateCheckLast = DateTime.Now;
            logRotateChekMaxSeconds = 60;

            //logIndex = 0;
            //delegateUpdateLogText = updateLogText;
            //delegateClearLogText = clearLogText;

            start ();
        }

        private Logging ()
        {
            start ();
        }

        /// <summary>
        /// Обновить признак необходимости размещения в журнале сообщения подтипа, указанного в аргументе
        /// </summary>
        /// <param name="indxDebugLogMessage">Индекс/идентификатор подтипа сообщения</param>
        private static void updateMarkDebugLog (INDEX_MESSAGE indxDebugLogMessage)
        {
            bool bMarked = false;

            if (!(s_arDebugLogMessageIds [(int)indxDebugLogMessage] == (int)INDEX_MESSAGE.NOT_SET)) {
                bMarked = false;

                if (!(DelegateGetINIParametersOfKEY == null))
                    ; //bool.TryParse(FormMainBase.DelegateGetINIParametersOfKey(...). out bMarked);
                else
                    if (!(DelegateGetINIParametersOfID == null))
                    bool.TryParse (DelegateGetINIParametersOfID (s_arDebugLogMessageIds [(int)indxDebugLogMessage]), out bMarked);
                else
                    ;

                s_markDebugLog.Set ((int)indxDebugLogMessage, bMarked);
            } else
                ;
        }

        /// <summary>
        /// Обновляет параметры журналирования "присоединенных" на текущий момент подтипов сообщений
        /// , присоединение/отсоединение ('Link'/'UnLink')
        /// </summary>
        public static void UpdateMarkDebugLog ()
        {
            Enum.GetValues (typeof (INDEX_MESSAGE)).OfType<INDEX_MESSAGE> ().ToList ().ForEach (indx => {
                if ((indx > INDEX_MESSAGE.NOT_SET)
                    && (indx < INDEX_MESSAGE.COUNT_INDEX_MESSAGE))
                    updateMarkDebugLog (indx);
                else
                    ;
            });
        }

        /// <summary>
        /// Приостановка логгирования
        /// </summary>
        /// <returns>строка с именем лог-файла</returns>
        public string Suspend ()
        {
            switch (s_mode) {
                case LOG_MODE.FILE_EXE:
                case LOG_MODE.FILE_DESKTOP:
                case LOG_MODE.FILE_LOCALDEV:
                case LOG_MODE.FILE_NETDEV:
                    LogLock ();

                    Debug ("Пауза ведения журнала...", Logging.INDEX_MESSAGE.NOT_SET, false);

                    m_sw.Close ();
                    break;
                case LOG_MODE.DB:
                    Debug ("Пауза ведения журнала...", Logging.INDEX_MESSAGE.NOT_SET, false);
                    break;
                case LOG_MODE.UNKNOWN:
                default:
                    break;
            }

            return m_fi.FullName;
        }

        /// <summary>
        /// Восстановление логгирования
        /// </summary>
        public void Resume ()
        {
            switch (s_mode) {
                case LOG_MODE.FILE_EXE:
                case LOG_MODE.FILE_DESKTOP:
                case LOG_MODE.FILE_LOCALDEV:
                case LOG_MODE.FILE_NETDEV:
                    m_sw = new LogStreamWriter (m_fi.FullName, true, Encoding.GetEncoding ("windows-1251"));

                    Debug ("Возобновление ведения журнала...", Logging.INDEX_MESSAGE.NOT_SET, false);

                    LogUnlock ();
                    break;
                case LOG_MODE.DB:
                    Debug ("Возобновление ведения журнала...", Logging.INDEX_MESSAGE.NOT_SET, false);
                    break;
                case LOG_MODE.UNKNOWN:
                default:
                    break;
            }
        }

        /// <summary>
        /// Блокирование лог-файла для изменения содержания
        /// </summary>
        public void LogLock ()
        {
            sema.WaitOne ();
        }

        /// <summary>
        /// Разблокирование лог-файла после изменения содержания
        /// </summary>
        public void LogUnlock ()
        {
            sema.Release ();
        }

        /// <summary>
        /// Перечисление - возможные состояние для сообщения
        /// </summary>
        private enum STATE_MESSAGE {
            UNKNOWN,
            /// <summary>
            /// Поставлен в очередь
            /// </summary>
            QUEUE,
            /// <summary>
            /// В процессе сохранения
            /// </summary>
            RUNNING
        };
        /// <summary>
        /// Перечисление - индексы для объектов синхронизации, определяющих выполнение журналирование или его прерывание
        /// </summary>
        private enum INDEX_SEMATHREAD { MSG, STOP };

        private class MESSAGE {
            public int m_id;
            public STATE_MESSAGE m_state;
            public string m_strDatetimeReg;
            public string m_text;

            public bool m_bSeparator
                , m_bDatetimeStamp
                , m_bLockFile;

            public MESSAGE (int id, DateTime dtReg, string text, bool bSep, bool bDatetime, bool bLock)
            {
                m_id = id;
                m_state = STATE_MESSAGE.QUEUE;
                m_strDatetimeReg = dtReg.ToString (s_strDatetimeFrmt);
                m_text = text;

                m_bSeparator = bSep;
                m_bDatetimeStamp = bDatetime;
                m_bLockFile = bLock;
            }
        }

        private void addMessage (int id_msg, string msg, bool bSep, bool bDatetime, bool bLock)
        {
            if (m_listQueueMessage == null)
                m_listQueueMessage = new List<MESSAGE> ();
            else
                ;
            lock (m_objQueueMessage) {
                if (m_listQueueMessage.Count > MAXCOUNT_LISTQUEUEMESSAGE)
                    m_listQueueMessage.RemoveAt (0);
                else
                    ;

                m_listQueueMessage.Add (new MESSAGE ((int)id_msg, HDateTime.ToMoscowTimeZone (DateTime.Now), msg, bSep, bDatetime, bLock));
            }
        }

        private string getInsertQuery (MESSAGE msg)
        {
            return $@"INSERT INTO [dbo].[logging]([ID_LOGMSG],[ID_APP],[ID_USER],[DATETIME_WR],[MESSAGE], [INSERT_DATETIME])VALUES" +
                $"({msg.m_id}, {AppId}, {UserId}, '{msg.m_strDatetimeReg}', '{msg.m_text.Replace ('\'', '`')}', GETDATE())";
        }

        private string getInsertQuery (int id, string text)
        {
            return $"INSERT INTO [dbo].[logging]([ID_LOGMSG],[ID_APP],[ID_USER],[DATETIME_WR],[MESSAGE], [INSERT_DATETIME])VALUES" +
                $"({id}, {AppId}, {UserId}, GETDATE (), '{text.Replace ('\'', '`')}', GETDATE())";
        }

        /// <summary>
        /// Запись сообщения в лог-файл
        /// </summary>
        /// <param name="message">сообщение</param>
        /// <param name="separator">признак наличия разделителя</param>
        /// <param name="timeStamp">признак наличия метки времени</param>
        /// <param name="locking">признак блокирования при записи сообщения</param>
        private void post (ID_MESSAGE id, string message, bool separator, bool timeStamp, bool locking/* = false*/)
        {
            if (s_mode > LOG_MODE.UNKNOWN) {
                bool bAddMessage = false;

                switch (s_mode) {
                    case LOG_MODE.DB:
                        //...запомнить очередное сообщение...
                        addMessage ((int)id, message, true, true, true); //3 крайних параметра для БД ничего не значат...

                        if (m_evtIsDbConnect.WaitOne (0, true) == true) {
                            //Установить признак возможности для отправки
                            bAddMessage = true;
                        } else {
                        }
                        break;
                    case LOG_MODE.FILE_EXE:
                    case LOG_MODE.FILE_DESKTOP:
                    case LOG_MODE.FILE_LOCALDEV:
                    case LOG_MODE.FILE_NETDEV:
                        addMessage ((int)id, message, separator, timeStamp, locking); //3 крайних параметра для БД ничего не значат...
                        //Установить признак возможности для отправки
                        bAddMessage = true;
                        break;
                    default:
                        break;
                }

                if (bAddMessage == true)
                    if (m_arEvtThread [(int)INDEX_SEMATHREAD.MSG].WaitOne (0, true) == false)
                        //Не установлен - отправить сообщения...
                        m_arEvtThread [(int)INDEX_SEMATHREAD.MSG].Set ();
                    else
                        ; //Установлен - ...
                else
                    ;
            } else
                ;
        }

        /*
        public bool Log
        {
            get { return logging; }
            set { logging = value; }
        }
        */

        /// <summary>
        /// Наименование лог-файла
        /// </summary>
        /// <returns>строка с наименованием лог-файла</returns>
        private string LogFileName (int indxArchive)
        {
            string strRes = string.Empty;
            if (indxArchive == 0)
                strRes = Path.GetDirectoryName (m_fileName) + "\\" + Path.GetFileNameWithoutExtension (m_fileName) + Path.GetExtension (m_fileName);
            else
                strRes = Path.GetDirectoryName (m_fileName) + "\\" + Path.GetFileNameWithoutExtension (m_fileName) + indxArchive.ToString () + Path.GetExtension (m_fileName);

            return strRes;
        }

        private void LogRotateNowLocked ()
        {
            //if (externalLog == true)
            //    delegateClearLogText();
            //else
            //    ;

            try {
                m_sw.Close ();

                //logIndex = (logIndex + 1) % logRotateFiles;
                //m_fileName = LogFileName();

                LogToArchive ();

                m_sw = new LogStreamWriter (m_fileName, false, Encoding.GetEncoding ("windows-1251"));
                m_fi = new FileInfo (m_fileName);
            } catch (Exception e) {
                /*m_sw.Close ();*/
                m_sw = null;
                m_fi = null;
            }
        }

        private void LogToArchive (int indxArchive = 0)
        {
            string logFileName = LogFileName (indxArchive),
                logToFileName = LogFileName (++indxArchive);

            if (File.Exists (logToFileName) == true) {
                if (!(indxArchive > (MAX_ROTATE_COUNT - 1)))
                    LogToArchive (indxArchive);
                else
                    File.Delete (logToFileName);
            } else {
            }

            if (File.Exists (logToFileName) == false)
                File.Create (logToFileName).Close ();
            else
                ;

            File.Copy (logFileName, logToFileName, true);
        }

        private void LogRotateNow ()
        {
            LogLock ();
            LogRotateNowLocked ();
            LogUnlock ();
        }

        private void LogCheckRotate ()
        {
            logRotateCheckLast = DateTime.Now;

            if (!(m_fi == null)) {
                if (File.Exists (m_fileName) == true)
                    try {
                        m_fi.Refresh ();

                        if (m_fi.Length > logRotateSize)
                            LogRotateNowLocked ();
                        else
                            ;
                    } catch (Exception e) {
                        //m_fi = null;
                    } else
                    ;
            } else
                ;
        }

        public bool LogRotate
        {
            get
            {
                return logRotate;
            }
            set
            {
                logRotate = value;
            }
        }

        public int LogRotateMaxSize
        {
            get
            {
                return logRotateSize;
            }
            set
            {
                if (value <= 0 || value > logRotateSizeMax)
                    logRotateSize = logRotateSizeMax;
                else
                    logRotateSize = value;
            }
        }

        private bool post (INDEX_MESSAGE indx)
        {
            bool bRes = false;
            // если подтип не указан ('NOT_SET'), то сообщение размещается в журнале
            bRes = indx == INDEX_MESSAGE.NOT_SET;

            if (bRes == false)
                // подтип сообщения указан, требуется проверка пользовательской конфигурации
                bRes = s_markDebugLog.IsMarked ((int)indx);
            else
                ;

            return bRes;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public void PostStart (string message)
        {
            if (s_mode == Logging.LOG_MODE.UNKNOWN)
                s_mode = Logging.LOG_MODE.FILE_EXE;
            else
                ;

            post (ID_MESSAGE.START, message, true, true, true);
        }

        public void Action (string message, INDEX_MESSAGE indx, bool bLock = true)
        {
            if (post (indx) == true)
                post (ID_MESSAGE.ACTION, "!Действие!: " + message, true, true, bLock);
            else
                ;
        }

        public void Error (string message, INDEX_MESSAGE indx, bool bLock = true)
        {
            if (post (indx) == true)
                post (ID_MESSAGE.ERROR, "!Ошибка!: " + message, true, true, bLock);
            else
                ;
        }

        public void Error (MethodBase methodBase, string message, INDEX_MESSAGE indx, bool bLock = true)
        {
            if (post (indx) == true)
                post (ID_MESSAGE.ERROR, "!Ошибка!: "
                    + GetMethodInfo (methodBase)
                    + message
                    , true, true, bLock);
            else
                ;
        }

        public void Warning (string message, INDEX_MESSAGE indx, bool bLock = true)
        {
            if (post (indx) == true)
                post (ID_MESSAGE.WARNING, "!Предупреждение!: " + message, true, true, bLock);
            else
                ;
        }

        public void Warning (MethodBase methodBase, string message, INDEX_MESSAGE indx, bool bLock = true)
        {
            if (post (indx) == true)
                post (ID_MESSAGE.WARNING, "!Предупреждение!: "
                    + GetMethodInfo (methodBase)
                    + message
                    , true, true, bLock);
            else
                ;
        }

        public void Debug (string message, INDEX_MESSAGE indx, bool bLock = true)
        {
            if (post (indx) == true)
                post (ID_MESSAGE.DEBUG, "!Отладка!: " + message, true, true, bLock);
            else
                ;
        }

        public static string GetMethodInfo (MethodBase methodBase)
        {
            return string.Format (@"{0}.{1}::{2} () - ", methodBase.Module, methodBase.DeclaringType, methodBase.Name);
        }

        public void Debug (MethodBase methodBase, string message, INDEX_MESSAGE indx, bool bLock = true)
        {
            if (post (indx) == true)
                post (ID_MESSAGE.DEBUG, "!Отладка!: "
                    + GetMethodInfo (methodBase)
                    + message
                    , true, true, bLock);
            else
                ;
        }

        public void Exception (Exception e, string message, INDEX_MESSAGE indx, bool bLock = true)
        {
            if (post (indx) == true) {
                string msg = string.Empty;
                msg += "!Исключение! обработка: " + message + Environment.NewLine;
                msg += "Исключение: " + e.Message + Environment.NewLine;
                msg += e.ToString ();

                post (ID_MESSAGE.EXCEPTION, msg, true, true, bLock);
            } else
                ;
        }

        public void ExceptionDB (string message)
        {
            post (ID_MESSAGE.EXCEPTION_DB, message, true, true, true);
        }

        internal class LogStreamWriter : StreamWriter {
            /*
            public LogStreamWriter(FileStream fs, System.Text.Encoding e)
                : base(fs, e)
            {
            }
            */

            public LogStreamWriter (string path, bool append, System.Text.Encoding e)
                : base (path, append, e)
            {
            }

            ~LogStreamWriter ()
            {
                this.Dispose ();
            }

            protected override void Dispose (bool disposing)
            {
                try {
                    base.Dispose (disposing);
                } catch (Exception e) { }
            }
        }
    }
}
