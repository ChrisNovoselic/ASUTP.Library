using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Threading;
using System.Data.Common;
using System.Data.OleDb;
using System.Data.SqlClient;
using MySql.Data.MySqlClient;

//namespace HClassLibrary
namespace HClassLibrary
{
    public abstract class DbInterface
    {
        public enum DB_TSQL_INTERFACE_TYPE
        {
            MySQL,
            MSSQL,
            MSExcel,
            ModesCentre,
            UNKNOWN
        }

        public static int MAX_RETRY = 3;
        public static int MAX_WAIT_COUNT = 39;
        public static int WAIT_TIME_MS = 106;

        public static int MAX_WATING {
            get {
                return MAX_RETRY * MAX_WAIT_COUNT * WAIT_TIME_MS;
                //return 6666;
            }
        }

        protected class DbInterfaceListener
        {
            public volatile bool listenerActive; 
            public volatile bool dataPresent;
            public volatile bool dataError;
            public volatile object requestDB;
            public volatile DataTable dataTable;

            public DbInterfaceListener () {
                listenerActive =
                dataPresent =
                dataError =
                false;

                requestDB = null; //new object ();
                dataTable = new DataTable ();
            }
        }
        protected Dictionary <int, DbInterfaceListener> m_dictListeners;
        //private int maxListeners;
        public int ListenerCount { get { return m_dictListeners.Count; } }

        public object m_connectionSettings;

        protected object lockListeners;
        protected object lockConnectionSettings;
        private Thread dbThread;
        private Semaphore sem;
        private volatile bool threadIsWorking;
        private string m_ThreadName;
        public string Desc {
            get { return m_ThreadName; }
            set
            {
                if (m_ThreadName == string.Empty)
                    m_ThreadName = value;
                else
                    m_ThreadName += @"; " + value;
            }
        }

        protected bool needReconnect;
        private bool connected;

        public DbInterface(string name)
        {
            Desc = name;

            lockListeners = new object();
            lockConnectionSettings = new object();
            
            //listeners = new DbInterfaceListener[maxListeners];
            m_dictListeners = new Dictionary <int, DbInterfaceListener> ();

            connected = false;
            needReconnect = false;

            dbThread = new Thread(new ParameterizedThreadStart(DbInterface_ThreadFunction));
            dbThread.Name = m_ThreadName;
            dbThread.IsBackground = true;

            sem = new Semaphore(1, 1);
        }

        public bool Connected
        {
            get { return connected; }
        }

        public int ListenerRegister()
        {
            int i = -1;
            
            lock (lockListeners)
            {
                for (i = 0; i < m_dictListeners.Count; i ++) {
                    if (m_dictListeners.ContainsKey (i) == false)
                        break;
                    else
                        ;
                }
                m_dictListeners.Add (i, new DbInterfaceListener ());
                m_dictListeners [i].listenerActive = true;
                return i;
            }

            //return -1;
        }

        public void ListenerUnregister(int listenerId)
        {
            if (m_dictListeners.ContainsKey(listenerId) == false)
                return;

            lock (lockListeners)
            {
                m_dictListeners.Remove(listenerId);
            }
        }

        public void Start()
        {
            threadIsWorking = true;
            sem.WaitOne();
            dbThread.Start();
        }

        public void Stop()
        {
            bool joined;
            threadIsWorking = false;
            lock (lockListeners)
            {
                foreach (KeyValuePair <int, DbInterfaceListener> pair in m_dictListeners)
                {
                    pair.Value.requestDB = null;
                }
            }
            if (dbThread.IsAlive)
            {
                try
                {
                    sem.Release(1);
                }
                catch
                {
                }

                joined = dbThread.Join(5000);
                if (!joined)
                    dbThread.Abort();
                else
                    ;
            }
            else
                ;
        }

        public void Request(int listenerId, string request)
        {
            //Logging.Logg().Debug(@"DbInterface::Request (int, string) - listenerId=" + listenerId.ToString() + @", request=" + request);
            
            lock (lockListeners)
            {
                if ((m_dictListeners.ContainsKey (listenerId) == false) || (request.Length == 0))
                    return;
                else
                    ;

                m_dictListeners[listenerId].requestDB = request;
                m_dictListeners[listenerId].dataPresent = false;
                m_dictListeners[listenerId].dataError = false;

                try
                {
                    if (!(WaitHandle.WaitAny(new WaitHandle[] { sem }, 0) == 0/*WaitHandle.WaitTimeout*/)) sem.Release(1); else ;
                }
                catch (Exception e)
                {
                    Logging.Logg ().Exception (e, @"DbInterface::Request (int, string)");
                }
            }

            //Logging.Logg().Debug(@"DbInterface::Request (int, string) - " + listenerId + @", " + request);
        }

