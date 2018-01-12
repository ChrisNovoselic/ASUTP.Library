using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Data;
using System.Data.Common;
using ASUTP.Core;

//using MySql.Data.MySqlClient;

namespace ASUTP.Database
{
    /// <summary>
    /// Класс для работы с параметрами списка источников данных
    /// </summary>
    public class ConnectionSettingsSource
    {
        public ConnectionSettingsSource ()
        {
        }

        /// <summary>
        /// Запрос для получения таблицы (одна строка) с параметрами источника данных
        /// </summary>
        /// <param name="id">идентификатор источника данных в таблице 'SOURCE'</param>
        /// <returns>строка с параметрами источника данных</returns>
        private static string ConnectionSettingsRequest (int id)
        {
            return "SELECT src.* FROM SOURCE src WHERE src.ID = " + id.ToString();
        }

        /// <summary>
        /// Запрос пароля (шифрованного для 2.Х.Х и не шифрованного для 1.9.Х)
        /// </summary>
        /// <param name="id">идентификатор пользователя(роли) - часть составного ключа</param>
        /// <param name="id_role">роль пользователя (только для 2.Х.Х), "роль" источника данных 501</param>
        /// <returns>текст запроса</returns>
        private static string PasswordRequest(int id, int id_role)
        {
            string strRes = string.Empty;

            if (id_role < 0)
                id_role = 501;
            else
                ;

            strRes = "SELECT psw.* FROM passwords psw WHERE ";

            strRes += @"psw.ID_EXT = " + id.ToString() + " AND ";
            
            strRes += "ID_ROLE = " + id_role.ToString();

            return strRes;
        }

        /// <summary>
        /// Получение таблицы с параметрами соединения источника данных дешифрованным паролем
        /// </summary>
        /// <param name="src">таблица - рез-т выполнения функции 'GetConnectionSettings'</param>
        /// <param name="row_src"></param>
        /// <param name="psw"></param>
        /// <param name="row_psw"></param>
        /// <returns></returns>
        private static DataTable GetConnectionSettings(ref DataTable src, int row_src, ref DataTable psw, int row_psw)
        {
            string errMsg, strPsw;

            errMsg = strPsw = string.Empty;

            if (psw.Rows.Count == 1) {
                //hash = psw.Rows[row_psw]["HASH"].ToString ().ToCharArray ();
                //len_hash = psw.Rows[row_psw]["HASH"].ToString().Length;

                strPsw = Crypt.Crypting ().Decrypt (psw.Rows[row_psw]["HASH"].ToString (), Crypt.KEY);
            }
            else
                ;

            //Проверка с каким вариантом БД происходит работа
            if (src.Columns.IndexOf ("PASSWORD") < 0) {
                src.Columns.Add("PASSWORD", typeof(string));
            }
            else
                ;

            if (row_src < src.Rows.Count)
                src.Rows[row_src]["PASSWORD"] = strPsw;
            else
                ;

            return src;
        }

        /// <summary>
        /// Возвратить таблицу (!одна строка) с параметрами для устанвления соединения с БД
        /// </summary>
        /// <param name="iListenerId">Идентификатор актуального соединения с БД, в которой размещается информация о запрашиваемых параметрах (БД конфигурации)</param>
        /// <param name="id_ext">Идентификатор источника данных (параметры для соединения с источником данных)</param>
        /// <param name="id_role">Идентификатор типа источника данных</param>
        /// <param name="er">Признак ошибки впри выполнении запроса значений</param>
        /// <returns>Таблица (!одна строка) с параметрами соединения</returns>
        public static DataTable GetConnectionSettings (int iListenerId, int id_ext, int id_role, out int er)
        {
            DataTable tableRes = new DataTable ();

            DbConnection dbConn = DbSources.Sources ().GetConnection (iListenerId, out er);

            if (er == 0)
                tableRes = GetConnectionSettings (ref dbConn, id_ext, id_role, out er);
            else
                ;

            return tableRes;
        }

