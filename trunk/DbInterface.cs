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
            /// <summary>My Sql</summary>
            MySQL
            /// <summary>MS SQL</summary>
            , MSSQL
            /// <summary>MS Excel</summary>
            , MSExcel
            /// <summary>Модес - Центр</summary>
            , ModesCentre
            /// <summary>Oracle</summary>
            , Oracle
            /// <summary>Неизвестный тип</summary>
            , UNKNOWN
        }
        /// <summary>
        /// Максимальное количество повторов
        /// </summary>
        public static volatile int MAX_RETRY = 3;
        /// <summary>
        /// Количество попыток проверки наличия результата в одном цикле
        /// </summary>
        public static volatile int MAX_WAIT_COUNT = 39;
        /// <summary>
        /// Интервал ожидания между проверками наличия результата
        ///  , при условии что в предыдущей итерации результат не был получен
        /// </summary>
        public static volatile int WAIT_TIME_MS = 106;
        /// <summary>
        /// Максимальное время ожидания окончания длительно выполняющейся операции
        /// </summary>
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

        /// <summary>
        /// Признак наличия соединения с источником данных
        /// </summary>
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
        /// <param name="listenerId">Идентификатор подписчика</param>
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

                joined = dbThread.Join(MAX_WATING);
                if (!joined)
                    dbThread.Interrupt();
                else
                    ;
            } else
                ;
        }

        /// <summary>
        /// Отправить запрос источнику данных
        /// </summary>
        /// <param name="listenerId">Идентификатор подписчика - инициатора запроса</param>
        /// <param name="request">Строка - содержание запроса</param>
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

        /// <summary>
        /// Получить результат запроса для подписчика с диагностическими признаками
        /// </summary>
        /// <param name="listenerId">Идентификатор подписчика - инициатора запроса</param>
        /// <param name="error">Признак ошибки при получении данных</param>
        /// <param name="table">Таблица - результат запроса к источнику данных</param>
        /// <returns>Результат выполнения метода</returns>
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

        /// <summary>
        /// Применить параметры соединения с источником данных - активировать объект доступа к нему
        /// </summary>
        protected void SetConnectionSettings()
        {
            try {
                sem.Release(1);
            } catch (Exception e) {
                Logging.Logg().Exception(e, "DbInterface::SetConnectionSettings () - обращение к переменной sem (вызов: sem.Release ())", Logging.INDEX_MESSAGE.NOT_SET);
            }
        }

        /// <summary>
        /// Установить параметры соединения с источником данных
        /// </summary>
        /// <param name="cs">Объект с параметрами соединения</param>
        /// <param name="bStarted">Признак немедленной активации объекта доступа к источнику данных</param>
        public abstract void SetConnectionSettings(object cs, bool bStarted);

        /// <summary>
        /// Объект синхронизации для распознования команды на аварийное завершение подпотока получения данных запроса
        /// </summary>
        private AutoResetEvent m_eventGetDataBreak;

        private void DbInterface_ThreadFunction(object data)
        {
            object request;
            bool result;
            bool reconnection/* = false*/;
            Thread threadGetData;
            // Массив объектов синхронизации текущего потока и подпотока ожидания
            // 0 - нормальное завершение, 1 - аврийное завершение
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
                    // в новом цикле - новое состояние для прерывания
                    iGetData = -1;

                    //??? внутри цикла при аварийном прерывании из словаря удаляется элемент
                    foreach (KeyValuePair<int, DbInterfaceListener> pair in m_dictListeners) {
                        //??? если прервали обработку запроса одного из подписчиков, то остальные продолжаем обрабатывать
                        //if (iGetData > 0)
                        //    break;
                        //else
                        //    ;

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
                            threadGetData = new Thread(new ParameterizedThreadStart(delegate (object obj) {
                                try {
                                    result = GetData(pair.Value.dataTable, request);

                                    (obj as AutoResetEvent).Set();
                                } catch (ThreadAbortException ae) {
                                    Logging.Logg().ExceptionDB(string.Format(@"::DbInterface_ThreadFunction () - {1}:{2}{0}{3}{0}{4}"
                                        , Environment.NewLine
                                        , Name, pair.Key
                                        , ((!(ae.Data == null)) && (ae.Data.Count > 0))
                                            ? string.Format(@"внешний объект(индекс-0)={0}", ae.Data[0])
                                                : "внешний объект не указан"
                                        , ae.Message, ae.StackTrace));

                                    Thread.ResetAbort();
                                } catch (Exception e) {
                                    Logging.Logg().ExceptionDB(string.Format(@"DbInterface_ThreadFunction () - {1}:{2}{0}{3}{0}{4}"
                                        , Environment.NewLine
                                        , Name, pair.Key
                                        , e.Message, e.StackTrace));
                                } finally {
                                }
                            })) { // параметры для запуска потока
                                IsBackground = true
                                , Name = string.Format (@"{0}:{1}", Name, pair.Key)
                                , Priority = ThreadPriority.AboveNormal
                            };
                            // запуск потока
                            threadGetData.Start(waitHandleGetData [0]);

                            if ((iGetData = WaitHandle.WaitAny(waitHandleGetData, MAX_WATING)) > 0) {
                                switch (iGetData) {
                                    case WaitHandle.WaitTimeout:
                                        // команда на аварийное завершение
                                        (waitHandleGetData [1] as AutoResetEvent).Set ();
                                        needReconnect = true;
                                        break;
                                    default:                                        
                                        break;
                                }

                                // ждем мсек норм. завершения после исполнения команды на аварийное завершение внутр. потока
                                if (waitHandleGetData [0].WaitOne (WAIT_TIME_MS) == false)
                                    // ждем еще мсек норм. завершения
                                    if (threadGetData.Join (WAIT_TIME_MS) == false) {
                                        // аваавррийно завершаем
                                        threadGetData.Abort (string.Format (@"Аварийное завершение подпотока получения данных..."));
                                        //threadGetData.Interrupt ();
                                        //// перейти к следующему подписчику
                                        //continue;
                                    } else
                                        ;
                                else
                                    ;
                            } else
                                ;
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
