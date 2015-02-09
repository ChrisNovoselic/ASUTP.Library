using System;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace HClassLibrary
{
    public class DbSources
    {        
        protected static DbSources m_this;
        protected Dictionary<int, DbInterface> m_dictDbInterfaces;

        protected class DbSourceListener
        {
            public volatile DbConnection dbConn;
            public volatile int idDbInterface;
            public volatile int iListenerId;

            public DbSourceListener(int id, int indx, DbConnection conn)
            {
                idDbInterface = id;
                iListenerId = indx;
                dbConn = conn;
            }
        }
        protected Dictionary <int, DbSourceListener> m_dictListeners;
        private object m_objDictListeners;

        protected DbSources()
        {
            m_dictDbInterfaces = new Dictionary <int, DbInterface> ();
            m_dictListeners = new Dictionary<int,DbSourceListener> ();
            m_objDictListeners = new object ();
        }

        public static DbSources Sources () {
            if (m_this == null)
                m_this = new DbSources ();
            else
                ;

            return m_this;
        }

        /// <summary>
        /// Регистриует клиента соединения, активным или нет, при необходимости принудительно отдельный экземпляр
        /// </summary>
        /// <param name="connSett">параметры соединения</param>
        /// <param name="active">признак активности</param>
        /// <param name="bReq">признак принудительного создания отдельного экземпляра</param>
        /// <returns></returns>
        public virtual int Register(object connSett, bool active, string desc, bool bReq = false)
        {
            int id = -1,
                err = -1;

            lock (m_objDictListeners)
            {
                if (connSett is ConnectionSettings == true)
                    if ((m_dictDbInterfaces.ContainsKey(((ConnectionSettings)connSett).id) == true) && (bReq == false))
                    {
                        id = m_dictDbInterfaces[((ConnectionSettings)connSett).id].ListenerRegister();
                    }
                    else 
                        ;
                else
                    if (connSett is string == true) {
                    }
                    else
                        ;

                if ((id < 0) && (m_dictDbInterfaces.ContainsKey(((ConnectionSettings)connSett).id) == false))
                {
                    string dbNameType = string.Empty;
                    DbTSQLInterface.DB_TSQL_INTERFACE_TYPE dbType = DbTSQLInterface.DB_TSQL_INTERFACE_TYPE.UNKNOWN;
                    switch (((ConnectionSettings)connSett).port)
                    {
                        case -666:
                            dbType = DbTSQLInterface.DB_TSQL_INTERFACE_TYPE.ModesCentre;
                            break;
                        case 1433:
                            dbType = DbTSQLInterface.DB_TSQL_INTERFACE_TYPE.MSSQL;
                            break;
                        case 3306:
                            dbType = DbTSQLInterface.DB_TSQL_INTERFACE_TYPE.MySQL;                        
                            break;
                        default:
                            break;
                    }

                    dbNameType = dbType.ToString();

                    switch (dbType) {
                        case DbInterface.DB_TSQL_INTERFACE_TYPE.ModesCentre:
                            //m_dictDbInterfaces.Add(((ConnectionSettings)connSett).id, new DbMCInterface (dbType, @"Интерфейс: " + dbNameType));
                            break;
                        case DbInterface.DB_TSQL_INTERFACE_TYPE.MSSQL:
                        case DbInterface.DB_TSQL_INTERFACE_TYPE.MySQL:
                            m_dictDbInterfaces.Add(((ConnectionSettings)connSett).id, new DbTSQLInterface(dbType, @"Интерфейс: " + dbNameType + @"-БД" + @"; " + desc));
                            break;
                        default:
                            break;
                    }
                    if (active == true) m_dictDbInterfaces[((ConnectionSettings)connSett).id].Start(); else ;
                    m_dictDbInterfaces[((ConnectionSettings)connSett).id].SetConnectionSettings(connSett, active);

                    id = m_dictDbInterfaces[((ConnectionSettings)connSett).id].ListenerRegister();
                }
                else
                    ; // m_dictDbInterfaces[((ConnectionSettings)connSett).id].Name = desc;
            }

            return RegisterListener (((ConnectionSettings)connSett).id, id, active, out err);
        }

        public void SetConnectionSettings (int id, ConnectionSettings connSett, bool active) {
            if ((m_dictListeners.ContainsKey (id) == true) && (m_dictDbInterfaces.ContainsKey (connSett.id) == true) &&
                (m_dictListeners[id].idDbInterface == connSett.id))
            {
                m_dictDbInterfaces[m_dictListeners[id].idDbInterface].SetConnectionSettings(connSett, active);
            }
            else 
                ;
        }

        protected int RegisterListener(int id, int idListener, bool active, out int err)
        {
            int iRes = -1;

            lock (m_objDictListeners) {
                for (iRes = 0; iRes < m_dictListeners.Keys.Count; iRes ++)
                {
                    if (m_dictListeners.ContainsKey(iRes) == false)
                    {
                        //registerListener(iRes, ((ConnectionSettings)connSett).id, id, active, out err);
                        break;
                    }
                    else
                        ;
                }

                //if (! (iRes < m_dictListeners.Keys.Count))
                    registerListener(iRes, id, idListener, active, out err);
                //else
                //    ;
            }

            return iRes;
        }

        private void registerListener(int idReg, int id, int idListener, bool active, out int err)
        {
            err = -1;
            DbConnection dbConn = null;
            
            if (active == true) {
                err = 0;
            }
            else {
                ////Вариант №1
                //dbConn = DbTSQLInterface.getConnection((ConnectionSettings)m_dictDbInterfaces[id].m_connectionSettings, out err);

                //Вариант №2
                err = 0;
                dbConn = ((DbTSQLInterface)m_dictDbInterfaces[id]).GetConnection(out err);
            }

            m_dictListeners.Add(idReg, new DbSourceListener(id, idListener, dbConn));
        }

        public void UnRegister()
        {
            List <int> keys = new List<int> ();
            foreach (int id in m_dictListeners.Keys)
                keys.Add (id);

            foreach (int id in keys)
                UnRegister(id);
        }

        /// <summary>
        /// Отмена регистрации клиента соединения
        /// </summary>
        /// <param name="id">зарегистрированный идентификатор</param>
        public void UnRegister(int id)
        {
            int err = -1;

            //lock (m_objDictListeners) {
                if (m_dictListeners.ContainsKey (id) == true)
                {
                    if (m_dictDbInterfaces.ContainsKey(m_dictListeners[id].idDbInterface) == true) {
                        m_dictDbInterfaces[m_dictListeners[id].idDbInterface].ListenerUnregister(m_dictListeners[id].iListenerId);
                        if (! (m_dictDbInterfaces[m_dictListeners[id].idDbInterface].ListenerCount > 0)) {
                            m_dictDbInterfaces [m_dictListeners [id].idDbInterface].Stop ();

                            ////Вариант №1
                            //m_dictDbInterfaces.Remove(m_dictListeners[id].idDbInterface);
                            //if (m_dictListeners[id].dbConn == null)
                            //{
                            //}
                            //else
                            //{
                            //    DbTSQLInterface.closeConnection(ref m_dictListeners[id].dbConn, out err);                                
                            //}

                            //Вариант №2
                            m_dictDbInterfaces [m_dictListeners[id].idDbInterface].Disconnect (out err);
                            if (!(m_dictListeners[id].dbConn == null))
                                if (!(m_dictListeners[id].dbConn.State == ConnectionState.Closed))
                                    ; //Ошибка ???
                                else
                                    m_dictListeners[id].dbConn = null;
                            else
                                ;
                            m_dictDbInterfaces.Remove(m_dictListeners[id].idDbInterface);
                        }
                        else
                            ;
                    }
                    else
                        ;

                    m_dictListeners.Remove(id);
                }
                else
                    ;
            //}
        }

        /// <summary>
        /// Отправляет запрос к источнику БД с идентификатором
        /// </summary>
        /// <param name="id">идентификатор</param>
        /// <param name="query">запрос</param>
        public void Request (int id, string query) {
            if (m_dictListeners.ContainsKey (id) == true)
            {
                m_dictDbInterfaces[m_dictListeners[id].idDbInterface].Request(m_dictListeners[id].iListenerId, query);
            }
            else
                ;
        }

        /// <summary>
        /// Получает рез-т запроса  - таблицу, от источника с идентификатором, с признаком ошибки
        /// </summary>
        /// <param name="id">идентификатор</param>
        /// <param name="err">признак4 ошибки</param>
        /// <param name="tableRes">результирующая таблица</param>
        public int Response(int id, out bool err, out DataTable tableRes)
        {
            int iRes = -1;

            tableRes = null;
            err = true;

            //lock (m_objDictListeners) {
                if (m_dictListeners.ContainsKey (id) == true)
                    if (m_dictDbInterfaces.ContainsKey(m_dictListeners[id].idDbInterface) == true)
                        iRes = m_dictDbInterfaces[m_dictListeners[id].idDbInterface].Response(m_dictListeners[id].iListenerId, out err, out tableRes);
                    else
                        ;
                else
                    ;
            //}

            return iRes;
        }

        public DbConnection GetConnection (int id, out int err) {
            DbConnection res = null;
            err = -1;

            //lock (m_objDictListeners) {
                if ((m_dictListeners.ContainsKey (id) == true) && (! (m_dictListeners [id].dbConn == null)))
                {
                    res = m_dictListeners[id].dbConn;
                    err = 0;
                }
                else
                    ;
            //}

            return res;
        }
    }
}