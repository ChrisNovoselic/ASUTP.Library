using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Threading;
using System.Data.Common;
using System.Data.OleDb;
using System.Data.SqlClient;
using MySql.Data.MySqlClient;
using System.ComponentModel;

//namespace HClassLibrary
namespace HClassLibrary
{
    /// <summary>
    /// Класс для описания объекта для обращения к БД нескольких подписчиков
    /// </summary>
    public abstract class DbInterface
    {
        /// <summary>
        /// Перечисление - типы поддерживаемых интерфейсов
        /// </summary>
        public enum DB_TSQL_INTERFACE_TYPE
        {
            MySQL
            , MSSQL
            , MSExcel
            , ModesCentre
            , Oracle
            , UNKNOWN
        }

        public static volatile int MAX_RETRY = 3;
        public static volatile int MAX_WAIT_COUNT = 39;
        public static volatile int WAIT_TIME_MS = 106;

        public static int MAX_WATING
        {
            get {
                return MAX_RETRY * MAX_WAIT_COUNT * WAIT_TIME_MS;
                //return 6666;
            }
        }
        /// <summary>
        /// Перечисление - возможные состояния подписчика на выполнение запросов
        /// </summary>
        public enum STATE_LISTENER
        {
            /// <summary>
            /// Зарегистрирован
            /// </summary>
            READY
            /// <summary>
            /// Поставлен в 
            /// </summary>
            , REQUEST
            /// <summary>
            /// Занят, выполняется запрос
            /// </summary>
            , BUSY
        }
        /// <summary>
        /// Класс для описания подписчика еа выполнение запросов к БД
        /// </summary>
        protected class DbInterfaceListener
        {
            /// <summary>
            /// Состояние подписчика
            /// </summary>
            public volatile STATE_LISTENER state;
            /// <summary>
            /// Признак наличия данных в результате запроса
            /// </summary>
            public volatile bool dataPresent;
            /// <summary>
            /// Признак ошибки по результатам запроса
            /// </summary>
            public volatile bool dataError;
            /// <summary>
            /// Строка запроса на получение данных
            /// </summary>
            public volatile object requestDB;
            /// <summary>
            /// Результат запроса
            /// </summary>
            public volatile DataTable dataTable;
            /// <summary>
            /// конструктор - основной (без аргументов)
            /// </summary>
            public DbInterfaceListener()
            {
                state = STATE_LISTENER.READY;
                dataPresent =
                dataError =
                    false;

                requestDB = null; //new object ();
                dataTable = new DataTable();
            }
        }
        /// <summary>
        /// Словарь с подписчиками для выполнения запросов
        /// </summary>
        protected Dictionary<int, DbInterfaceListener> m_dictListeners;
        /// <summary>
        /// Количество подписчиков в интерфейсе
        /// </summary>
        public int ListenerCount { get { return m_dictListeners.Count; } }
        /// <summary>
        /// Объект соединения с БД
        /// </summary>
        public object m_connectionSettings;
        /// <summary>
        /// Объект для синхпрнизации доступа к подписчикам
        /// </summary>
        protected object lockListeners;
        /// <summary>
        /// Объект для синхронизации изменения параметров соединения
        /// </summary>
        protected object lockConnectionSettings;

        private Thread dbThread;
        private Semaphore sem;
        private volatile bool threadIsWorking;
        /// <summary>
        /// Наименование интерфейса
        /// </summary>
        public string Name
        {
            get { return dbThread.Name; }
        }
        /// <summary>
        /// Признак необходимости выполнить повторное соединение с БД
        /// </summary>
        protected bool needReconnect;
        /// <summary>
        /// Признак установленного соединения с БД
        /// </summary>
        private bool connected;
        /// <summary>
        /// Конструктор - основной (с аргументом)
        /// </summary>
        /// <param name="name">Наименование интерфейса (рабочего потока)</param>
        public DbInterface(string name)
        {
            lockListeners = new object();
            lockConnectionSettings = new object();

            //listeners = new DbInterfaceListener[maxListeners];
            m_dictListeners = new Dictionary<int, DbInterfaceListener>();

            connected = false;
            needReconnect = false;

            dbThread = new Thread(new ParameterizedThreadStart(DbInterface_ThreadFunction));
            dbThread.Name = name;
            //Name = name;
            dbThread.IsBackground = true;

            sem = new Semaphore(0, 1);
        }

