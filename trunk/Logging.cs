using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
//using System.Windows.Forms;
using System.Threading;
using System.Data.Common;

namespace HClassLibrary
{
    //public class Logging
    //{

    //    //private Logging () {
    //    //}
    //}

    public class Logging //LoggingFS //: Logging
    {
        public enum LOG_MODE { ERROR = -1, UNKNOWN, FILE_EXE, FILE_DESKTOP, FILE_LOCALDEV, FILE_NETDEV, DB };
        private enum ID_MESSAGE { START = 1, STOP, ACTION, DEBUG, EXCEPTION, EXCEPTION_DB, ERROR, WARNING };
        public enum INDEX_MESSAGE { NOT_SET = -1
                                    , A_001, A_002, A_003
                                    , D_001, D_002, D_003, D_004, D_005, D_006
                                    , EXCPT_001, EXCPT_002, EXCPT_003, EXCPT_004, EXCPT_005, EXCPT_006
                                    , EXCPT_DB_001, EXCPT_DB_002, EXCPT_DB_003, EXCPT_DB_004
                                    , ERR_001, ERR_002, ERR_003, ERR_004, ERR_005, ERR_006
                                    , W_001, W_002, W_003
                    , COUNT_INDEX_MESSAGE
        };

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
        public static LOG_MODE s_mode
        {
            get { return _mode; }

            set {
                _mode = value;

                switch (_mode)
                {
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
        private const int MAX_ARCHIVE = 6;
        private static int logRotateSizeDefault = (int) Math.Floor((double)(1024 * 1024 * 5)); //1024 * 1024 * 5;
        private static int logRotateSizeMax = (int) Math.Floor((double)(1024 * 1024 * 10)); //1024 * 1024 * 1024;
        private int logRotateSize;
        //private int logIndex;
        private const int logRotateFilesDefault = 1;
        private const int logRotateFilesMax = 100;
        private int logRotateFiles;
        private DateTime logRotateCheckLast; //Дата/время крайней проверки размера файла (для окончания записи)
        private int logRotateChekMaxSeconds; //Макс. кол-во сек. между проверки размера файла (для окончания записи)

        public static string DatetimeStampSeparator = new string ('-', 49); //"------------------------------------------------";
        public static string MessageSeparator = new string('=', 49); //"================================================";

        protected static Logging m_this = null;

        private object m_objQueueMessage;

        private static ConnectionSettings s_connSett = null;
        private static int s_iIdListener = -1;
        private static DbConnection s_dbConn = null;
        
        public static ConnectionSettings ConnSett {
            //get { return s_connSett; }
            set {
                s_connSett = new ConnectionSettings ();

                s_connSett.id = value.id;
                s_connSett.name = value.name;
                s_connSett.server = value.server;
                s_connSett.port = value.port;
                s_connSett.dbName = value.dbName;
                s_connSett.userName = value.userName;
                s_connSett.password = value.password;

                s_connSett.ignore = value.ignore;
            }
        }
        private static List<MESSAGE> m_listQueueMessage;

        private Thread m_threadPost;
        private
            System.Threading.Timer
            //System.Windows.Forms.Timer
                m_timerConnSett
                ;
        private ManualResetEvent [] m_arEvtThread;
        private ManualResetEvent m_evtConnSett;

        private static int[] s_arDebugLogMessageIds = new int [(int)INDEX_MESSAGE.COUNT_INDEX_MESSAGE];
        private static HMark s_markDebugLog = new HMark(0);
        public static StringDelegateIntFunc DelegateGetINIParametersOfID;
        public static StringDelegateStringFunc DelegateGetINIParametersOfKEY;

        public static void LinkId (INDEX_MESSAGE indx, int id) {
            s_arDebugLogMessageIds [(int)indx] = id;
        }

        public static void UnLink(INDEX_MESSAGE indx)
        {
            s_arDebugLogMessageIds[(int)indx] = (int)INDEX_MESSAGE.NOT_SET;
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

                posAppName = args[0].LastIndexOf('\\') + 1;

                //Отсечь параметры (после пробела)
                posDelim = args[0].IndexOf(' ', posAppName);
                if (!(posDelim < 0))
                    appName = args[0].Substring(posAppName, posDelim - posAppName - 1);
                else
                    appName = args[0].Substring(posAppName);
                //Отсечь расширение
                posDelim = appName.IndexOf('.');
                if (!(posDelim < 0))
                    appName = appName.Substring(0, posDelim);
                else
                    ;

                return appName;
            }
        }

        private static int connect () {
            int err = -1;

            if (! (s_connSett == null)) {
                s_iIdListener = DbSources.Sources().Register(s_connSett, false, @"LOGGING_DB");
                //Console.WriteLine(@"Logging::connect (active=false) - s_iIdListener=" + s_iIdListener);
                if (!(s_iIdListener < 0))
                    s_dbConn = DbSources.Sources().GetConnection(s_iIdListener, out err);
                else
                    ;
            }
            else
                ;

            return err;
        }

        private static void disconnect()
        {
            if (!(s_iIdListener < 0)) DbSources.Sources().UnRegister(s_iIdListener); else ;
            s_iIdListener = -1;
            s_dbConn = null;
        }

        public static void ReLogg(LOG_MODE mode)
        {
            if (! (s_mode == mode))
            {
                m_this = null;
                s_mode = mode;
            }
            else
                ;
        }

        public static Logging Logg()
        {
            if (m_this == null)
            {
                switch (s_mode) {
                    case LOG_MODE.FILE_EXE:
                        //m_this = new Logging(System.Environment.CurrentDirectory + @"\" + AppName + "_" + Environment.MachineName + "_log.txt", false, null, null);
                        m_this = new Logging(System.Environment.CurrentDirectory + @"\logs\" + AppName + "_" + Environment.MachineName + "_log.txt");
                        break;
                    case LOG_MODE.FILE_DESKTOP:
                        m_this = new Logging(System.Environment.GetFolderPath (Environment.SpecialFolder.Desktop) + @"\" + AppName + "_" + Environment.MachineName + "_log.txt");
                        break;
                    case LOG_MODE.FILE_NETDEV:
                        m_this = new Logging(@"\\ne1150\D$\My Project's\Work's\C.Net\Temp" + @"\" + AppName + "_" + Environment.MachineName + "_log.txt");
                        break;
                    case LOG_MODE.FILE_LOCALDEV:
                        m_this = new Logging(@"D:\My Project's\Work's\C.Net\Temp" + @"\" + AppName + "_" + Environment.MachineName + "_log.txt");
                        break;
                    case LOG_MODE.DB:
                    case LOG_MODE.UNKNOWN:
                    default:
                        m_this = new Logging ();
                        break;
                }
            }
            else
                ;

            return m_this;
        }

        protected virtual void start () {
            m_arEvtThread = new ManualResetEvent[] { new ManualResetEvent(false), new ManualResetEvent(false) };            

            if (s_mode == LOG_MODE.DB) {
                m_evtConnSett = new ManualResetEvent(false);
                m_timerConnSett =
                    new System.Threading.Timer (TimerConnSett_Tick, null, 0, 6666)
                    //new System.Windows.Forms.Timer ()
                    ;                
                //m_timerConnSett.Tick += new EventHandler(TimerConnSett_Tick);
                //m_timerConnSett.Start ();
                //m_timerConnSett.Interval = 6666;
            }
            else
                ;

            m_objQueueMessage = new object ();

            m_threadPost = new Thread (new ParameterizedThreadStart (threadPost));
            m_threadPost.Name = @"Логгирование приложения..." + AppName;
            m_threadPost.IsBackground = true;
            m_threadPost.Start();

            for (int i = (int)INDEX_MESSAGE.A_001; i < (int)INDEX_MESSAGE.COUNT_INDEX_MESSAGE; i ++)
                UnLink((INDEX_MESSAGE)i);
            UpdateMarkDebugLog ();
        }

        public void PostStop (string message)
        {
            post(ID_MESSAGE.STOP, message, true, true, true);
        }

        public void Stop () {
            m_arEvtThread[(int)INDEX_SEMATHREAD.STOP].Set ();

            if ((!(m_threadPost == null)) && (m_threadPost.IsAlive == true)) {
                if ((m_threadPost.Join(6666) == false))
                    m_threadPost.Abort ();
                else
                    ;
            }
            else
                ;

            if ((s_mode == LOG_MODE.DB) && (! (m_timerConnSett == null))) {
                m_timerConnSett.Change (System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
                //m_timerConnSett.Stop ();
                m_timerConnSett.Dispose ();
                m_timerConnSett = null;

                disconnect();
                m_evtConnSett.Reset();
            }
            else
                if (((s_mode == LOG_MODE.FILE_EXE) || (s_mode == LOG_MODE.FILE_DESKTOP) || (s_mode == LOG_MODE.FILE_LOCALDEV) || (s_mode == LOG_MODE.FILE_NETDEV))
                    || (s_mode == LOG_MODE.UNKNOWN)) {
                    if ((!(m_sw == null)) && (! (m_sw.BaseStream == null)) && (m_sw.BaseStream.CanWrite == true))
                    {
                        m_sw.Flush();
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

        private void threadPost (object par) {
            if (s_mode == LOG_MODE.DB)
                m_evtConnSett.WaitOne ();
            else
                ;

            while (true) {
                INDEX_SEMATHREAD indx_semathread = (INDEX_SEMATHREAD)WaitHandle.WaitAny(m_arEvtThread);

                //Отправление сообщений...
                int err = -1;
                string toPost = string.Empty;

                if (m_listQueueMessage.Count > 0)
                {
                    switch (s_mode) {
                        case LOG_MODE.DB:
                            if (m_evtConnSett.WaitOne (0, true) == true)
                            {                                
                                lock (m_objQueueMessage)
                                {
                                    ////Отладка!!!
                                    //Console.WriteLine(@"Логгирование: сообщений на вХоде=" + m_listQueueMessage.Count);
                                    
                                    int indx_queue = 0
                                        , cnt_running = 0;
                                    while ((indx_queue < m_listQueueMessage.Count) && (cnt_running < MAX_COUNT_MESSAGE_ONETIME))
                                    {
                                        if (m_listQueueMessage[indx_queue].m_state == STATE_MESSAGE.QUEUE)
                                        {
                                            toPost += getInsertQuery(m_listQueueMessage[indx_queue]) + @";";
                                            m_listQueueMessage[indx_queue].m_state = STATE_MESSAGE.RUNNING;

                                            cnt_running ++;
                                        }
                                        else
                                            ;

                                        indx_queue++;
                                    }
                                }

                                DbTSQLInterface.ExecNonQuery(ref s_dbConn, toPost, null, null, out err);

                                if (!(err == 0))
                                { //Ошибка при записи сообщений...
                                    retryQueueMessage ();

                                    disconnect();
                                    m_evtConnSett.Reset ();
                                }
                                else
                                { //Успех при записи сообщений...
                                    clearQueueMessage ();
                                }

                                ////Отладка!!!
                                //lock (m_objQueueMessage)
                                //{
                                //    Console.WriteLine(@"Логгирование: сообщений на вЫходе=" + m_listQueueMessage.Count);
                                //    foreach (MESSAGE msg in m_listQueueMessage)
                                //        Console.WriteLine(@"Тип сообщения=" + msg.m_state.ToString ());
                                //}
                            }
                            else
                                ; //Нет соединения с БД
                            break;
                        case LOG_MODE.FILE_EXE:
                        case LOG_MODE.FILE_DESKTOP:
                        case LOG_MODE.FILE_LOCALDEV:
                        case LOG_MODE.FILE_NETDEV:
                            bool locking = false;
                            if ((!(m_listQueueMessage == null)) && (m_listQueueMessage.Count > 0))
                            {
                                lock (m_objQueueMessage)
                                {
                                    int indx_queue = 0;
                                    while (indx_queue < m_listQueueMessage.Count)
                                    {
                                        if (m_listQueueMessage[indx_queue].m_bSeparator == true)
                                            toPost += MessageSeparator + Environment.NewLine;
                                        else
                                            ;

                                        if (m_listQueueMessage[indx_queue].m_bDatetimeStamp == true)
                                        {
                                            toPost += m_listQueueMessage[indx_queue].m_strDatetimeReg + Environment.NewLine;
                                            toPost += DatetimeStampSeparator + Environment.NewLine;
                                        }
                                        else
                                            ;

                                        toPost += m_listQueueMessage[indx_queue].m_text + Environment.NewLine;

                                        if (m_listQueueMessage.Count == 1)
                                            locking = m_listQueueMessage[indx_queue].m_bLockFile;
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

                                        m_listQueueMessage[indx_queue].m_state = STATE_MESSAGE.RUNNING;

                                        indx_queue ++;
                                    }
                                }

                                if (locking == true)
                                {
                                    LogLock();
                                    LogCheckRotate();
                                }
                                else
                                    ;

                                if (File.Exists(m_fileName) == true)
                                {
                                    try
                                    {
                                        if ((m_sw == null) || (m_fi == null))
                                        {
                                            //Вариант №1
                                            //FileInfo f = new FileInfo(m_fileName);
                                            //FileStream fs = f.Open(FileMode.Append, FileAccess.Write, FileShare.Write);
                                            //m_sw = new LogStreamWriter(fs, Encoding.GetEncoding("windows-1251"));
                                            //Вариант №2                        
                                            m_sw = new LogStreamWriter(m_fileName, true, Encoding.GetEncoding("windows-1251"));

                                            m_fi = new FileInfo(m_fileName);
                                        }
                                        else
                                            ;

                                        m_sw.Write(toPost);
                                        m_sw.Flush();
                                    }
                                    catch (Exception e)
                                    {
                                        retryQueueMessage ();
                                        
                                        /*m_sw.Close ();*/
                                        m_sw = null;
                                        m_fi = null;
                                    }
                                }
                                else
                                    retryQueueMessage();

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
                                    LogUnlock();
                                else
                                    ;
                            }
                            else
                            {
                            }
                            break;
                        case LOG_MODE.UNKNOWN:
                        default:
                            break;
                    }
                }
                else
                    ;

                if (indx_semathread == INDEX_SEMATHREAD.STOP)
                    break;
                else {
                    m_arEvtThread[(int)INDEX_SEMATHREAD.MSG].Reset();                    
                }
            }
        }

        private void retryQueueMessage () {
            lock (m_objQueueMessage)
            {
                //Постановка ПОВТОРно сообщений в очередь
                foreach (MESSAGE msg in m_listQueueMessage)
                    if (msg.m_state == STATE_MESSAGE.RUNNING)
                        msg.m_state = STATE_MESSAGE.QUEUE;
                    else
                        ;
            }
        }

        private void clearQueueMessage () {
            lock (m_objQueueMessage)
            {
                //Найти обработанные сообщения
                List<int> listIndxMsgRunning = new List<int>();
                foreach (MESSAGE msg in m_listQueueMessage)
                    if (msg.m_state == STATE_MESSAGE.RUNNING)
                        listIndxMsgRunning.Add(m_listQueueMessage.IndexOf (msg));
                    else
                        ;

                //Сортировать список индексов в ОБРАТном порядке
                // для удаления сообщений из списка по ИНДЕКСу
                listIndxMsgRunning.Sort(delegate(int i1, int i2) { return i1 > i2 ? -1 : 1; });

                //Удалить обработанные сообщения
                foreach (int indx in listIndxMsgRunning)
                    m_listQueueMessage.RemoveAt(indx);
            }
        }

        private void TimerConnSett_Tick (object par)
        //private void TimerConnSett_Tick(object par, EventArgs ev)
        {
            if (m_evtConnSett.WaitOne (0, true) == false)
                if (connect() == 0)
                {
                    m_evtConnSett.Set();
                    m_arEvtThread[(int)INDEX_SEMATHREAD.MSG].Set ();
                }
                else {
                    disconnect();
                    m_evtConnSett.Reset();
                }
            else
                ;
        }

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="name">имя лог-файла</param>
        /// <param name="extLog">признак - внешнее логгирование</param>
        /// <param name="updateLogText">функция записи во внешний лог-файл</param>
        /// <param name="clearLogText">функция очистки внешнего лог-файла</param>
        //private Logging(string name, bool extLog, DelegateStringFunc updateLogText, DelegateFunc clearLogText)
        private Logging(string name)
        {
            //externalLog = extLog;
            logRotateSize = logRotateSizeDefault;
            logRotateFiles = logRotateFilesDefault;
            m_fileNameStart = m_fileName = name;
            sema = new Semaphore(1, 1);

            try {
                m_sw = new LogStreamWriter(m_fileName, true, Encoding.GetEncoding("windows-1251"));
                m_fi = new FileInfo(m_fileName);
            }
            catch (Exception e) {
                //Нельзя сообщить программе...
                //throw new Exception(@"private Logging::Logging () - ...", e);
                ProgramBase.Abort ();
            }

            logRotateCheckLast = DateTime.Now;
            logRotateChekMaxSeconds = 60;

            //logIndex = 0;
            //delegateUpdateLogText = updateLogText;
            //delegateClearLogText = clearLogText;

            start ();
        }

        private Logging () {
            start();
        }
        /// <summary>
        /// Обновляет параметры журналирования "присоединенных" типов сообщений
        /// , присоединение/отсоединение ('Link'/'UnLink')
        /// </summary>
        public static void UpdateMarkDebugLog()
        {
            bool bMarked = false;
            for (int i = 0; i < s_arDebugLogMessageIds.Length; i++)
            {
                if (! (s_arDebugLogMessageIds[i] == (int)INDEX_MESSAGE.NOT_SET))
                {
                    bMarked = false;
                    if (!(DelegateGetINIParametersOfKEY == null))
                        ; //bMarked = bool.Parse(FormMainBase.DelegateGetINIParametersOfKey(...));
                    else
                        if (!(DelegateGetINIParametersOfID == null))
                            bMarked = bool.Parse(DelegateGetINIParametersOfID(s_arDebugLogMessageIds[i]));
                        else
                            ;
                    s_markDebugLog.Set(i, bMarked);
                }
                else
                    ;
            }
        }

        /// <summary>
        /// Приостановка логгирования
        /// </summary>
        /// <returns>строка с именем лог-файла</returns>
        public string Suspend()
        {
            switch (s_mode) {
                case LOG_MODE.FILE_EXE:
                case LOG_MODE.FILE_DESKTOP:
                case LOG_MODE.FILE_LOCALDEV:
                case LOG_MODE.FILE_NETDEV:
                    LogLock();

                    Debug("Пауза ведения журнала...", Logging.INDEX_MESSAGE.NOT_SET, false);

                    m_sw.Close();
                    break;
                case LOG_MODE.DB:
                    Debug("Пауза ведения журнала...", Logging.INDEX_MESSAGE.NOT_SET, false);
                    break;
                case LOG_MODE.UNKNOWN:
                default:
                    break;
            }

            return m_fi.FullName;
        }

        /// <summary>
        /// Восстановление гоггирования
        /// </summary>
        public void Resume()
        {
            switch (s_mode)
            {
                case LOG_MODE.FILE_EXE:
                case LOG_MODE.FILE_DESKTOP:
                case LOG_MODE.FILE_LOCALDEV:
                case LOG_MODE.FILE_NETDEV:
                    m_sw = new LogStreamWriter(m_fi.FullName, true, Encoding.GetEncoding("windows-1251"));

                    Debug("Возобновление ведения журнала...", Logging.INDEX_MESSAGE.NOT_SET, false);

                    LogUnlock();
                    break;
                case LOG_MODE.DB:
                    Debug("Возобновление ведения журнала...", Logging.INDEX_MESSAGE.NOT_SET, false);
                    break;
                case LOG_MODE.UNKNOWN:
                default:
                    break;
            }
        }

        /// <summary>
        /// Блокирование лог-файла для изменения содержания
        /// </summary>
        public void LogLock()
        {
            sema.WaitOne();
        }

        /// <summary>
        /// Разблокирование лог-файла после изменения содержания
        /// </summary>
        public void LogUnlock()
        {
            sema.Release();
        }

        private enum STATE_MESSAGE { UNKNOWN, QUEUE, RUNNING };
        private enum INDEX_SEMATHREAD {MSG, STOP};
        private class MESSAGE {
            public int m_id;
            public STATE_MESSAGE m_state;
            public string m_strDatetimeReg;
            public string m_text;
            
            public bool m_bSeparator
                , m_bDatetimeStamp
                , m_bLockFile;

            public MESSAGE (int id, DateTime dtReg, string text, bool bSep, bool bDatetime, bool bLock) {
                m_id = id;
                m_state = STATE_MESSAGE.QUEUE;
                m_strDatetimeReg = dtReg.ToString (s_strDatetimeFrmt);
                m_text = text;

                m_bSeparator= bSep;
                m_bDatetimeStamp = bDatetime;
                m_bLockFile = bLock;
            }
        }

        private void addMessage (int id_msg, string msg, bool bSep, bool bDatetime, bool bLock) {
            if (m_listQueueMessage == null) m_listQueueMessage = new List<MESSAGE>(); else ;
            lock (m_objQueueMessage) {
                if (m_listQueueMessage.Count > MAXCOUNT_LISTQUEUEMESSAGE)
                    m_listQueueMessage.RemoveAt (0);
                else
                    ;

                m_listQueueMessage.Add(new MESSAGE((int)id_msg, HDateTime.ToMoscowTimeZone (DateTime.Now), msg, bSep, bDatetime, bLock));
            }
        }

        private string getInsertQuery (MESSAGE msg) {
            return @"INSERT INTO [dbo].[logging]([ID_LOGMSG],[ID_APP],[ID_USER],[DATETIME_WR],[MESSAGE], [INSERT_DATETIME])VALUES" +
                                @"(" + msg.m_id + @"," + ProgramBase.s_iAppID + @"," + HUsers.Id + @",'" + msg.m_strDatetimeReg + @"','" + msg.m_text.Replace ('\'', '`') + @"', GETDATE())";
        }

        private string getInsertQuery(int id, string text)
        {
            return @"INSERT INTO [dbo].[logging]([ID_LOGMSG],[ID_APP],[ID_USER],[DATETIME_WR],[MESSAGE], [INSERT_DATETIME])VALUES" +
                                @"(" + id + @"," + ProgramBase.s_iAppID + @"," + HUsers.Id + @",GETDATE (),'" + text.Replace('\'', '`') + @"', GETDATE())";
        }
        
        /// <summary>
        /// Запись сообщения в лог-файл
        /// </summary>
        /// <param name="message">сообщение</param>
        /// <param name="separator">признак наличия разделителя</param>
        /// <param name="timeStamp">признак наличия метки времени</param>
        /// <param name="locking">признак блокирования при записи сообщения</param>
        private void post(ID_MESSAGE id, string message, bool separator, bool timeStamp, bool locking/* = false*/)
        {
            if (s_mode > LOG_MODE.UNKNOWN)
            {
                bool bAddMessage = false;

                switch (s_mode)
                {
                    case LOG_MODE.DB:
                        //...запомнить очередное сообщение...
                        addMessage((int)id, message, true, true, true); //3 крайних параметра для БД ничего не значат...

                        if (m_evtConnSett.WaitOne (0, true) == true) {
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
                    if (m_arEvtThread[(int)INDEX_SEMATHREAD.MSG].WaitOne(0, true) == false)
                        //Не установлен - отправить сообщения...
                        m_arEvtThread[(int)INDEX_SEMATHREAD.MSG].Set();
                    else
                        ; //Установлен - ...
                else
                    ;
            }
            else
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
        private string LogFileName(int indxArchive)
        {
            string strRes = string.Empty;
            if (indxArchive == 0)
                strRes = Path.GetDirectoryName(m_fileName) + "\\" + Path.GetFileNameWithoutExtension(m_fileName) + Path.GetExtension(m_fileName);
            else
                strRes = Path.GetDirectoryName(m_fileName) + "\\" + Path.GetFileNameWithoutExtension(m_fileName) + indxArchive.ToString() + Path.GetExtension(m_fileName);

            return strRes;
        }

        private void LogRotateNowLocked()
        {
            //if (externalLog == true)
            //    delegateClearLogText();
            //else
            //    ;

            try
            {
                m_sw.Close();

                //logIndex = (logIndex + 1) % logRotateFiles;
                //m_fileName = LogFileName();

                LogToArchive ();

                m_sw = new LogStreamWriter(m_fileName, false, Encoding.GetEncoding("windows-1251"));
                m_fi = new FileInfo(m_fileName);
            }
            catch (Exception e)
            {
                /*m_sw.Close ();*/
                m_sw = null;
                m_fi = null;
            }
        }

        private void LogToArchive (int indxArchive = 0) {
            string logFileName = LogFileName (indxArchive),
                logToFileName = LogFileName(++ indxArchive);

            if (File.Exists(logToFileName) == true)
            {
                if (! (indxArchive > (MAX_ARCHIVE - 1)))
                    LogToArchive(indxArchive);
                else
                    File.Delete(logToFileName);
            }
            else {
            }

            if (File.Exists(logToFileName) == false)
                File.Create (logToFileName).Close ();
            else
                ;

            File.Copy(logFileName, logToFileName, true);
        }

        private void LogRotateNow()
        {
            LogLock();
            LogRotateNowLocked();
            LogUnlock();
        }

        private void LogCheckRotate()
        {
            logRotateCheckLast = DateTime.Now;

            if (!(m_fi == null))
            {
                if (File.Exists (m_fileName) == true)
                    try {
                        m_fi.Refresh();

                        if (m_fi.Length > logRotateSize)
                            LogRotateNowLocked();
                        else
                            ;
                    }
                    catch (Exception e)
                    {
                        //m_fi = null;
                    }
                else
                    ;
            }
            else
                ;
        }

        public bool LogRotate
        {
            get { return logRotate; }
            set { logRotate = value; }
        }

        public int LogRotateMaxSize
        {
            get { return logRotateSize; }
            set 
            {
                if (value <= 0 || value > logRotateSizeMax)
                    logRotateSize = logRotateSizeMax;
                else
                    logRotateSize = value;
            }
        }

        public int LogRotateFiles
        {
            get { return logRotateFiles; }
            set
            {
                if (value <= 0 || value > logRotateFilesMax)
                    logRotateFiles = logRotateFilesMax;
                else
                    logRotateFiles = value;
            }
        }

        private bool post (INDEX_MESSAGE indx)
        {
            bool bRes = indx == INDEX_MESSAGE.NOT_SET;
            if (bRes == false)
                bRes = s_markDebugLog.IsMarked ((int)indx);
            else
                ;

            return bRes;
        }

        public void PostStart (string message) {
            if (s_mode == Logging.LOG_MODE.UNKNOWN)
                s_mode = Logging.LOG_MODE.FILE_EXE;
            else ;

            post(ID_MESSAGE.START, message, true, true, true);
        }

        public void Action(string message, INDEX_MESSAGE indx, bool bLock = true)
        {
            if (post (indx) == true)
                post(ID_MESSAGE.ACTION, "!Действие!: " + message, true, true, bLock);
            else
                ;
        }

        public void Error(string message, INDEX_MESSAGE indx, bool bLock = true)
        {
            if (post(indx) == true)
                post(ID_MESSAGE.ERROR, "!Ошибка!: " + message, true, true, bLock);
            else
                ;
        }

        public void Warning(string message, INDEX_MESSAGE indx, bool bLock = true)
        {
            if (post (indx) == true)
                post(ID_MESSAGE.WARNING, "!Предупреждение!: " + message, true, true, bLock);
            else
                ;
        }

        public void Debug(string message, INDEX_MESSAGE indx, bool bLock = true)
        {
            if (post(indx) == true)
                post(ID_MESSAGE.DEBUG, "!Отладка!: " + message, true, true, bLock);
            else
                ;
        }

        public void Exception(Exception e, string message, INDEX_MESSAGE indx, bool bLock = true)
        {
            if (post (indx) == true)
            {
                string msg = string.Empty;
                msg += "!Исключение! обработка: " + message + Environment.NewLine;
                msg += "Исключение: " + e.Message + Environment.NewLine;
                msg += e.ToString();

                post(ID_MESSAGE.EXCEPTION, msg, true, true, bLock);
            }
            else
                ;
        }

        public void ExceptionDB(string message)
        {
            post(ID_MESSAGE.EXCEPTION_DB, message, true, true, true);
        }

        internal class LogStreamWriter : StreamWriter
        {
            /*
            public LogStreamWriter(FileStream fs, System.Text.Encoding e)
                : base(fs, e)
            {
            }
            */

            public LogStreamWriter(string path, bool append, System.Text.Encoding e)
                : base(path, append, e)
            {
            }

            ~LogStreamWriter()
            {
                this.Dispose();
            }

            protected override void Dispose(bool disposing)
            {
                try { base.Dispose(disposing); }
                catch (Exception e) { }
            }
        }
    }
}
