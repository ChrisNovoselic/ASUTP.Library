using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
//using System.ComponentModel;
using System.Data;
//using System.Data.SqlClient;
using System.Data.OleDb;
using System.IO;
//using MySql.Data.MySqlClient;
using System.Threading;
using System.Globalization;

using HClassLibrary;

namespace HClassLibrary
{
    public abstract class HStates : object
    {
        protected DelegateFunc delegateStartWait;
        protected DelegateFunc delegateStopWait;
        protected DelegateFunc delegateEventUpdate;

        protected DelegateFunc errorReport;
        protected DelegateFunc actionReport;

        protected int m_IdListenerCurrent;

        /// <summary>
        /// ������ ��� ������������� ��������� ������ ���������
        /// </summary>
        protected Object m_lockState;

        protected Thread taskThread;
        protected Semaphore semaState;
        public enum INDEX_WAITHANDLE_REASON { SUCCESS, ERROR, BREAK, COUNT_INDEX_WAITHANDLE_REASON }
        protected WaitHandle [] m_waitHandleState;
        //protected AutoResetEvent evStateEnd;
        public volatile int threadIsWorking;
        protected volatile bool newState;
        protected volatile List<int /*StatesMachine*/> states;

        protected Dictionary <int, int []> m_dictIdListeners;

        private bool actived;
        public bool m_bIsActive { get { return actived; } }

        public HStates()
        {
            m_IdListenerCurrent = -1;

            m_dictIdListeners = new Dictionary<int,int[]> ();

            Initialize ();
        }

        protected virtual void Initialize () {
            actived = false;
            threadIsWorking = -1;

            m_lockState = new Object();

            states = new List<int /*StatesMachine*/>();
        }

        public virtual void Activate(bool active)
        {
            if (active == true) threadIsWorking++; else ;

            if (actived == active)
            {
                return ;
            }
            else
            {
                actived = active;
            }
        }

        protected void register(int id, ConnectionSettings connSett, string name, int indx)
        {
            m_dictIdListeners[id][indx] = DbSources.Sources().Register(connSett, true, @"���=" + name + @", DESC=" + indx.ToString());
        }

        public abstract void StartDbInterfaces();

        private void stopDbInterfaces()
        {
            if (!(m_dictIdListeners == null))
                foreach (int key in m_dictIdListeners.Keys)
                    for (int i = 0; i < m_dictIdListeners[key].Length; i++)
                    {
                        if (!(m_dictIdListeners[key][i] < 0))
                        {
                            DbSources.Sources().UnRegister(m_dictIdListeners[key][i]);
                            m_dictIdListeners[key][i] = -1;
                        }
                        else
                            ;
                    }
            else
                //������ ������ ���-���� ����������������
                Logging.Logg().Error(@"HStates::stopDbInterfaces () - m_dictIdListeners == null ...");
        }

        public void StopDbInterfaces()
        {
            stopDbInterfaces();
        }

        public void SetDelegateWait(DelegateFunc dStart, DelegateFunc dStop, DelegateFunc dStatus)
        {
            this.delegateStartWait = dStart;
            this.delegateStopWait = dStop;
            this.delegateEventUpdate = dStatus;
        }

        public void SetDelegateReport(DelegateFunc ferr, DelegateFunc fact)
        {
            this.errorReport = ferr;
            this.actionReport = fact;
        }

        protected void MessageBox(string msg, MessageBoxButtons btn = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.Error)
        {
            //MessageBox.Show(this, msg, "������", MessageBoxButtons.OK, MessageBoxIcon.Error);

            Logging.Logg().Error(msg);
        }

        public abstract void ClearValues();

        protected virtual void InitializeSyncState ()
        {
            if (m_waitHandleState == null)
                m_waitHandleState = new WaitHandle [1];
            else
                ;

            m_waitHandleState [(int)INDEX_WAITHANDLE_REASON.SUCCESS] = new AutoResetEvent(true);
        }

        //protected abstract bool InitDbInterfaces ();

        public void Request(int idListener, string request)
        {
            DbSources.Sources().Request(m_IdListenerCurrent = idListener, request);
        }

        public virtual bool Response(int idListener, out bool error, out DataTable table/*, bool bIsTec*/)
        {
            return DbSources.Sources().Response(idListener, out error, out table);
        }