        public bool Connected
        {
            get { return connected; }
        }
        /// <summary>
        /// Зарегистрировать нового подписчика, вернуть его идентификатор
        /// </summary>
        /// <returns>Идентификатор нового подписчика</returns>
        public int ListenerRegister()
        {
            int i = -1;

            lock (lockListeners) {
                for (i = 0; i < m_dictListeners.Count; i++) {
                    if (m_dictListeners.ContainsKey(i) == false)
                        break;
                    else
                        ;
                }
                m_dictListeners.Add(i, new DbInterfaceListener());

                return i;
            }

            //return -1;
        }
        /// <summary>
        /// Отменить регистрацию подписчика по идентификатору
        /// </summary>
        /// <param name="listenerId"></param>
        public void ListenerUnregister(int listenerId)
        {
            if (m_dictListeners.ContainsKey(listenerId) == false)
                return;

            foreach (DbInterfaceListener listener in m_dictListeners.Values)
                if (listener.state == STATE_LISTENER.BUSY) {
                    ////if (m_threadGetData.CancellationPending == false)
                    //    m_threadGetData.CancelAsync();
                    ////else
                    ////    ;
                    m_eventGetDataBreak.Set();

                    break;
                } else
                    ;

            lock (lockListeners) {                
                m_dictListeners.Remove(listenerId);                
            }
        }
        /// <summary>
        /// Запустить поток выполнения запросов
        /// </summary>
        public void Start()
        {
            threadIsWorking = true;
            //sem.WaitOne();
            dbThread.Start();
        }
        /// <summary>
        /// Остановить выполнение всех запросов
        /// </summary>
        public void Stop()
        {
            bool joined;
            threadIsWorking = false;
            lock (lockListeners) {
                foreach (KeyValuePair<int, DbInterfaceListener> pair in m_dictListeners) {
                    pair.Value.requestDB = null;
                    pair.Value.state = STATE_LISTENER.READY;
                }
            }
            if (dbThread.IsAlive == true) {
                try {
                    if (!(WaitHandle.WaitAny(new WaitHandle[] { sem }, 0) == 0))
                        sem.Release(1);
                    else
                        ;
                } catch (Exception e) {
                    Logging.Logg().Exception(e, @"DbInterface::Stop () - ...", Logging.INDEX_MESSAGE.NOT_SET);
                }

                joined = dbThread.Join(6666);
                if (!joined)
                    dbThread.Abort();
                else
                    ;
            } else
                ;
        }

        public void Request(int listenerId, string request)
        {
            //Logging.Logg().Debug(@"DbInterface::Request (int, string) - listenerId=" + listenerId.ToString() + @", request=" + request);

            lock (lockListeners) {
                if ((m_dictListeners.ContainsKey(listenerId) == false) || (request.Length == 0))
                    return;
                else
                    ;

                m_dictListeners[listenerId].requestDB = request;
                m_dictListeners[listenerId].state = STATE_LISTENER.REQUEST;
                m_dictListeners[listenerId].dataPresent = false;
                m_dictListeners[listenerId].dataError = false;

                try {
                    if (!(WaitHandle.WaitAny(new WaitHandle[] { sem }, 0) == 0/*WaitHandle.WaitTimeout*/))
                        sem.Release(1);
                    else
                        ;
                } catch (Exception e) {
                    Logging.Logg().Exception(e, @"DbInterface::Request (int, string)", Logging.INDEX_MESSAGE.NOT_SET);
                }
            }

            //Logging.Logg().Debug(@"DbInterface::Request (int, string) - " + listenerId + @", " + request);
        }

        public int Response(int listenerId, out bool error, out DataTable table)
        {
            int iRes = -1;

            lock (lockListeners) {
                if ((m_dictListeners.ContainsKey(listenerId) == false) || listenerId < 0) {
                    error = true;
                    table = null;

                    iRes = -1;
                } else {
                    error = m_dictListeners[listenerId].dataError;
                    table = m_dictListeners[listenerId].dataTable;

                    iRes = m_dictListeners[listenerId].dataPresent == true ? 0 : -1;
                }
            }

            //Logging.Logg().Debug(@"DbInterface::Response (int, out bool , out DataTable) - listenerId = " + listenerId + @", error = " + error.ToString() + @", m_dictListeners[listenerId].dataPresent = " + m_dictListeners[listenerId].dataPresent);

            return iRes;
        }

        protected void SetConnectionSettings()
        {
            try {
                sem.Release(1);
            } catch (Exception e) {
                Logging.Logg().Exception(e, "DbInterface::SetConnectionSettings () - обращение к переменной sem (вызов: sem.Release ())", Logging.INDEX_MESSAGE.NOT_SET);
            }
        }

        public abstract void SetConnectionSettings(object cs, bool bStarted);

        //BackgroundWorker m_threadGetData;
        private AutoResetEvent m_eventGetDataBreak;

