using ASUTP.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;

namespace ASUTP.Database {
    /// <summary>
    /// Класс для описания объекта для обращения к БД нескольких подписчиков
    /// </summary>
    public abstract class DbInterface {
        /// <summary>
        /// Перечисление - типы поддерживаемых интерфейсов
        /// </summary>
        public enum DB_TSQL_INTERFACE_TYPE {
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
        /// Перечисление - возможные состояния подписчика на выполнение запросов
        /// </summary>
        public enum STATE_LISTENER {
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
            private volatile STATE_LISTENER _state;
            /// <summary>
            /// Состояние подписчика
            /// </summary>
            public STATE_LISTENER State
            {
                get
                {
                    return _state;
                }

                set
                {
                    //if ((_state == STATE_LISTENER.READY) && value == STATE_LISTENER.REQUEST) { // штатно
                        
                    ////} else if ((_state == STATE_LISTENER.READY) && value == STATE_LISTENER.BUSY) { // невозможно

                    //} else if ((_state == STATE_LISTENER.REQUEST) && value == STATE_LISTENER.BUSY) { // штатно

                    //} else if ((_state == STATE_LISTENER.REQUEST) && value == STATE_LISTENER.READY) { // только при 'Reset'

                    //} else if ((_state == STATE_LISTENER.BUSY) && value == STATE_LISTENER.READY) { // штатно

                    //} else if ((_state == STATE_LISTENER.BUSY) && value == STATE_LISTENER.REQUEST) { // только при неудачной попытке

                    //} else if ((_state == STATE_LISTENER.READY) && value == STATE_LISTENER.READY) { // только при 'Reset'

                    //} else if ((_state == STATE_LISTENER.REQUEST) && value == STATE_LISTENER.REQUEST) { // только при неудачной попытке

                    ////} else if ((_state == STATE_LISTENER.BUSY) && value == STATE_LISTENER.BUSY) { // невозможно

                    //} else
                    //    ;

                    _state = value;
                }
            }
            ///// <summary>
            ///// Признак наличия данных в результате запроса
            ///// </summary>
            //public volatile bool dataPresent;
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
            public DbInterfaceListener ()
            {
                dataError = false;

                requestDB = null; //new object ();
                _state = STATE_LISTENER.READY;

                dataTable = new DataTable ();
            }

            /// <summary>
            /// Привести объект в исходное состояние
            /// </summary>
            public void Reset ()
            {
                dataError = false;

                requestDB = null;
                State = STATE_LISTENER.READY;

                dataTable = null;
            }

            /// <summary>
            /// Привести объект в состояние для использования в запросе
            /// </summary>
            /// <param name="req">Содержание запроса</param>
            public void SetRequest (string req)
            {
                dataError = false;

                if (Equals(requestDB, null) == false)
                    requestDB = null;
                else
                    ;
                requestDB = req;
                State = STATE_LISTENER.REQUEST;

                if (Equals(dataTable, null) == false)
                    dataTable = null;
                else
                    ;
                dataTable = new DataTable();
            }

            /// <summary>
            /// Установить финальное состояние (после выполнения запроса)
            /// </summary>
            /// <param name="bResult">Результатт выполнения запроса</param>
            public void SetResult (bool bResult)
            {
                dataError = !bResult;

                // requestDB сохранить до очередного назначения
                // , чтобы результат соответствовал запросу
                State = Error == false
                    ? STATE_LISTENER.READY
                        : STATE_LISTENER.REQUEST; // для возможности повтрной обработки(если успеем)
            }

            /// <summary>
            /// Тип запроса (выборка или вставка/обновление/удаление)
            /// </summary>
            public int IsSelected
            {
                get
                {
                    return (Equals (requestDB, null) == true)
                        ? -1
                            : (((string)requestDB).StartsWith ("INSERT", StringComparison.InvariantCultureIgnoreCase) == false)
                                && (((string)requestDB).StartsWith ("UPDATE", StringComparison.InvariantCultureIgnoreCase) == false)
                                && (((string)requestDB).StartsWith ("DELETE", StringComparison.InvariantCultureIgnoreCase) == false)
                                ? 1
                                    : 0;

                }
            }

