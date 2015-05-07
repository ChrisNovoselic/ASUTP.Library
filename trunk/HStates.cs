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

using System.Globalization;

using HClassLibrary;

namespace HClassLibrary
{
    public abstract class HStates : HHandler
    {
        protected DelegateFunc delegateStartWait;
        protected DelegateFunc delegateStopWait;
        protected DelegateFunc delegateEventUpdate;

        protected DelegateStringFunc errorReport;
        protected DelegateStringFunc warningReport;
        protected DelegateStringFunc actionReport;
        protected DelegateBoolFunc clearReportStates;

        protected int m_IdListenerCurrent;
        protected Dictionary <int, int []> m_dictIdListeners;

        public HStates() : base ()
        {
            m_dictIdListeners = new Dictionary<int,int[]> ();
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

        //protected abstract bool InitDbInterfaces ();

        public void Request(int idListener, string request)
        {
            DbSources.Sources().Request(m_IdListenerCurrent = idListener, request);
        }

        public virtual int Response(int idListener, out bool error, out object obj/*, bool bIsTec*/)
        {
            obj = null;
            DataTable table = obj as DataTable;
            return DbSources.Sources().Response(idListener, out error, out table);
        }

        public virtual int Response(out bool error, out object outobj/*, bool bIsTec*/)
        {
            outobj = null;
            DataTable table = outobj as DataTable;
            return DbSources.Sources().Response(m_IdListenerCurrent, out error, out table);
        }

        //protected abstract int StateCheckResponse(int /*StatesMachine*/ state, out bool error, out DataTable table);

        //protected abstract int StateResponse(int /*StatesMachine*/ state, DataTable table);

        public override void ClearStates()
        {
            base.ClearStates ();

            if (!(clearReportStates == null))
                clearReportStates (true);
            else
                ;
        }

        public override void Stop()
        {
            StopDbInterfaces ();
            
            base.Stop ();
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
                case DbInterface.DB_TSQL_INTERFACE_TYPE.Oracle:
                    query = @"SELECT SYSTIMESTAMP FROM dual";
                    break;
                default:
                    break;
            }

            if (query.Equals(string.Empty) == false)
                Request(idListatener, query);
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