        /// <summary>
        /// Возвратить таблицу (!одна строка) с параметрами для устанвления соединения с БД
        /// </summary>
        /// <param name="conn">Объект для соединения с БД, в которой размещается информация о запрашиваемых параметрах (БД конфигурации)</param>
        /// <param name="id_ext">Идентификатор источника данных (параметры для соединения с источником данных)</param>
        /// <param name="id_role">Идентификатор типа источника данных</param>
        /// <param name="er">Признак ошибки впри выполнении запроса значений</param>
        /// <returns>Таблица (!одна строка) с параметрами соединения</returns>
        public static DataTable GetConnectionSettings(ref DbConnection conn, int id_ext, int id_role, out int er)
        {
            er = 0;

            DataTable tableRes = DbTSQLInterface.Select(ref conn, ConnectionSettingsRequest(id_ext), null, null, out er)
                , tablePsw = DbTSQLInterface.Select(ref conn, PasswordRequest(id_ext, id_role), null, null, out er);

            if ((tableRes.Rows.Count > 0) && (tablePsw.Rows.Count > 0))
                tableRes = GetConnectionSettings(ref tableRes, 0, ref tablePsw, 0);
            else {
                if ((!(tablePsw.Rows.Count > 0)) && (tableRes.Columns.IndexOf (@"PASSWORD") < 0))
                {
                    er = -1;
                }
                else {
                    if (tableRes.Columns.IndexOf (@"PASSWORD") < 0)
                        tableRes.Columns.Add (@"PASSWORD", typeof (string));
                    else
                        ;

                    er = 0;
                }
            }

            return tableRes;
        }

        public void Read(int idListener, out List<ConnectionSettings> listConnSett, out int err, out string mes)
        {
            listConnSett = new List<ConnectionSettings> ();
            err = 0;
            mes = string.Empty;

            int i = -1;

            DbConnection conn = DbSources.Sources().GetConnection(idListener, out err);
            //DbConnection conn = DbTSQLInterface.GetConnection (m_ConnectionSettings, out err);

            if (err == 0)
            {
                DataTable tableSource = DbTSQLInterface.Select(ref conn, "SELECT * FROM SOURCE", null, null, out err),
                            tablePsw;

                if (err == 0)
                {
                    for (i = 0; i < tableSource.Rows.Count; i ++)
                    {
                        listConnSett.Add (new ConnectionSettings ());

                        listConnSett[i].id = Convert.ToInt32(tableSource.Rows[i]["ID"]);
                        listConnSett[i].name = tableSource.Rows[i]["NAME_SHR"].ToString();

                        listConnSett [i].server = tableSource.Rows [i] ["IP"].ToString ();
                        listConnSett[i].instance = (!(tableSource.Rows[i]["INSTANCE"] is DBNull)) ? tableSource.Rows[i]["INSTANCE"].ToString().Trim() : string.Empty;
                        listConnSett[i].port = Convert.ToInt32 (tableSource.Rows[i]["PORT"]);
                        listConnSett[i].dbName = tableSource.Rows[i]["DB_NAME"].ToString();
                        listConnSett[i].userName = tableSource.Rows[i]["UID"].ToString();
                        ////Ignore
                        //listConnSett[i].ignore = tableSource.Columns.IndexOf(@"IGNORE") < 0 ? false : Convert.ToInt32(tableSource.Rows[i]["IGNORE"].ToString()) == 1;

                        //TYPE_DATABASE_CFG.CFG_200 = ???
                        tablePsw = DbTSQLInterface.Select(ref conn, PasswordRequest(Convert.ToInt32(tableSource.Rows[i]["ID"]), 501), null, null, out err);

                        tableSource = GetConnectionSettings(ref tableSource, i, ref tablePsw, 0);
                        //Password
                        listConnSett[i].password = tableSource.Rows[i]["PASSWORD"].ToString();
                    }
                }
                else
                    ;
            }
            else
                ;

            //DbTSQLInterface.CloseConnection (conn, out err);
        }