        private void DbInterface_ThreadFunction(object data)
        {
            object request;
            bool result;
            bool reconnection/* = false*/;
            Thread threadGetData;
            WaitHandle[] waitHandleGetData = new WaitHandle[] { new AutoResetEvent(false), m_eventGetDataBreak = new AutoResetEvent(false) };
            int iGetData = -1;

            //m_threadGetData = new BackgroundWorker() {
            //    WorkerReportsProgress = true
            //    , WorkerSupportsCancellation = true
            //};
            //m_threadGetData.DoWork += new DoWorkEventHandler((object sender, DoWorkEventArgs e) => {
            //    e.Result = GetData((e.Argument as object[])[0] as DataTable, (string)(e.Argument as object[])[1]);
            //});
            //m_threadGetData.RunWorkerCompleted += new RunWorkerCompletedEventHandler((object sender, RunWorkerCompletedEventArgs e) => {
            //    if (e.Cancelled == false) {
            //        result = (bool)e.Result;

            //        (waitHandleGetData[0] as AutoResetEvent).Set();
            //    } else
            //        (waitHandleGetData[1] as AutoResetEvent).Set();
            //});

            while (threadIsWorking) {
                sem.WaitOne();

                lock (lockConnectionSettings) // атомарно читаю и сбрасываю флаг, чтобы при параллельной смене настроек не сбросить повторно выставленный флаг
                {
                    reconnection = needReconnect;
                    needReconnect = false;
                }

                if (reconnection == true) {
                    Disconnect();
                    connected = false;
                    if ((threadIsWorking == true) && (Connect() == true))
                        connected = true;
                    else
                        needReconnect = true; // выставлять флаг можно без блокировки
                } else
                    ;

                if (connected == false) // не удалось подключиться - не пытаемся получить данные
                    continue;
                else
                    ;

                //Logging.Logg().Debug("DbInterface::DbInterface_ThreadFunction () - m_listListeners.Count = " + m_listListeners.Count);

                lock (lockListeners) {
                    foreach (KeyValuePair<int, DbInterfaceListener> pair in m_dictListeners) {
                        if (iGetData > 0)
                            break;
                        else
                            ;

                        //lock (lockListeners)
                        //{
                        if (pair.Value.state == STATE_LISTENER.READY) {
                            continue;
                        } else
                            ;

                        request = pair.Value.requestDB;

                        if ((request == null)
                            || (!(request.ToString().Length > 0))) {
                            continue;
                        } else
                            ;
                        //}

                        result = false;
                        pair.Value.state = STATE_LISTENER.BUSY;

                        //Logging.Logg().Debug("DbInterface::DbInterface_ThreadFunction () - GetData(...) - request = " + request);

                        try {
                            //m_threadGetData.RunWorkerAsync(new object[] { pair.Value.dataTable, request });
                            threadGetData = new Thread(new ParameterizedThreadStart((obj) => {
                                result = GetData(pair.Value.dataTable, request);

                                (obj as AutoResetEvent).Set();
                            })) { IsBackground = true };
                            threadGetData.Start(waitHandleGetData[0]);

                            iGetData = WaitHandle.WaitAny(waitHandleGetData);

                            threadGetData = null;
                        } catch (DbException e) {
                            Logging.Logg().Exception(e
                                , string.Format("DbInterface::DbInterface_ThreadFunction () - result = GetData(...) - request = {0}", request)
                                , Logging.INDEX_MESSAGE.NOT_SET);
                        } finally {
                            if (request == pair.Value.requestDB) {
                                pair.Value.dataPresent = result;
                                pair.Value.dataError = !result;

                                pair.Value.requestDB = null;
                                pair.Value.state = STATE_LISTENER.READY;
                            } else
                                pair.Value.state = STATE_LISTENER.REQUEST;
                        }

                        //Logging.Logg().Debug("DbInterface::DbInterface_ThreadFunction () - result = GetData(...) - pair.Value.dataPresent = " + pair.Value.dataPresent + @", pair.Value.dataError = " + pair.Value.dataError.ToString ());
                        //}
                    }
                }
            }
            try {
                if (!(WaitHandle.WaitAny(new WaitHandle[] { sem }, 0) == 0))
                    sem.Release(1);
                else
                    ;
            } catch (Exception e) {
                Logging.Logg().Exception(e, "DbInterface::DbInterface_ThreadFunction () - выход...", Logging.INDEX_MESSAGE.NOT_SET);
            } finally {
                foreach (WaitHandle eventAuto in waitHandleGetData)
                    eventAuto.Close();
            }

            Disconnect();
        }

        /// <summary>
        /// Установить соединение с БД
        /// </summary>
        /// <returns>Результат попытки установить соединенние с БД</returns>
        protected abstract bool Connect();
        /// <summary>
        /// Разорвать установленное ранее соединение с БД
        /// </summary>
        /// <returns>Результат попытки разорвать соединенние с БД</returns>
        protected abstract bool Disconnect();
        /// <summary>
        /// Разорвать установленное ранее соединение с БД
        /// </summary>
        /// <param name="err">Результат попытки разорвать соединенние с БД</param>
        public abstract void Disconnect(out int err);
        /// <summary>
        /// Заполнить таблицу значениями - результатом запроса
        /// </summary>
        /// <param name="table">Таблица для значений</param>
        /// <param name="query">Запрос для получения значений</param>
        /// <returns>Результат попытки получения значений</returns>
        protected abstract bool GetData(DataTable table, object query);
    }
}
