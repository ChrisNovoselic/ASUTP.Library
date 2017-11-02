using ASUTP.Database;
using System;
using System.Data;
using System.Data.Common;

namespace ASUTP.Helper
{
    partial class HUsers
    {
        /// <summary>
        /// Класс для возвращения/установки значений параметров профиля пользователя
        /// </summary>
        protected class HProfiles
        {
            /// <summary>
            /// Наименование таблиц в БД
            /// </summary>
            public static string s_nameTableProfilesData = @"profiles"
                , s_nameTableProfilesUnit = @"profiles_unit";
            /// <summary>
            /// Таблица БД со значениями параметров профиля
            /// </summary>
            protected static DataTable m_tblValues;
            /// <summary>
            /// Таблица БД с описанием всех возможных параметров профиля
            /// </summary>
            protected static DataTable m_tblTypes;
            /// <summary>
            /// Таблица БД с описанием всех возможных параметров профиля
            /// ??? (Get в наименовании следует исключить)
            /// </summary>
            public static DataTable GetTableUnits
            {
                get
                {
                    return m_tblTypes;
                }
            }

            /// <summary>
            /// Функция подключения пользователя
            /// </summary>
            public HProfiles (int iListenerId, int id_role, int id_user)
            {
                Update (iListenerId, id_role, id_user, true);
            }

            /// <summary>
            /// Обновить(прочитать) значения параметров профиля, список параметров
            /// </summary>
            /// <param name="iListenerId">Идентификатор подписчика объекта обращения к данным</param>
            /// <param name="id_role">Идентификатор группы(роли) пользователей</param>
            /// <param name="id_user">Идентификатор пользователя</param>
            /// <param name="bThrow">Признак инициирования исключения при ошибке</param>
            public void Update (int iListenerId, int id_role, int id_user, bool bThrow)
            {
                int err = -1;
                string query = string.Empty
                    , errMsg = string.Empty;

                DbConnection dbConn = DbSources.Sources ().GetConnection (iListenerId, out err);

                if (!(err == 0))
                    errMsg = @"нет соединения с БД";
                else {
                    query = $@"SELECT * FROM {s_nameTableProfilesData} WHERE (ID_EXT={id_role} AND IS_ROLE=1) OR (ID_EXT={id_user} AND IS_ROLE=0)";
                    m_tblValues = DbTSQLInterface.Select (ref dbConn, query, null, null, out err);

                    if (!(err == 0))
                        errMsg = @"Ошибка при чтении НАСТРоек для группы(роли) (irole = " + id_role + @"), пользователя (iuser=" + id_user + @")";
                    else {
                        query = $@"SELECT * from {s_nameTableProfilesUnit}";
                        m_tblTypes = DbTSQLInterface.Select (ref dbConn, query, null, null, out err);

                        if (!(err == 0))
                            errMsg = @"Ошибка при чтении ТИПов ДАНных настроек для группы(роли) (irole = " + id_role + @"), пользователя (iuser=" + id_user + @")";
                        else
                            ;
                    }
                }

                if (
                    (!(err == 0))
                    && (bThrow == true)
                    )
                    throw new Exception ($"HProfiles::HProfiles () - {errMsg}...");
                else
                    ;
            }

            /// <summary>
            /// Функция получения значения права доступа
            /// </summary>
            /// <param name="id">ID типа(unit)</param>
            /// <returns>Признак права доступа к элменту с указанным идентификатором</returns>
            public static object GetAllowed (int id)
            {
                object objRes = false;
                bool bValidate = false;
                int indxRowAllowed = -1;
                Int16 val = -1
                   , type = -1;
                string strVal = string.Empty;