        public void Save(int idListener, List<ConnectionSettings> listConnSett, out int err)
        {
            err = 1;
            int i = -1,c = -1;
            string strQuery, psw
                , hash;
            //char []hash;
            //StringBuilder sb;
            
            strQuery = psw = string.Empty;

            DbConnection conn = DbSources.Sources().GetConnection(idListener, out err);
            //DbConnection conn = DbTSQLInterface.GetConnection (m_ConnectionSettings, out err);

            if (err == 0)
            {
                DataTable tableSource = null,
                            tablePsw = null;

                if (err == 0)
                {
                    for (i = 0; i < listConnSett.Count; i++)
                    {
                        tableSource = DbTSQLInterface.Select(ref conn, ConnectionSettingsRequest(listConnSett[i].id), null, null, out err);
                        //TYPE_DATABASE_CFG.CFG_200 = ???
                        tablePsw = DbTSQLInterface.Select(ref conn, PasswordRequest(listConnSett[i].id, 501), null, null, out err);

                        if (tableSource.Rows.Count == 0)
                        {//INSERT
                        }
                        else
                            if (tableSource.Rows.Count == 1)
                            {//UPDATE
                                if ((listConnSett[i].server.Equals (tableSource.Rows [0]["IP"].ToString ()) == false)
                                    || (listConnSett[i].instance.Equals((!(tableSource.Rows[0]["INSTANCE"] is DBNull)) ? tableSource.Rows[0]["INSTANCE"].ToString().Trim() : string.Empty) == false)
                                    || (listConnSett[i].dbName.Equals(tableSource.Rows[0]["DB_NAME"].ToString()) == false)
                                    || (listConnSett[i].userName.Equals(tableSource.Rows[0]["UID"].ToString()) == false)
                                    )
                                {
                                    strQuery += "UPDATE SOURCE SET ";

                                    strQuery += "IP='" + listConnSett[i].server + "',";
                                    strQuery += "INSTANCE='" + listConnSett[i].instance + "',";
                                    strQuery += "DB_NAME='" + listConnSett[i].dbName + "',";
                                    strQuery += "UID='" + listConnSett[i].userName + "'";

                                    strQuery += " WHERE ID=" + listConnSett[i].id + ";";
                                }
                                else
                                    ; //Ничего не изменилось

                                if (listConnSett[i].password.Length > 0)
                                {
                                    //sb = new StringBuilder(listConnSett[i].password);
                                    //hash = Crypt.Crypting().to(sb, out err);
                                    hash = Crypt.Crypting().Encrypt(listConnSett[i].password, Crypt.KEY);

                                    //if (err > 0)
                                    if (hash.Length > 0)
                                    {
                                        if (tablePsw.Rows.Count == 0)
                                        {//INSERT
                                            strQuery += "INSERT INTO passwords (ID_EXT, ID_ROLE, HASH) VALUES (";

                                            strQuery += listConnSett[i].id + ", ";
                                            strQuery += 501 + ", ";
                                            strQuery += "'" + hash + "'";

                                            strQuery += ");";
                                        }
                                        else
                                            if (tablePsw.Rows.Count == 1)
                                            {//UPDATE
                                                strQuery += "UPDATE passwords SET ";

                                                strQuery += "HASH='" + hash + "'";

                                                strQuery += " WHERE ID_EXT=" + listConnSett[i].id;
                                                strQuery += " AND ";
                                                strQuery += "ID_ROLE=" + 501 + ";";
                                            }
                                            else
                                                ;
                                    }
                                    else
                                        ; //Ошибка шифрования пароля ИЛИ нет пароля
                                }
                                else
                                    ; //Нет пароля
                            }
                            else
                                ;
                    }

                    DbTSQLInterface.ExecNonQuery (ref conn, strQuery, null, null, out err);
                }
                else
                    ;
            }
            else
                ;

            //DbTSQLInterface.CloseConnection (conn, out err);
        }
    }
}