            public bool Error
            {
                get
                {
                    return (dataError == true)
                        || ((IsSelected == 1)
                            && ((dataTable.Columns.Count == 0)
                                /*|| (dataTable.Rows.Count == 0)*/));
                }
            }
        }
        /// <summary>
        /// Словарь с подписчиками для выполнения запросов
        /// </summary>
        protected Dictionary<int, DbInterfaceListener> m_dictListeners;
        /// <summary>
        /// Количество подписчиков в интерфейсе
        /// </summary>
        public int ListenerCount
        {
            get
            {
                return m_dictListeners.Count;
            }
        }
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
        //private Semaphore sem;
        private volatile bool threadIsWorking;
        /// <summary>
        /// Наименование интерфейса
        /// </summary>
        public string Name
        {
            get
            {
                return dbThread.Name;
            }
        }
        /// <summary>
        /// Тип ожидаемоего соединения: мягкий/штатный, жесткий/полный
        /// </summary>
        protected enum RECONNECT {
            /// <summary>
            /// Не требуется
            /// </summary>
            NOT_REQ
            /// <summary>
            /// Мягкий(штатный) - только объект 'DbConnection'
            /// </summary>
            , SOFT
            ///<summary>
            /// Жесткий(полный) с обновлением 'DbCommand', 'DbAdapter'
            ///</summary>
            , HARD
            , NEW
        }
        /// <summary>
        /// Признак необходимости выполнить повторное соединение с БД
        /// </summary>
        private RECONNECT _needReconnect;
        /// <summary>
        /// Признак установленного соединения с БД
        /// </summary>
        private bool _connected;

        /// <summary>
        /// Конструктор - основной (с аргументом)
        /// </summary>
        /// <param name="name">Наименование интерфейса (рабочего потока)</param>
        public DbInterface (string name)
        {
            lockListeners = new object ();
            lockConnectionSettings = new object ();

            //listeners = new DbInterfaceListener[maxListeners];
            m_dictListeners = new Dictionary<int, DbInterfaceListener> ();

            _connected = false;
            _needReconnect = RECONNECT.SOFT;

            dbThread = new Thread (new ParameterizedThreadStart (DbInterface_ThreadFunction));
            dbThread.Name = name;
            //Name = name;
            dbThread.IsBackground = true;

            _eventDbInterface_ThreadFunctionRun = new ManualResetEvent(false);
        }

        /// <summary>
        /// Признак наличия соединения с источником данных
        /// </summary>
        public bool Connected
        {
            get
            {
                return _connected;
            }
        }

        public bool IsNeedReconnectHard { get { return _needReconnect == RECONNECT.HARD; } }

        public bool IsNeedReconnectNew { get { return _needReconnect == RECONNECT.NEW; } }

        public abstract bool EqualeConnectionSettings(object cs);

        public abstract bool IsEmptyConnectionSettings { get; }

        protected void setConnectionSettings(object cs)
        {
            _needReconnect = (_needReconnect == RECONNECT.NOT_REQ)
                ? RECONNECT.SOFT
                    : EqualeConnectionSettings(cs) == true
                        ? RECONNECT.SOFT
                            : IsEmptyConnectionSettings == true
                                ? RECONNECT.SOFT
                                    : RECONNECT.NEW;
        }

        /// <summary>
        /// Зарегистрировать нового подписчика, вернуть его идентификатор
        /// </summary>
        /// <returns>Идентификатор нового подписчика</returns>
        public int ListenerRegister ()
        {
            int i = -1;

            lock (lockListeners) {
                for (i = 0; i < m_dictListeners.Count; i++) {
                    if (m_dictListeners.ContainsKey (i) == false)
                        break;
                    else
                        ;
                }
                m_dictListeners.Add (i, new DbInterfaceListener ());

                return i;
            }

            //return -1;
        }

        /// <summary>
        /// Отменить регистрацию подписчика по идентификатору
        /// </summary>
        /// <param name="listenerId">Идентификатор подписчика</param>
        public void ListenerUnregister (int listenerId)
        {
            if (m_dictListeners.ContainsKey (listenerId) == false)
                return;

            if (m_dictListeners[listenerId].State == STATE_LISTENER.BUSY)
            // команда на аварийное завершение
                GetDataCancel();

            lock (lockListeners) {
                m_dictListeners.Remove (listenerId);
            }
        }

        /// <summary>
        /// Запустить поток выполнения запросов
        /// </summary>
        public void Start ()
        {
            threadIsWorking = true;
            //sem.WaitOne();
            dbThread.Start ();
        }