        public virtual bool Response(out bool error, out DataTable table/*, bool bIsTec*/)
        {
            return DbSources.Sources().Response(m_IdListenerCurrent, out error, out table);
        }

        protected abstract bool StateRequest(int /*StatesMachine*/ state);

        protected abstract bool StateCheckResponse(int /*StatesMachine*/ state, out bool error, out DataTable table);

        protected abstract bool StateResponse(int /*StatesMachine*/ state, DataTable table);

        protected abstract void StateErrors(int /*StatesMachine*/ state, bool response);

        public virtual void Start()
        {
            threadIsWorking = 0;
            taskThread = new Thread(new ParameterizedThreadStart(TecView_ThreadFunction));
            taskThread.Name = "��������� � ���";
            taskThread.IsBackground = true;

            semaState = new Semaphore(1, 1);

            InitializeSyncState ();

            semaState.WaitOne();
            taskThread.Start();
        }

        public virtual void ClearStates()
        {
            //lock (m_lockState)
            //{
                newState = true;
                states.Clear ();
        }

        public virtual void Stop()
        {
            bool joined;
            threadIsWorking = -1;

            StopDbInterfaces ();
            
            ClearStates ();

            if ((!(taskThread == null)) && taskThread.IsAlive)
            {
                try { semaState.Release(1); }
                catch (Exception e)
                {
                    Logging.Logg().Exception(e, "HAdmin::StopThreadSourceData () - semaState.Release(1)");
                }

                joined = taskThread.Join(666);
                if (joined == false)
                    taskThread.Abort();
                else
                    ;
            }
            else ;
        }

        private void TecView_ThreadFunction(object data)
        {
            int index;
            int /*StatesMachine*/ currentState;

            while (! (threadIsWorking < 0))
            {
                semaState.WaitOne();

                index = 0;

                lock (m_lockState)
                {
                    if (states.Count == 0)
                        continue;
                    else
                        ;

                    currentState = states[index];
                    newState = false;
                }

                while (true)
                {
                    bool requestIsOk = true;
                    bool error = true;
                    bool dataPresent = false;
                    DataTable table = null;
                    for (int i = 0; i < DbInterface.MAX_RETRY && !dataPresent && !newState; i++)
                    {
                        if (error)
                        {
                            requestIsOk = StateRequest(currentState);
                            if (!requestIsOk)
                                break;
                            else
                                ;
                        }
                        else
                            ;

                        error = false;
                        for (int j = 0; j < DbInterface.MAX_WAIT_COUNT && !dataPresent && !error && !newState; j++)
                        {
                            System.Threading.Thread.Sleep(DbInterface.WAIT_TIME_MS);
                            dataPresent = StateCheckResponse(currentState, out error, out table);
                        }
                    }

                    if (requestIsOk)
                    {
                        bool responseIsOk = true;
                        if ((dataPresent == true) && (error == false) && (newState == false))
                            responseIsOk = StateResponse(currentState, table);
                        else
                            ;

                        if (((responseIsOk == false) || (dataPresent == false) || (error == true)) && (newState == false))
                        {
                            StateErrors(currentState, !responseIsOk);
                            lock (m_lockState)
                            {
                                if (newState == false)
                                {
                                    states.Clear();
                                    break;
                                }
                                else
                                    ;
                            }
                        }
                        else
                            ;
                    }
                    else
                    {
                        lock (m_lockState)
                        {
                            if (newState == false)
                            {
                                states.Clear();
                                break;
                            }
                            else
                                ;
                        }
                    }

                    index++;

                    lock (m_lockState)
                    {
                        if (index == states.Count)
                            break;
                        else
                            ;

                        if (newState)
                            break;
                        else
                            ;
                        currentState = states[index];
                    }
                }

                //��������� ��������� ���� �������
                completeHandleStates();
            }
            try
            {
                semaState.Release(1);
            }
            catch (Exception e)
            { //System.Threading.SemaphoreFullException
                Logging.Logg().Exception(e, "HAdmin::TecView_ThreadFunction () - semaState.Release(1)");
            }
        }