        public bool Response(int listenerId, out bool error, out DataTable table)
        {
            lock (lockListeners)
            {
                if ((m_dictListeners.ContainsKey(listenerId) == false) || listenerId < 0)
                {
                    error = true;
                    table = null;

                    return false;
                }
                else
                    ;

                error = m_dictListeners[listenerId].dataError;
                table = m_dictListeners[listenerId].dataTable;
            }

            //Logging.Logg().Debug(@"DbInterface::Response (int, out bool , out DataTable) - listenerId = " + listenerId + @", error = " + error.ToString() + @", m_dictListeners[listenerId].dataPresent = " + m_dictListeners[listenerId].dataPresent);

            return m_dictListeners[listenerId].dataPresent;
        }

        protected void SetConnectionSettings()
        {
            try
            {
                sem.Release(1);
            }
            catch (Exception e)
            {
                Logging.Logg().Exception(e, "DbInterface::SetConnectionSettings () - обращение к переменной sem (вызов: sem.Release ())");
            }
        }

        public abstract void SetConnectionSettings(object cs, bool bStarted);

        private void DbInterface_ThreadFunction(object data)
        {
            object request;
            bool result;
            bool reconnection/* = false*/;

            while (threadIsWorking)
            {
                sem.WaitOne();

                lock (lockConnectionSettings) // атомарно читаю и сбрасываю флаг, чтобы при параллельной смене настроек не сбросить повторно выставленный флаг
                {
                    reconnection = needReconnect;
                    needReconnect = false;
                }

                if (reconnection == true)
                {
                    Disconnect();
                    connected = false;
                    if ((threadIsWorking == true) && (Connect() == true))
                        connected = true;
                    else
                        needReconnect = true; // выставлять флаг можно без блокировки
                }
                else
                    ;

                if (connected == false) // не удалось подключиться - не пытаемся получить данные
                    continue;
                else
                    ;

                //Logging.Logg().Debug("DbInterface::DbInterface_ThreadFunction () - m_listListeners.Count = " + m_listListeners.Count);

                lock (lockListeners)
                {
                    foreach (KeyValuePair <int, DbInterfaceListener> pair in m_dictListeners)
                    {
                        //lock (lockListeners)
                        //{
                            if (pair.Value.listenerActive == false) {
                                continue;
                            }
                            else
                                ;

                            request = pair.Value.requestDB;

                            if ((request == null) || (!(request.ToString ().Length > 0))) {
                                continue;
                            }
                            else
                                ;
                        //}

                        //Logging.Logg().Debug("DbInterface::DbInterface_ThreadFunction () - GetData(...) - request = " + request);

                        try
                        {
                            result = GetData(pair.Value.dataTable, request);
                        }
                        catch (DbException e)
                        {
                            Logging.Logg().Exception(e, "DbInterface::DbInterface_ThreadFunction () - result = GetData(...) - request = " + request);
                        
                            result = false;
                        }

                        //Logging.Logg().Debug("DbInterface::DbInterface_ThreadFunction () - result = GetData(...) - result = " + result);

                        //lock (lockListeners)
                        //{
                            if (pair.Value.listenerActive == false)
                                continue;
                            else
                                ;

                            //Logging.Logg().Debug("DbInterface::DbInterface_ThreadFunction () - result = GetData(...) - pair.Value.listenerActive = " + pair.Value.listenerActive);

                            if (request == pair.Value.requestDB)
                            {
                                pair.Value.dataPresent = result;
                                pair.Value.dataError = ! result;

                                //if (result == true)
                                //{
                                //    pair.Value.dataPresent = true;
                                //}
                                //else
                                //{
                                //    pair.Value.dataError = true;
                                //}

                                pair.Value.requestDB = null;
                            }
                            else
                                ;

                            //Logging.Logg().Debug("DbInterface::DbInterface_ThreadFunction () - result = GetData(...) - pair.Value.dataPresent = " + pair.Value.dataPresent + @", pair.Value.dataError = " + pair.Value.dataError.ToString ());
                        //}
                    }
                }
            }
            try
            {
                if (!(WaitHandle.WaitAny(new WaitHandle[] { sem }, 0) == 0)) sem.Release(1); else ;
            }
            catch (Exception e)
            {
                Logging.Logg().Exception(e, "DbInterface::DbInterface_ThreadFunction () - выход...");
            }

            Disconnect();
        }

        protected abstract bool Connect();

        protected abstract bool Disconnect();

        protected abstract bool GetData(DataTable table, object query);
    }
}
