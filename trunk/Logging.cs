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
        public enum LOG_MODE { ERROR = -1, UNKNOWN, FILE, DB };
        public enum ID_MESSAGE { START = 1, STOP, ACTION, DEBUG, EXCEPTION, EXCEPTION_DB, ERROR, WARNING };

        private int MAX_COUNT_MESSAGE_ONETIME = 66;
        
        private string m_fileNameStart;
        private string m_fileName;
        //private bool externalLog;
        //private DelegateStringFunc delegateUpdateLogText;
        //private DelegateFunc delegateClearLogText;
        private Semaphore sema;
        private LogStreamWriter m_sw;
        private FileInfo m_fi;
        public static LOG_MODE s_mode = LOG_MODE.UNKNOWN;
        private bool logRotate = true;
        private const int MAX_ARCHIVE = 6;
        private static int logRotateSizeDefault = (int) Math.Floor((double)(1024 * 1024 * 5)); //1024 * 1024 * 5;
        private static int logRotateSizeMax = (int) Math.Floor((double)(1024 * 1024 * 10)); //1024 * 1024 * 1024;
        private int logRotateSize;
        //private int logIndex;
        private const int logRotateFilesDefault = 1;
        private const int logRotateFilesMax = 100;
        private int logRotateFiles;

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
        private System.Threading.Timer m_timerConnSett;
        private ManualResetEvent [] m_arEvtThread;
        private ManualResetEvent m_evtConnSett;

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
                    case LOG_MODE.FILE:
                        //m_this = new Logging(System.Environment.CurrentDirectory + @"\" + AppName + "_" + Environment.MachineName + "_log.txt", false, null, null);
                        m_this = new Logging(System.Environment.CurrentDirectory + @"\" + AppName + "_" + Environment.MachineName + "_log.txt");
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

        private void start () {
            m_arEvtThread = new ManualResetEvent[] { new ManualResetEvent(false), new ManualResetEvent(false) };            

            m_objQueueMessage = new object ();

            m_threadPost = new Thread (new ParameterizedThreadStart (threadPost));
            m_threadPost.Name = @"Логгирование приложения..." + AppName;
            m_threadPost.IsBackground = true;
            m_threadPost.Start();

            if (s_mode == LOG_MODE.DB) {
                m_evtConnSett = new ManualResetEvent(false);
                m_timerConnSett = new System.Threading.Timer (TimerConnSett_Tick, null, 0, 6666);
            }
            else
                ;
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
                m_timerConnSett.Dispose ();
                m_timerConnSett = null;

                disconnect();
                m_evtConnSett.Reset();
            }
            else
                if ((s_mode == LOG_MODE.FILE) || (s_mode == LOG_MODE.UNKNOWN)) {
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
            while (true) {
                INDEX_SEMATHREAD indx_semathread = (INDEX_SEMATHREAD)WaitHandle.WaitAny(m_arEvtThread);

                //Отправление сообщений...
                int err = -1;
                string toPost = string.Empty;

                if (m_listQueueMessage.Count > 0)
                {
                    switch (s_mode) {
                        case LOG_MODE.DB:
                            if (m_evtConnSett.WaitOne (0) == true)
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
                                    lock (m_objQueueMessage)
                                    {
                                        //Постановка ПОВТОРно сообщений в очередь
                                        foreach (MESSAGE msg in m_listQueueMessage)
                                            if (msg.m_state == STATE_MESSAGE.RUNNING)
                                                msg.m_state = STATE_MESSAGE.QUEUE;
                                            else
                                                ;
                                    }

                                    disconnect();
                                    m_evtConnSett.Reset ();
                                }
                                else
                                { //Успех при записи сообщений...
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
                        case LOG_MODE.FILE:
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
                                        /*m_sw.Close ();*/
                                        m_sw = null;
                                        m_fi = null;
                                    }
                                }
                                else
                                    ;

                                //if (externalLog == true)
                                //{
                                //    if (timeStamp == true)
                                //        delegateUpdateLogText(DateTime.Now.ToString() + ": " + message + Environment.NewLine);
                                //    else
                                //        delegateUpdateLogText(message + Environment.NewLine);
                                //}
                                //else
                                //    ;

                                lock (m_objQueueMessage) {
                                    int indx_queue = 0;
                                    m_listQueueMessage.RemoveAt (indx_queue);
                                }

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

        private void TimerConnSett_Tick (object par) {
            if (m_evtConnSett.WaitOne (0) == false)
                if (connect() == 0)
                    m_evtConnSett.Set();
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

            //logIndex = 0;
            //delegateUpdateLogText = updateLogText;
            //delegateClearLogText = clearLogText;

            start ();
        }

        private Logging () {
            start();
        }

        /// <summary>
        /// Приостановка логгирования
        /// </summary>
        /// <returns>строка с именем лог-файла</returns>
        public string Suspend()
        {
            switch (s_mode) {
                case LOG_MODE.FILE:
                    LogLock();

                    Debug("Пауза ведения журнала...", false);

                    m_sw.Close();
                    break;
                case LOG_MODE.DB:
                    Debug("Пауза ведения журнала...", false);
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
                case LOG_MODE.FILE:
                    m_sw = new LogStreamWriter(m_fi.FullName, true, Encoding.GetEncoding("windows-1251"));

                    Debug("Возобновление ведения журнала...", false);

                    LogUnlock();
                    break;
                case LOG_MODE.DB:
                    Debug("Возобновление ведения журнала...", false);
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
                m_strDatetimeReg = dtReg.ToString (@"yyyyMMdd HH:mm:ss.fff");
                m_text = text;

                m_bSeparator= bSep;
                m_bDatetimeStamp = bDatetime;
                m_bLockFile = bLock;
            }
        }

        private void addMessage (int id_msg, string msg, bool bSep, bool bDatetime, bool bLock) {
            if (m_listQueueMessage == null) m_listQueueMessage = new List<MESSAGE>(); else ;
            lock (m_objQueueMessage) { m_listQueueMessage.Add(new MESSAGE((int)id_msg, DateTime.Now, msg, bSep, bDatetime, bLock)); }
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
        public void Post(ID_MESSAGE id, string message, bool separator, bool timeStamp, bool locking/* = false*/)
        {
            if (s_mode > LOG_MODE.UNKNOWN)
            {
                bool bAddMessage = false;

                switch (s_mode)
                {
                    case LOG_MODE.DB:
                        //...запомнить очередное сообщение...
                        addMessage((int)id, message, true, true, true); //3 крайних параметра для БД ничего не значат...

                        if (m_evtConnSett.WaitOne (0) == true) {
                            //Установить признак возможности для отправки
                            bAddMessage = true;
                        } else {
                        }
                        break;
                    case LOG_MODE.FILE:
                        addMessage ((int)id, message, separator, timeStamp, locking); //3 крайних параметра для БД ничего не значат...
                        //Установить признак возможности для отправки
                        bAddMessage = true;
                        break;
                    default:
                        break;
                }

                if (bAddMessage == true)
                    if (m_arEvtThread[(int)INDEX_SEMATHREAD.MSG].WaitOne(0) == false)
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

        public void Action(string message, bool bLock = true)
        {
            Post(ID_MESSAGE.ACTION, "!Действие!: " + message, true, true, bLock);
        }

        public void Error(string message, bool bLock = true)
        {
            Post(ID_MESSAGE.ERROR, "!Ошибка!: " + message, true, true, bLock);
        }

        public void Warning(string message, bool bLock = true)
        {
            Post(ID_MESSAGE.WARNING, "!Предупреждение!: " + message, true, true, bLock);
        }

        public void Debug(string message, bool bLock = true)
        {
            Post(ID_MESSAGE.DEBUG, "!Отладка!: " + message, true, true, bLock);
        }

        public void Exception(Exception e, string message, bool bLock = true)
        {
            string msg = string.Empty;
            msg += "!Исключение! обработка: " + message + Environment.NewLine;
            msg += "Исключение: " + e.Message + Environment.NewLine;
            msg += e.ToString();

            Post(ID_MESSAGE.EXCEPTION, msg, true, true, bLock);
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
