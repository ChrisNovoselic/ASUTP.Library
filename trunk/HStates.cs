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

        protected DelegateStringFunc errorReport;
        protected DelegateStringFunc warningReport;
        protected DelegateStringFunc actionReport;
        protected DelegateBoolFunc clearReportStates;

        protected int m_IdListenerCurrent;

        /// <summary>
        /// Объект для синхронизации изменения списка состояний
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

        public virtual bool Activate(bool active)
        {
            bool bRes = true;
            
            if (active == true) threadIsWorking++; else ;

            if (actived == active)
            {
                bRes = false;
            }
            else
            {
                actived = active;
            }

            return bRes;
        }

        protected void register(int id, ConnectionSettings connSett, string name, int indx)
        {
            m_dictIdListeners[id][indx] = DbSources.Sources().Register(connSett, true, @"ТЭЦ=" + name + @", DESC=" + indx.ToString());
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
                //Вообще нельзя что-либо инициализировать
                Logging.Logg().Error(@"HStates::stopDbInterfaces () - m_dictIdListeners == null ...", Logging.INDEX_MESSAGE.NOT_SET);
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

        public void SetDelegateReport(DelegateStringFunc ferr, DelegateStringFunc fwar, DelegateStringFunc fact, DelegateBoolFunc fclr)
        {
            this.errorReport = ferr;
            this.warningReport = fwar;
            this.actionReport = fact;
            this.clearReportStates = fclr;
        }

        protected void MessageBox(string msg, MessageBoxButtons btn = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.Error)
        {
            //MessageBox.Show(this, msg, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);

            Logging.Logg().Error(msg, Logging.INDEX_MESSAGE.NOT_SET);
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

        public virtual int Response(int idListener, out bool error, out DataTable table/*, bool bIsTec*/)
        {
            return DbSources.Sources().Response(idListener, out error, out table);
        }

        public virtual int Response(out bool error, out DataTable table/*, bool bIsTec*/)
        {
            return DbSources.Sources().Response(m_IdListenerCurrent, out error, out table);
        }

        protected abstract int StateRequest(int /*StatesMachine*/ state);

        protected abstract int StateCheckResponse(int /*StatesMachine*/ state, out bool error, out DataTable table);

        protected abstract int StateResponse(int /*StatesMachine*/ state, DataTable table);

        protected abstract void StateErrors(int /*StatesMachine*/ state, bool response);

        protected abstract void StateWarnings(int /*StatesMachine*/ state, bool response);

        public virtual void Start()
        {
            Thread.CurrentThread.CurrentCulture =
            Thread.CurrentThread.CurrentUICulture =
                ProgramBase.ss_MainCultureInfo;

            if (threadIsWorking < 0)
            {
                threadIsWorking = 0;
                taskThread = new Thread(new ParameterizedThreadStart(TecView_ThreadFunction));
                taskThread.Name = "Интерфейс к РДГ";
                taskThread.IsBackground = true;
                taskThread.CurrentCulture =
                taskThread.CurrentUICulture =
                    ProgramBase.ss_MainCultureInfo;

                semaState = new Semaphore(1, 1);

                InitializeSyncState();

                semaState.WaitOne();
                taskThread.Start();
            }
            else
                ;
        }

        public virtual void ClearStates()
        {
            //lock (m_lockState)
            //{
                newState = true;
                states.Clear ();

                if (!(clearReportStates == null))
                    clearReportStates (true);
                else
                    ;
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
                    Logging.Logg().Exception(e, Logging.INDEX_MESSAGE.NOT_SET, "HAdmin::StopThreadSourceData () - semaState.Release(1)");
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
                    int requestIsOk = 0;
                    bool error = true;
                    int dataPresent = -1;
                    DataTable table = null;
                    for (int i = 0; i < DbInterface.MAX_RETRY && (! (dataPresent == 0)) && (newState == false); i++)
                    {
                        if (error)
                        {
                            requestIsOk = StateRequest(currentState);
                            if (! (requestIsOk == 0))
                                break;
                            else
                                ;
                        }
                        else
                            ;

                        error = false;
                        for (int j = 0; j < DbInterface.MAX_WAIT_COUNT && (! (dataPresent == 0)) && (error == false) && (newState == false); j++)
                        {
                            System.Threading.Thread.Sleep(DbInterface.WAIT_TIME_MS);
                            dataPresent = StateCheckResponse(currentState, out error, out table);
                        }
                    }

                    if (requestIsOk == 0)
                    {
                        int responseIsOk = 0;
                        if ((dataPresent == 0) && (error == false) && (newState == false))
                            responseIsOk = StateResponse(currentState, table);
                        else
                            responseIsOk = -1;

                        if (((! (responseIsOk == 0)) || (! (dataPresent == 0)) || (error == true)) && (newState == false))
                        {
                            if (responseIsOk < 0)
                            {
                                StateErrors(currentState, true);
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
                                StateWarnings(currentState, true);
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

                //Закончена обработка всех событий
                completeHandleStates();
            }
            try
            {
                semaState.Release(1);
            }
            catch (Exception e)
            { //System.Threading.SemaphoreFullException
                Logging.Logg().Exception(e, Logging.INDEX_MESSAGE.NOT_SET, "HAdmin::TecView_ThreadFunction () - semaState.Release(1)");
            }
        }

        /// <summary>
        /// Установить признак окончания обработки всех событий
        /// </summary>
        protected void completeHandleStates () {
            try { ((AutoResetEvent)m_waitHandleState[0]).Set (); }
            catch (Exception e) {
                Logging.Logg().Exception(e, Logging.INDEX_MESSAGE.NOT_SET, "TecView_ThreadFunction () - m_waitHandleState[0]).Set()");
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
            ////Перечисление всех зо ОС
            //System.Collections.ObjectModel.ReadOnlyCollection <TimeZoneInfo> tzi = TimeZoneInfo.GetSystemTimeZones ();
            //foreach (TimeZoneInfo tz in tzi) {
            //    Console.WriteLine (tz.DisplayName + @", " +  tz.StandardName + @", " + tz.Id);
            //}

            ////Вариант №1 - работает, если у пользователя установлено обновление (сезонный переход 26.10.2014)
            //return TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Now, HAdmin.s_Name_Moscow_TimeZone) - DateTime.UtcNow;
            ////Вариант №2 - работает, если у пользователя установлено обновление (сезонный переход 26.10.2014)
            //return TimeZoneInfo.FindSystemTimeZoneById(HAdmin.s_Name_Moscow_TimeZone).GetUtcOffset(DateTime.Now);
            ////Вариант №3 - работает, если у пользователя установлено обновление (сезонный переход 26.10.2014) + известно смещение зоны пользователя от МСК
            //return DateTime.UtcNow - DateTime.Now - TimeSpan.FromHours(offset_msc);
            //Вариант №4
            return TimeSpan.FromHours (3);
        }

        public void ErrorReport (string msg) {
            if (!(errorReport == null))
                errorReport (msg);
            else
                ;
        }

        public void WarningReport(string msg)
        {
            if (!(warningReport == null))
                warningReport(msg);
            else
                ;
        }

        public void ActionReport(string msg)
        {
            if (! (actionReport == null))
                actionReport(msg);
            else
                ;
        }

        public void ReportClear (bool bClear)
        {
            clearReportStates (bClear);
        }
    }
}
