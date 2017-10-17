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
    /// ����� ��� �������� ������� ��� ��������� � �� ���������� �����������
    /// </summary>
    public abstract class DbInterface
    {
        /// <summary>
        /// ������������ - ���� �������������� �����������
        /// </summary>
        public enum DB_TSQL_INTERFACE_TYPE
        {
            /// <summary>My Sql</summary>
            MySQL
            /// <summary>MS SQL</summary>
            , MSSQL
            /// <summary>MS Excel</summary>
            , MSExcel
            /// <summary>����� - �����</summary>
            , ModesCentre
            /// <summary>Oracle</summary>
            , Oracle
            /// <summary>����������� ���</summary>
            , UNKNOWN
        }
        /// <summary>
        /// ������������ ���������� ��������
        /// </summary>
        public static volatile int MAX_RETRY = 3;
        /// <summary>
        /// ���������� ������� �������� ������� ���������� � ����� �����
        /// </summary>
        public static volatile int MAX_WAIT_COUNT = 39;
        /// <summary>
        /// �������� �������� ����� ���������� ������� ����������
        ///  , ��� ������� ��� � ���������� �������� ��������� �� ��� �������
        /// </summary>
        public static volatile int WAIT_TIME_MS = 106;
        /// <summary>
        /// ������������ ����� �������� ��������� ��������� ������������� ��������
        /// </summary>
        public static int MAX_WATING
        {
            get {
                return MAX_RETRY * MAX_WAIT_COUNT * WAIT_TIME_MS;
                //return 6666;
            }
        }
        /// <summary>
        /// ������������ - ��������� ��������� ���������� �� ���������� ��������
        /// </summary>
        public enum STATE_LISTENER
        {
            /// <summary>
            /// ���������������
            /// </summary>
            READY
            /// <summary>
            /// ��������� � 
            /// </summary>
            , REQUEST
            /// <summary>
            /// �����, ����������� ������
            /// </summary>
            , BUSY
        }
        /// <summary>
        /// ����� ��� �������� ���������� �� ���������� �������� � ��
        /// </summary>
        protected class DbInterfaceListener
        {
            /// <summary>
            /// ��������� ����������
            /// </summary>
            public volatile STATE_LISTENER state;
            /// <summary>
            /// ������� ������� ������ � ���������� �������
            /// </summary>
            public volatile bool dataPresent;
            /// <summary>
            /// ������� ������ �� ����������� �������
            /// </summary>
            public volatile bool dataError;
            /// <summary>
            /// ������ ������� �� ��������� ������
            /// </summary>
            public volatile object requestDB;
            /// <summary>
            /// ��������� �������
            /// </summary>
            public volatile DataTable dataTable;
            /// <summary>
            /// ����������� - �������� (��� ����������)
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
        /// ������� � ������������ ��� ���������� ��������
        /// </summary>
        protected Dictionary<int, DbInterfaceListener> m_dictListeners;
        /// <summary>
        /// ���������� ����������� � ����������
        /// </summary>
        public int ListenerCount { get { return m_dictListeners.Count; } }
        /// <summary>
        /// ������ ���������� � ��
        /// </summary>
        public object m_connectionSettings;
        /// <summary>
        /// ������ ��� ������������� ������� � �����������
        /// </summary>
        protected object lockListeners;
        /// <summary>
        /// ������ ��� ������������� ��������� ���������� ����������
        /// </summary>
        protected object lockConnectionSettings;

        private Thread dbThread;
        private Semaphore sem;
        private volatile bool threadIsWorking;
        /// <summary>
        /// ������������ ����������
        /// </summary>
        public string Name
        {
            get { return dbThread.Name; }
        }
        /// <summary>
        /// ������� ������������� ��������� ��������� ���������� � ��
        /// </summary>
        protected bool needReconnect;
        /// <summary>
        /// ������� �������������� ���������� � ��
        /// </summary>
        private bool connected;

        /// <summary>
        /// ����������� - �������� (� ����������)
        /// </summary>
        /// <param name="name">������������ ���������� (�������� ������)</param>
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
        /// ������� ������� ���������� � ���������� ������
        /// </summary>
        public bool Connected
        {
            get { return connected; }
        }

        /// <summary>
        /// ���������������� ������ ����������, ������� ��� �������������
        /// </summary>
        /// <returns>������������� ������ ����������</returns>
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
        /// �������� ����������� ���������� �� ��������������
        /// </summary>
        /// <param name="listenerId">������������� ����������</param>
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
        /// ��������� ����� ���������� ��������
        /// </summary>
        public void Start()
        {
            threadIsWorking = true;
            //sem.WaitOne();
            dbThread.Start();
        }

        /// <summary>
        /// ���������� ���������� ���� ��������
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
        /// ��������� ������ ��������� ������
        /// </summary>
        /// <param name="listenerId">������������� ���������� - ���������� �������</param>
        /// <param name="request">������ - ���������� �������</param>
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
        /// �������� ��������� ������� ��� ���������� � ���������������� ����������
        /// </summary>
        /// <param name="listenerId">������������� ���������� - ���������� �������</param>
        /// <param name="error">������� ������ ��� ��������� ������</param>
        /// <param name="table">������� - ��������� ������� � ��������� ������</param>
        /// <returns>��������� ���������� ������</returns>
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
        /// ��������� ��������� ���������� � ���������� ������ - ������������ ������ ������� � ����
        /// </summary>
        protected void SetConnectionSettings()
        {
            try {
                sem.Release(1);
            } catch (Exception e) {
                Logging.Logg().Exception(e, "DbInterface::SetConnectionSettings () - ��������� � ���������� sem (�����: sem.Release ())", Logging.INDEX_MESSAGE.NOT_SET);
            }
        }

        /// <summary>
        /// ���������� ��������� ���������� � ���������� ������
        /// </summary>
        /// <param name="cs">������ � ����������� ����������</param>
        /// <param name="bStarted">������� ����������� ��������� ������� ������� � ��������� ������</param>
        public abstract void SetConnectionSettings(object cs, bool bStarted);

        /// <summary>
        /// ������ ������������� ��� ������������� ������� �� ��������� ���������� ��������� ��������� ������ �������
        /// </summary>
        private AutoResetEvent m_eventGetDataBreak;

        private void DbInterface_ThreadFunction(object data)
        {
            object request;
            bool result;
            bool reconnection/* = false*/;
            Thread threadGetData;
            // ������ �������� ������������� �������� ������ � ��������� ��������
            // 0 - ���������� ����������, 1 - �������� ����������
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

                lock (lockConnectionSettings) // �������� ����� � ��������� ����, ����� ��� ������������ ����� �������� �� �������� �������� ������������ ����
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
                        needReconnect = true; // ���������� ���� ����� ��� ����������
                } else
                    ;

                if (connected == false) // �� ������� ������������ - �� �������� �������� ������
                    continue;
                else
                    ;

                //Logging.Logg().Debug("DbInterface::DbInterface_ThreadFunction () - m_listListeners.Count = " + m_listListeners.Count);

                lock (lockListeners) {
                    // � ����� ����� - ����� ��������� ��� ����������
                    iGetData = -1;

                    //??? ������ ����� ��� ��������� ���������� �� ������� ��������� �������
                    foreach (KeyValuePair<int, DbInterfaceListener> pair in m_dictListeners) {
                        //??? ���� �������� ��������� ������� ������ �� �����������, �� ��������� ���������� ������������
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
                                            ? string.Format(@"������� ������(������-0)={0}", ae.Data[0])
                                                : "������� ������ �� ������"
                                        , ae.Message, ae.StackTrace));

                                    Thread.ResetAbort();
                                } catch (Exception e) {
                                    Logging.Logg().ExceptionDB(string.Format(@"DbInterface_ThreadFunction () - {1}:{2}{0}{3}{0}{4}"
                                        , Environment.NewLine
                                        , Name, pair.Key
                                        , e.Message, e.StackTrace));
                                } finally {
                                }
                            })) { // ��������� ��� ������� ������
                                IsBackground = true
                                , Name = string.Format (@"{0}:{1}", Name, pair.Key)
                                , Priority = ThreadPriority.AboveNormal
                            };
                            // ������ ������
                            threadGetData.Start(waitHandleGetData [0]);

                            if ((iGetData = WaitHandle.WaitAny(waitHandleGetData, MAX_WATING)) > 0) {
                                switch (iGetData) {
                                    case WaitHandle.WaitTimeout:
                                        // ������� �� ��������� ����������
                                        (waitHandleGetData [1] as AutoResetEvent).Set ();
                                        needReconnect = true;
                                        break;
                                    default:                                        
                                        break;
                                }

                                // ���� ���� ����. ���������� ����� ���������� ������� �� ��������� ���������� �����. ������
                                if (waitHandleGetData [0].WaitOne (WAIT_TIME_MS) == false)
                                    // ���� ��� ���� ����. ����������
                                    if (threadGetData.Join (WAIT_TIME_MS) == false) {
                                        // ����������� ���������
                                        threadGetData.Abort (string.Format (@"��������� ���������� ��������� ��������� ������..."));
                                        //threadGetData.Interrupt ();
                                        //// ������� � ���������� ����������
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
                Logging.Logg().Exception(e, "DbInterface::DbInterface_ThreadFunction () - �����...", Logging.INDEX_MESSAGE.NOT_SET);
            } finally {
                foreach (WaitHandle eventAuto in waitHandleGetData)
                    eventAuto.Close();
            }

            Disconnect();
        }

        /// <summary>
        /// ���������� ���������� � ��
        /// </summary>
        /// <returns>��������� ������� ���������� ����������� � ��</returns>
        protected abstract bool Connect();
        /// <summary>
        /// ��������� ������������� ����� ���������� � ��
        /// </summary>
        /// <returns>��������� ������� ��������� ����������� � ��</returns>
        protected abstract bool Disconnect();
        /// <summary>
        /// ��������� ������������� ����� ���������� � ��
        /// </summary>
        /// <param name="err">��������� ������� ��������� ����������� � ��</param>
        public abstract void Disconnect(out int err);
        /// <summary>
        /// ��������� ������� ���������� - ����������� �������
        /// </summary>
        /// <param name="table">������� ��� ��������</param>
        /// <param name="query">������ ��� ��������� ��������</param>
        /// <returns>��������� ������� ��������� ��������</returns>
        protected abstract bool GetData(DataTable table, object query);
    }
}
