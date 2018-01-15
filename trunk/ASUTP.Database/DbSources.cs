using ASUTP.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace ASUTP.Database {
    public delegate int DelegateRegisterDbSource (object connSett, bool active, string desc, bool bReq = false);
    /// <summary>
    /// Интерфейс для класса с описанием управления установленными соединениями с источниками данных
    /// </summary>
    interface IDbSources {
        /// <summary>
        /// Возвратить объект соединения с БД
        /// </summary>
        /// <param name="id">Идентификатор соединения (ключ для словаря - задается при установлении соединения в парметрах для соединения)</param>
        /// <param name="err">Признак ошибки при получении объекта соединения</param>
        /// <returns>Объект соединения с БД</returns>
        System.Data.Common.DbConnection GetConnection (int id, out int err);
        /// <summary>
        /// Зарегистрировать и установить соединение с БД
        /// </summary>
        /// <param name="connSett">Объект с параметрами для соединения с БД</param>
        /// <param name="active">Признак активности (ожидание запросов в отдельном потоке)</param>
        /// <param name="desc">Описание соединения с БД</param>
        /// <param name="bReq">Признак принудительного создания отдельного экземпляра соединения
        ///  (при наличии уже установленного, для использования в будущем)</param>
        /// <returns>Идентификатор соединения</returns>
        int Register (object connSett, bool active, string desc, bool bReq = false);
        /// <summary>
        /// Отправить запрос для обработки
        /// </summary>
        /// <param name="id">Идентификатор соединения</param>
        /// <param name="query">Запрос для обработки</param>
        void Request (int id, string query);
        /// <summary>
        /// Получить ответ на запрос к БД
        /// </summary>
        /// <param name="id">Идентификатор соединения</param>
        /// <param name="err">Признак ошибки при получении результата запроса</param>
        /// <param name="tableRes">Таблица-результат запроса</param>
        /// <returns>Признак результата выполнения</returns>
        int Response (int id, out bool err, out System.Data.DataTable tableRes);
        /// <summary>
        /// Установить новые параметры для соединения с БД
        ///  , старое при необходимости разрывается
        /// </summary>
        /// <param name="id">Идентификатор соединения</param>
        /// <param name="connSett">Параметры для нового соединения</param>
        /// <param name="active">Признак акивности нового соединения</param>
        void SetConnectionSettings (int id, ConnectionSettings connSett, bool active);
        /// <summary>
        /// Разорвать (отменить регистрацию) все установленные соединения
        /// </summary>
        void UnRegister ();
        /// <summary>
        /// Разорвать (отменить регистрацию) указанного соединения
        /// </summary>
        /// <param name="id">Идентификатор соединения</param>
        void UnRegister (int id);
    }
    /// <summary>
    /// Класс для описания управлением установленными соединениями с источниками данных
    /// </summary>
    public class DbSources : IDbSources {
        private event DelegateRegisterDbSource evtRegister;
        //public event DelegateIntFunc UnRegister;

        //private enum INDEX_SYNCHRONIZE { UNKNOWN = -1, EXIT, REGISTER, UNREGISTER }

        //private Queue <object []> m_queueToRegistered;
        //private Queue <int> m_queueResult;
        //private Queue<int> m_queueToUnRegistered;

        //private AutoResetEvent [] m_arEvtComleted;
        //private AutoResetEvent [] m_arSyncProc;

        //private BackgroundWorker m_threadConnSett; 
        /// <summary>
        /// Ссылка на "самого себя" - для исключения создания 2-х объектов класса
        /// </summary>
        protected static DbSources m_this;
        /// <summary>
        /// Словарь с объектами-потоками обработки запросов
        /// </summary>
        protected Dictionary<int, DbInterface> m_dictDbInterfaces;
        /// <summary>
        /// Класс для описания подписчика на установленное соединение
        /// </summary>
        protected class DbSourceListener {
            /// <summary>
            /// Объект соединения с БД
            /// </summary>
            public volatile DbConnection dbConn;
            /// <summary>
            /// Идентификатор объекта-потока для обработки запросов
            /// </summary>
            public volatile int idDbInterface;
            /// <summary>
            /// Идентификатор подписчика
            /// </summary>
            public volatile int iListenerId;
            /// <summary>
            /// Конструктор - основной (без параметров)
            /// </summary>
            /// <param name="id">Идентификатор объекта-потока для обработки запросов</param>
            /// <param name="indx">Идентификатор подписчика</param>
            /// <param name="conn">Объект соединения с БД</param>
            public DbSourceListener (int id, int indx, DbConnection conn)
            {
                idDbInterface = id;
                iListenerId = indx;
                dbConn = conn;
            }
        }
        /// <summary>
        /// Словарь объектов-подписчиков на установленные соединения
        /// </summary>
        protected Dictionary<int, DbSourceListener> m_dictListeners;
        /// <summary>
        /// Объект для блокирования доступа к словарю 'm_dictListeners'
        /// </summary>
        private object m_objDictListeners;
        /// <summary>
        /// Конструктор - основной (защищенный)
        /// </summary>
        protected DbSources ()
        {
            m_dictDbInterfaces = new Dictionary<int, DbInterface> ();
            m_dictListeners = new Dictionary<int, DbSourceListener> ();
            m_objDictListeners = new object ();

            //m_evtRegisterComleted = new AutoResetEvent (false);
            evtRegister += new DelegateRegisterDbSource (register);
            //UnRegister += new DelegateIntFunc(unRegister);

            //m_queueToRegistered = new Queue<object []> ();
            //m_queueResult = new Queue <int> ();
            //m_queueToUnRegistered = new Queue<int>();
            //m_arSyncProc = new AutoResetEvent [] {
            //    new AutoResetEvent (false)
            //    , new AutoResetEvent (false)
            //    , new AutoResetEvent (false)
            //};
            //m_arEvtComleted = new AutoResetEvent[] {
            //    new AutoResetEvent (false)
            //    , new AutoResetEvent (false)
            //};
            //m_threadConnSett = new BackgroundWorker ();
            //m_threadConnSett.DoWork += new DoWorkEventHandler(fThreadProcConnSett);
            //m_threadConnSett.RunWorkerAsync ();
        }

        ~DbSources ()
        {
            UnRegister ();

            //m_arSyncProc[(int)INDEX_SYNCHRONIZE.EXIT].Set ();
        }

        //private void fThreadProcConnSett (object obj, DoWorkEventArgs ev)
        //{
        //    INDEX_SYNCHRONIZE indx = INDEX_SYNCHRONIZE.UNKNOWN;

        //    while (! (indx == INDEX_SYNCHRONIZE.EXIT))
        //    {
        //        indx = (INDEX_SYNCHRONIZE) WaitHandle.WaitAny (m_arSyncProc);

        //        switch (indx)
        //        {
        //            case INDEX_SYNCHRONIZE.EXIT:
        //                break;
        //            case INDEX_SYNCHRONIZE.REGISTER:
        //                object []pars = m_queueToRegistered.Dequeue ();
        //                m_queueResult.Enqueue (register (pars [0], (bool)pars [1], (string)pars [2], (bool)pars [3])); 
        //                break;
        //            case INDEX_SYNCHRONIZE.UNREGISTER:
        //                unRegister (m_queueToUnRegistered.Dequeue ());
        //                break;
        //            default:
        //                break;
        //        }

        //        m_arEvtComleted[(int)indx - 1].Set ();
        //    }
        //}
        /// <summary>
        /// Функция для доступа к объекту
        /// </summary>
        /// <returns>Объект для управления установленными соединениями с источниками данных</returns>
        public static DbSources Sources ()
        {
            if (m_this == null)
                m_this = new DbSources ();
            else
                ;

            return m_this;
        }
        ///// <summary>
        ///// Регистриует клиента соединения, активным или нет, при необходимости принудительно отдельный экземпляр
        ///// </summary>
        ///// <param name="connSett">параметры соединения</param>
        ///// <param name="active">признак активности</param>
        ///// <param name="bReq">признак принудительного создания отдельного экземпляра</param>
        ///// <returns>Идентификатор для получения значений при обращении к БД</returns>
        //public virtual int Register(object connSett, bool active, string desc, bool bReq = false)
        //{
        //    m_queueToRegistered.Enqueue(new object[] { connSett, active, desc, bReq });

        //    m_arSyncProc [(int)INDEX_SYNCHRONIZE.REGISTER].Set ();

        //    m_arEvtComleted[(int)INDEX_SYNCHRONIZE.REGISTER - 1].WaitOne();

        //    return m_queueResult.Dequeue ();
        //}

        //private AutoResetEvent m_evtRegisterComleted;

        public virtual int Register (object connSett, bool active, string desc, bool bReq = false)
        {
            //m_evtRegisterComleted.WaitOne ();
            return evtRegister (connSett, active, desc, bReq);
        }

        private int register (object connSett, bool active, string desc, bool bReq = false)
        {
            int id = -1,
                err = 0;
            //Блокировать доступ к словарю
            lock (this)
            //lock (m_objDictListeners)
            {
                //Проверить тип объекта с параметрами соединения
                if (connSett is ConnectionSettings == true)
                    //Проверить наличие уже установленного соединения
                    // , и созданного объекта-потока для обработки запросов
                    if ((m_dictDbInterfaces.ContainsKey (((ConnectionSettings)connSett).id) == true) && (bReq == false)) {
                        try {
                            id = m_dictDbInterfaces [((ConnectionSettings)connSett).id].ListenerRegister ();
                        } catch (Exception e) { err = -1; }
                    } else
                        ;
                else
                    //Проверить тип объекта с параметрами соединения 
                    if (connSett is string == true) {
                } else
                    ;
                //Проверить результат предыдущей операции
                if (err == 0)
                    if ((id < 0) && (m_dictDbInterfaces.ContainsKey (((ConnectionSettings)connSett).id) == false)) {
                        string dbNameType = string.Empty;
                        DbTSQLInterface.DB_TSQL_INTERFACE_TYPE dbType = DbTSQLInterface.DB_TSQL_INTERFACE_TYPE.UNKNOWN;
                        switch (((ConnectionSettings)connSett).port) {
                            case -666:
                                dbType = DbTSQLInterface.DB_TSQL_INTERFACE_TYPE.ModesCentre;
                                break;
                            case 1433:
                                dbType = DbTSQLInterface.DB_TSQL_INTERFACE_TYPE.MSSQL;
                                break;
                            case 3306:
                                dbType = DbTSQLInterface.DB_TSQL_INTERFACE_TYPE.MySQL;
                                break;
                            case 1521:
                                dbType = DbTSQLInterface.DB_TSQL_INTERFACE_TYPE.Oracle;
                                break;
                            default:
                                break;
                        }

                        dbNameType = dbType.ToString ();
                        //
                        switch (dbType) {
                            case DbInterface.DB_TSQL_INTERFACE_TYPE.ModesCentre:
                                //m_dictDbInterfaces.Add(((ConnectionSettings)connSett).id, new DbMCInterface (dbType, @"Интерфейс: " + dbNameType));
                                break;
                            case DbInterface.DB_TSQL_INTERFACE_TYPE.MSSQL:
                            case DbInterface.DB_TSQL_INTERFACE_TYPE.MySQL:
                            case DbInterface.DB_TSQL_INTERFACE_TYPE.Oracle:
                                m_dictDbInterfaces.Add (((ConnectionSettings)connSett).id, new DbTSQLInterface (dbType, @"Интерфейс: " + dbNameType + @"-БД" + @"; " + desc));
                                break;
                            default:
                                break;
                        }
                        try {
                            if (active == true)
                                m_dictDbInterfaces [((ConnectionSettings)connSett).id].Start ();
                            else
                                ;

                            m_dictDbInterfaces [((ConnectionSettings)connSett).id].SetConnectionSettings (connSett, active);

                            id = m_dictDbInterfaces [((ConnectionSettings)connSett).id].ListenerRegister ();
                        } catch (Exception e) {
                            Logging.Logg ().Exception (e
                                , @"DbSources::register () - ListenerRegister () - ConnectionSettings.ID=" + (connSett as ConnectionSettings).id
                                , Logging.INDEX_MESSAGE.NOT_SET);
                            err = -1;
                        }
                    } else
                        ; // m_dictDbInterfaces[((ConnectionSettings)connSett).id].Name = desc;
                else
                    ;

                if (err == 0)
                    return registerListener (ListenerIdLocal, ((ConnectionSettings)connSett).id, id, active, out err);
                else
                    return err;
            }
        }
        /// <summary>
        /// Установить новые параметры для соединения с БД
        ///  , старое при необходимости разрывается
        /// </summary>
        /// <param name="id">Идентификатор соединения</param>
        /// <param name="connSett">Параметры для нового соединения</param>
        /// <param name="active">Признак акивности нового соединения</param>
        public void SetConnectionSettings (int id, ConnectionSettings connSett, bool active)
        {
            if ((m_dictListeners.ContainsKey (id) == true) && (m_dictDbInterfaces.ContainsKey (connSett.id) == true) &&
                (m_dictListeners [id].idDbInterface == connSett.id)) {
                m_dictDbInterfaces [m_dictListeners [id].idDbInterface].SetConnectionSettings (connSett, active);
            } else
                ;
        }

        protected int ListenerIdLocal
        {
            get
            {
                return HMath.GetRandomNumber () /*Int32.Parse(DateTime.UtcNow.ToString (@"mmssfffff"))*/;
            }
        }
        ///// <summary>
        ///// Регистрировать подписчика на установленное соединение - получить идентификатор для передачи во-вне
        ///// </summary>
        ///// <param name="id">Идентификатор чего ???</param>
        ///// <param name="idListener">Идентификатор чего ???</param>
        ///// <param name="active">Признак активности</param>
        ///// <param name="err">Признак ошибки при выполнении регистрации</param>
        ///// <returns>Результат выполнения</returns>
        //protected int registerListener(int id, int idListener, bool active, out int err)
        //{
        //    int iRes = -1;

        //    //lock (m_objDictListeners) {
        //        //Поиск нового идентифакатора для подписчика
        //        for (iRes = 0; iRes < m_dictListeners.Keys.Count; iRes ++)
        //        {
        //            if (m_dictListeners.ContainsKey(iRes) == false)
        //            {
        //                //registerListener(iRes, ((ConnectionSettings)connSett).id, id, active, out err);
        //                break;
        //            }
        //            else
        //                ;
        //        }

        //        //Зарегистрировать новый идентификатор
        //        //if (! (iRes < m_dictListeners.Keys.Count))
        //            registerListener(iRes, id, idListener, active, out err);
        //        //else
        //        //    ;
        //    //}

        //    if (! (err == 0))
        //        iRes = -1;
        //    else
        //        ;

        //    return iRes;
        //}
        /// <summary>
        ///  Регистрировать подписчика на установленное соединение - получить идентификатор для передачи в 'registerListener'
        /// </summary>
        /// <param name="idReg">Новый внешний идентификатор подписчика для регистрации</param>
        /// <param name="id">Идентификатор объекта-потока обработки запросов</param>
        /// <param name="idListener">Идентификатор</param>
        /// <param name="active"></param>
        /// <param name="err"></param>
        protected int registerListener (int idReg, int id, int idListener, bool active, out int err)
        {
            err = 0;
            DbConnection dbConn = null;

            if (active == false) {
                try {
                    dbConn = ((DbTSQLInterface)m_dictDbInterfaces [id]).GetConnection (out err);
                } catch (Exception e) { Logging.Logg ().Exception (e, @"DbSources::register () - GetConnection () - ...", Logging.INDEX_MESSAGE.NOT_SET); err = -1; }
            } else
                ;

            if (err == 0) {
                //Console.WriteLine(@"DbSources::registerListener (id=" + id + @", idReg=" + idReg + @") - ...");
                m_dictListeners.Add (idReg, new DbSourceListener (id, idListener, dbConn));
            } else
                ;

            return idReg;
        }

        /// <summary>
        /// Отменить регитсрацию всех подписчиков
        /// </summary>
        public void UnRegister ()
        {
            List<int> keys = new List<int> ();
            foreach (int id in m_dictListeners.Keys)
                keys.Add (id);

            foreach (int id in keys)
                UnRegister (id);
        }

        /// <summary>
        /// Отменить регистрацию подписчика по идентификатору
        /// </summary>
        /// <param name="id">Идентификатор подписчика - активного соединения с БД</param>
        public void UnRegister (int id)
        {
            int err = -1;

            //lock (m_objDictListeners) {
            if (m_dictListeners.ContainsKey (id) == true) {
                if (m_dictDbInterfaces.ContainsKey (m_dictListeners [id].idDbInterface) == true) {
                    m_dictDbInterfaces [m_dictListeners [id].idDbInterface].ListenerUnregister (m_dictListeners [id].iListenerId);
                    if (!(m_dictDbInterfaces [m_dictListeners [id].idDbInterface].ListenerCount > 0)) {
                        m_dictDbInterfaces [m_dictListeners [id].idDbInterface].Stop ();

                        m_dictDbInterfaces [m_dictListeners [id].idDbInterface].Disconnect (out err);
                        if (Equals (m_dictListeners [id].dbConn, null) == false)
                            if (!(m_dictListeners [id].dbConn.State == ConnectionState.Closed))
                                Logging.Logg ().Warning ($"DbSources::UnRegister (идентификатор={id}) - состояние объекта соединения \"Открыто\"..."
                                    , Logging.INDEX_MESSAGE.NOT_SET);
                            else
                                m_dictListeners [id].dbConn = null;
                        else
                            ;
                        m_dictDbInterfaces.Remove (m_dictListeners [id].idDbInterface);
                    } else
                        ;
                } else
                    ;

                //Console.WriteLine(@"DbSources::UnregisterListener (idReg=" + id + @") - ...");
                m_dictListeners.Remove (id);
            } else
                ;
            //}
        }

        /// <summary>
        /// Отправляет запрос к источнику БД с идентификатором
        /// </summary>
        /// <param name="id">идентификатор</param>
        /// <param name="query">запрос</param>
        public void Request (int id, string query)
        {
            if (m_dictListeners.ContainsKey (id) == true) {
                m_dictDbInterfaces [m_dictListeners [id].idDbInterface].Request (m_dictListeners [id].iListenerId, query);
            } else
                ;
        }

        /// <summary>
        /// Получает рез-т запроса  - таблицу, от источника с идентификатором, с признаком ошибки
        /// </summary>
        /// <param name="id">идентификатор</param>
        /// <param name="err">признак4 ошибки</param>
        /// <param name="tableRes">результирующая таблица</param>
        public int Response (int id, out bool err, out DataTable tableRes)
        {
            int iRes = -1;

            tableRes = null;
            err = true;

            //lock (m_objDictListeners) {
            if (m_dictListeners.ContainsKey (id) == true)
                if (m_dictDbInterfaces.ContainsKey (m_dictListeners [id].idDbInterface) == true)
                    iRes = m_dictDbInterfaces [m_dictListeners [id].idDbInterface].Response (m_dictListeners [id].iListenerId, out err, out tableRes);
                else
                    ;
            else
                ;
            //}

            return iRes;
        }

        /// <summary>
        /// Возвратить объект установленного соединения для указанного идентификатора
        /// </summary>
        /// <param name="id">Идентификатор подписчика</param>
        /// <param name="err">Признак ошибки</param>
        /// <returns>Объект установленного соединения</returns>
        public DbConnection GetConnection (int id, out int err)
        {
            DbConnection res = null;
            err = -1;

            //lock (m_objDictListeners) {
            if ((m_dictListeners.ContainsKey (id) == true)
                && (!(m_dictListeners [id].dbConn == null))) {
                res = m_dictListeners [id].dbConn;

                err = (res.State == ConnectionState.Broken)
                    || (res.State == ConnectionState.Closed) ? -2 : 0;
            } else
                ;
            //}

            return res;
        }
    }
}