        /// <summary>
        /// Остановить выполнение всех запросов
        /// </summary>
        public void Stop ()
        {
            bool joined;
            threadIsWorking = false;
            lock (lockListeners) {
                foreach (KeyValuePair<int, DbInterfaceListener> pair in m_dictListeners)
                    pair.Value.Reset();
            }

            if (dbThread.IsAlive == true) {
                    if (dbThread.Join (Constants.WAIT_TIME_MS) == false)
                        setEventDbInterface_ThreadFunctionRun (@"DbInterface::Stop () - ...");
                    else
                        ;

                joined = dbThread.Join (Constants.MAX_WATING);
                if (!joined)
                    dbThread.Interrupt ();
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
        public void Request (int listenerId, string request)
        {
            //Logging.Logg().Debug(@"DbInterface::Request (int, string) - listenerId=" + listenerId.ToString() + @", request=" + request);

            lock (lockListeners) {
                if ((m_dictListeners.ContainsKey (listenerId) == false) || (request.Length == 0))
                    return;
                else
                    ;

                m_dictListeners [listenerId].SetRequest(request);

                setEventDbInterface_ThreadFunctionRun (@"DbInterface::Request (int, string) - ...");
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
        public int Response (int listenerId, out bool error, out DataTable table)
        {
            int iRes = -1;

            lock (lockListeners) {
                if ((m_dictListeners.ContainsKey (listenerId) == false)
                    || listenerId < 0) {
                    error = true;
                    table = null;
                } else {
                    error = m_dictListeners [listenerId].Error;
                    table = m_dictListeners [listenerId].dataTable;
                }

                iRes = error == false ? 0 : -1;
            }

            //Logging.Logg().Debug(@"DbInterface::Response (int, out bool , out DataTable) - listenerId = " + listenerId + @", error = " + error.ToString() + @", m_dictListeners[listenerId].dataPresent = " + m_dictListeners[listenerId].dataPresent);

            return iRes;
        }

        /// <summary>
        /// Применить параметры соединения с источником данных - активировать объект доступа к нему
        /// </summary>
        protected void setConnectionSettings ()
        {
            setEventDbInterface_ThreadFunctionRun ("DbInterface::SetConnectionSettings () - m_eventDbInterface_ThreadFunctionRun.Set()...");
        }

        /// <summary>
        /// Установить параметры соединения с источником данных
        /// </summary>
        /// <param name="cs">Объект с параметрами соединения</param>
        /// <param name="bStarted">Признак немедленной активации объекта доступа к источнику данных</param>
        public abstract void SetConnectionSettings (object cs, bool bStarted);

        protected abstract int Timeout { get; set; }

        /// <summary>
        /// Объект синхронизации для распознования команды на аварийное завершение подпотока получения данных запроса
        /// </summary>
        private
            ManualResetEvent _eventDbInterface_ThreadFunctionRun
            //Semaphore _semaDbInterface_ThreadFunctionRun
            ;

        private void setEventDbInterface_ThreadFunctionRun (string message)
        {
            try {
                // проверить текущее состояние
                if ((Equals (_eventDbInterface_ThreadFunctionRun, null) == false)
                    && (_eventDbInterface_ThreadFunctionRun?.WaitOne (0) == false))
                    // разрешить обработку пустых запросов (для завершения потоковой ф-и)
                    _eventDbInterface_ThreadFunctionRun?.Set ();
                else
                    ;
            } catch (Exception e) {
            }
        }

        private void DbInterface_ThreadFunction (object data)
        {
            object request;
            bool result;
            bool reconnection/* = false*/;
            Thread threadGetData;
            // Массив объектов синхронизации текущего потока и подпотока ожидания
            // 0 - внешний инициатор, 1 - внутренний (при большом количестве отмененных запросов)
            WaitHandle [] waitHandleGetData = new WaitHandle [] {
                _eventDbInterface_ThreadFunctionRun
                , new AutoResetEvent (false)
            };
            int iReason = -1
                , counterFillError = -1
                , counterDataError = -1; ;

            while (threadIsWorking) {
                switch (iReason = WaitHandle.WaitAny(waitHandleGetData)) {
                    case 0:
                        (waitHandleGetData [iReason] as ManualResetEvent).Reset ();
                        break;
                    default:
                        break;
                }

                lock (lockConnectionSettings) // атомарно читаю и сбрасываю флаг, чтобы при параллельной смене настроек не сбросить повторно выставленный флаг
                {
                    reconnection = !(_needReconnect == RECONNECT.NOT_REQ);
                }

                if (reconnection == true) {
                    Disconnect ();
                    _connected = false;
                    if (threadIsWorking == true) {
                        _connected = Connect ();
                        _needReconnect = _connected == true
                            ? RECONNECT.NOT_REQ
                                : RECONNECT.SOFT;
                    } else
                        _needReconnect = RECONNECT.SOFT; // выставлять флаг можно без блокировки
                } else
                    ;

                if (_connected == false) // не удалось подключиться - не пытаемся получить данные
                    continue;
                else
                    ;

                Timeout = Constants.MAX_WATING / 1000;

                //Logging.Logg().Debug("DbInterface::DbInterface_ThreadFunction () - m_listListeners.Count = " + m_listListeners.Count);

                lock (lockListeners) {
                    // в новом цикле - новое состояние для прерывания
                    counterFillError = 0;
                    counterDataError = 0;

                    //??? внутри цикла при аварийном прерывании из словаря удаляется элемент
                    foreach (KeyValuePair<int, DbInterfaceListener> pair in m_dictListeners) {
                        if (pair.Value.State == STATE_LISTENER.READY) {
                            continue;
                        } else
                            ;

                        request = pair.Value.requestDB;

                        if ((request == null)
                            || (!(request.ToString ().Length > 0))) {
                            continue;
                        } else
                            ;

                        result = false;
                        pair.Value.State = STATE_LISTENER.BUSY;

                        //Logging.Logg().Debug("DbInterface::DbInterface_ThreadFunction () - GetData(...) - request = " + request);

                        try {
                            threadGetData = new Thread(new ParameterizedThreadStart((obj) => {
                                try {
                                    result = GetData(pair.Value.dataTable, request);
                                } catch (ApplicationException e) {
                                    // штатное завершение по превышению установленного лимита времени или другой ошибке (getData_OnFillError)
                                    counterFillError++;
                                } finally {
                                }
                            })) {
                                IsBackground = true
                                , Priority = ThreadPriority.AboveNormal
                            };
                            threadGetData.Start();

                            if (threadGetData.Join(Constants.MAX_WATING) == false) {
                                counterFillError++;
                                GetDataCancel();

                                if (threadGetData.Join(Constants.WAIT_TIME_MS) == false) {
                                    threadGetData.Abort(string.Format(@"Аварийное завершение подпотока получения данных..."));
                                } else
                                    ;
                            } else
                            //// сброс счетчика при успехе
                            //    counterFillError = 0
                                    ;
                        } catch (ThreadAbortException ae) {
                            counterFillError = m_dictListeners.Count + 1;

                            Logging.Logg().Exception(ae, string.Format(@"::DbInterface_ThreadFunction () - {0}:{1}"
                                    , Name, pair.Key)
                                , Logging.INDEX_MESSAGE.NOT_SET);

                            Thread.ResetAbort();
                        } catch (Exception e) {
                            counterFillError++;

                            Logging.Logg ().Exception (e, string.Format (@"DbInterface_ThreadFunction () - {0}:{1}"
                                    , Name, pair.Key)
                                , Logging.INDEX_MESSAGE.NOT_SET);
                        } finally {
                            // result(false) - признак возникновения исключения
                            pair.Value.SetResult(result);
                            counterFillError += result == false ? (m_dictListeners.Count + 1) : 0;
                            counterDataError += pair.Value.Error == true ? 1 : 0;

                            threadGetData = null;
                        }

                        if (counterFillError > m_dictListeners.Count) {
                            Logging.Logg().Error($"DbInterface_ThreadFunction () - {Name}:{pair.Key} - аврийное завершение цикла обработки запросов подписчиков..."
                                , Logging.INDEX_MESSAGE.NOT_SET);

                            break;
                        } else
                            ;

                        //Logging.Logg().Debug("DbInterface::DbInterface_ThreadFunction () - result = GetData(...) - pair.Value.dataPresent = " + pair.Value.dataPresent + @", pair.Value.dataError = " + pair.Value.dataError.ToString ());
                    } // foreach

                    _needReconnect = counterFillError > 1
                        ? counterFillError > m_dictListeners.Count
                            ? RECONNECT.HARD
                                : RECONNECT.SOFT
                                    : RECONNECT.NOT_REQ;

                    if ((!(_needReconnect == RECONNECT.NOT_REQ))
                        || (counterDataError > 0)) {
                    //??? установить в сигнальное состояние для дальнейшего использования
                        (waitHandleGetData[1] as AutoResetEvent).Set();
                    } else
                        ;
                } // lock
            } // while

            try {
                foreach (WaitHandle eventAuto in waitHandleGetData)
                    eventAuto.Close();
            } catch (Exception e) {
                Logging.Logg ().Exception (e, "DbInterface::DbInterface_ThreadFunction () - выход...", Logging.INDEX_MESSAGE.NOT_SET);
            } finally {
            }

            Disconnect ();
        }

        /// <summary>
        /// Установить соединение с БД
        /// </summary>
        /// <returns>Результат попытки установить соединенние с БД</returns>
        protected abstract bool Connect ();
        /// <summary>
        /// Разорвать установленное ранее соединение с БД
        /// </summary>
        /// <returns>Результат попытки разорвать соединенние с БД</returns>
        protected abstract bool Disconnect ();
        /// <summary>
        /// Разорвать установленное ранее соединение с БД
        /// </summary>
        /// <param name="err">Результат попытки разорвать соединенние с БД</param>
        public abstract void Disconnect (out int err);

        protected abstract void GetDataCancel();

        protected void getData_OnFillError(object sender, FillErrorEventArgs e)
        {
            e.Continue = false;

            throw new ApplicationException($"::getData_OnFillError() - ...", e.Errors);
        }
        /// <summary>
        /// Заполнить таблицу значениями - результатом запроса
        /// </summary>
        /// <param name="table">Таблица для значений</param>
        /// <param name="query">Запрос для получения значений</param>
        /// <returns>Результат попытки получения значений</returns>
        protected abstract bool GetData (DataTable table, object query);
    }
}