                DataRow [] rowsAllowed = m_tblValues.Select ($"ID_UNIT={id}");
                switch (rowsAllowed.Length) {
                    case 1:
                        indxRowAllowed = 0;
                        break;
                    case 2:
                        //В табл. с настройками возможность 'id' определена как для "роли", так и для "пользователя"
                        // требуется выбрать строку с 'IS_ROLE' == 0 (пользователя)
                        // ...
                        foreach (DataRow r in rowsAllowed) {
                            indxRowAllowed++;
                            if (Int16.Parse (r [@"IS_ROLE"].ToString ()) == 0)
                                break;
                            else
                                ;
                        }
                        break;
                    default: //Ошибка - исключение
                        throw new Exception ($"HUsers.HProfiles::GetAllowed (id={id}) - не найдено ни одной записи...");
                        //Logging.Logg().Error(@"HUsers.HProfiles::GetAllowed (id=" + id + @") - не найдено ни одной записи...", Logging.INDEX_MESSAGE.NOT_SET);
                        break;
                }

                // проверка не нужна, т.к. вызывается исключение
                //if ((!(indxRowAllowed < 0))
                //    && (indxRowAllowed < rowsAllowed.Length))
                //{
                strVal = !(indxRowAllowed < 0) ? rowsAllowed [indxRowAllowed] [@"VALUE"].ToString ().Trim () : string.Empty;

                //По идкнтификатору параметра должны знать тип...
                Int16.TryParse (m_tblTypes.Select (@"ID=" + id) [0] [@"ID_UNIT"].ToString (), out type);
                switch (type) {
                    case 8: //bool
                        bValidate = Int16.TryParse (strVal, out val);
                        if (bValidate == true)
                            objRes = val == 1;
                        else
                            objRes = false;

                        objRes = objRes.ToString ();
                        break;
                    case 9: //string
                    case 10: //int
                        objRes = strVal;
                        break;
                    default:
                        throw new Exception (@"HUsers.HProfiles::GetAllowed (id=" + id + @") - не найден тип параметра...");
                }
                //} else ;

                return objRes;
            }

            /// <summary>
            /// Установить значение для параметра профиля
            /// , если параметр не определен - добавить в таблицу БД
            /// </summary>
            public static void SetAllowed (int iListenerId, int id, string val)
            {
                string query = string.Empty;
                int err = -1
                    , cntRows = -1;
                DbConnection dbConn = null;

                //Проверить наличие индивидуальной записи...
                cntRows = m_tblValues.Select (@"ID_UNIT=" + id).Length;
                switch (cntRows) {
                    case 1: //Вставка записи...
                        query = $@"INSERT INTO {s_nameTableProfilesData} ([ID_EXT],[IS_ROLE],[ID_UNIT],[VALUE]) VALUES ({Id}, 0, {id}, '{val}')";
                        break;
                    case 2: //Обновление записи...
                        query = $@"UPDATE {s_nameTableProfilesData} SET [VALUE]='{val}' WHERE ID_EXT={Id} AND IS_ROLE=0 AND ID_UNIT={id}";
                        break;
                    default: //Ошибка - исключение
                        throw new Exception ($@"HUsers.HProfiles::SetAllowed (id={id}) - не найдено ни одной записи...");
                }

                dbConn = DbSources.Sources ().GetConnection (iListenerId, out err);
                if ((!(dbConn == null)) && (err == 0)) {
                    DbTSQLInterface.ExecNonQuery (ref dbConn, query, null, null, out err);
                    //Проверить результат сохранения...
                    if (err == 0) {//Обновить таблицу пользовательских настроек...
                        switch (cntRows) {
                            case 1: //Вставка записи...
                                m_tblValues.Rows.Add (new object [] { Id, 0, id, val });
                                break;
                            case 2: //Обновление записи...
                                DataRow [] rows = m_tblValues.Select ($@"ID_EXT={Id} AND IS_ROLE=0 AND ID_UNIT={id}");
                                rows [0] [@"VALUE"] = val;
                                break;
                            default: //Ошибка - исключение
                                     //Ошибка обработана - создано исключение...
                                break;
                        }
                    } else
                        ;
                } else
                    ;
            }
        }
    }
}