        /// <summary>
        /// ���������� ������� ��������� ��������� ���� �������
        /// </summary>
        protected void completeHandleStates () {
            try { ((AutoResetEvent)m_waitHandleState[0]).Set (); }
            catch (Exception e) {
                Logging.Logg().Exception(e, "TecView_ThreadFunction () - m_waitHandleState[0]).Set()");
            }
        }

        protected void GetCurrentTimeRequest(DbInterface.DB_TSQL_INTERFACE_TYPE typeDB, int idListatener)
        {
            string query = string.Empty;

            switch (typeDB)
            {
                case DbInterface.DB_TSQL_INTERFACE_TYPE.MySQL:
                    query = @"SELECT now()";
                    break;
                case DbInterface.DB_TSQL_INTERFACE_TYPE.MSSQL:
                    query = @"SELECT GETDATE()";
                    break;
                default:
                    break;
            }

            if (query.Equals(string.Empty) == false)
                Request(idListatener, query);
            else
                ;
        }

        protected void abortThreadGetValues(INDEX_WAITHANDLE_REASON reason)
        {
            if (m_waitHandleState.Length > (int)reason)
            {
                ((ManualResetEvent)m_waitHandleState[(int)reason]).Set();
            }
            else
                ;
        }

        private static string s_Name_Moscow_TimeZone = @"Russian Standard Time";
        public static DateTime ToMoscowTimeZone(DateTime dt)
        //public static DateTime ToCurrentTimeZone(DateTime dt, int offset_msc)
        {
            DateTime dtRes;

            if (! (dt.Kind == DateTimeKind.Local)) {
            //    dtRes = TimeZoneInfo.ConvertTimeFromUtc(dt, TimeZoneInfo.FindSystemTimeZoneById(s_Name_Moscow_TimeZone));
                dtRes = dt.Add(GetUTCOffsetOfMoscowTimeZone ());
            } else {
                dtRes = dt - TimeZoneInfo.Local.GetUtcOffset (dt);
                if (dtRes.IsDaylightSavingTime () == true) {
                    dtRes = dtRes.AddHours(-1);
                } else { }

                dtRes = dtRes.Add(GetUTCOffsetOfMoscowTimeZone());
            //    //dtRes = dtRes.Add(GetUTCOffsetOfCurrentTimeZone(offset_msc));
            }

            return dtRes;
        }

        //public static TimeSpan GetOffsetOfCurrentTimeZone()
        //{
        //    return DateTime.Now - HAdmin.ToCurrentTimeZone(TimeZone.CurrentTimeZone.ToUniversalTime(DateTime.Now));
        //}

        public static TimeSpan GetUTCOffsetOfMoscowTimeZone()
        {
            ////������������ ���� �� ��
            //System.Collections.ObjectModel.ReadOnlyCollection <TimeZoneInfo> tzi = TimeZoneInfo.GetSystemTimeZones ();
            //foreach (TimeZoneInfo tz in tzi) {
            //    Console.WriteLine (tz.DisplayName + @", " +  tz.StandardName + @", " + tz.Id);
            //}

            ////������� �1 - ��������, ���� � ������������ ����������� ���������� (�������� ������� 26.10.2014)
            //return TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Now, HAdmin.s_Name_Moscow_TimeZone) - DateTime.UtcNow;
            ////������� �2 - ��������, ���� � ������������ ����������� ���������� (�������� ������� 26.10.2014)
            //return TimeZoneInfo.FindSystemTimeZoneById(HAdmin.s_Name_Moscow_TimeZone).GetUtcOffset(DateTime.Now);
            ////������� �3 - ��������, ���� � ������������ ����������� ���������� (�������� ������� 26.10.2014) + �������� �������� ���� ������������ �� ���
            //return DateTime.UtcNow - DateTime.Now - TimeSpan.FromHours(offset_msc);
            //������� �4
            return TimeSpan.FromHours (3);
        }

        public void ErrorReport (string msg) {
            if (!(errorReport == null))
            {
                FormMainBaseWithStatusStrip.m_report.ErrorReport (msg);
                errorReport ();
            }
            else
                ;
        }

        public void ActionReport(string msg)
        {
            if (! (actionReport == null))
            {
                FormMainBaseWithStatusStrip.m_report.ActionReport(msg);
                actionReport();
            }
            else
                ;
        }
    }
}
