using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms; //Application.ProductVersion
//using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.IO; //File
using System.Net;
using ASUTP.Database;
using ASUTP.Core;

namespace ASUTP.Helper
{    
    public partial class HUsers : object, IDisposable
    {
        private class DbTable_dictionary
        {
            enum INDEX_QUERY { SELECT, INSERT, UPDATE, DELETE }

            //ConnectionSettings m_connSett;
            DbConnection m_dbConn;
            string[] m_query;

            public DbTable_dictionary(ConnectionSettings connSett, string nameTable, string where)
            {
            }

            public void ReadString(string[] opt, string[] valDef)
            {
            }

            public void WriteString(string[] opt, string[] val)
            {
            }
        }

        /// <summary>
        /// Метод для освобождения ресурсов - реализация интерфейса 'IDisposable'
        /// </summary>
        public void Dispose ()
        {
        }

        /// <summary>
        /// Создать объект для доступа к значениям профиля пользователя
        /// </summary>
        /// <param name="iListenerId">Идентификатор подписки к источнику БД(конфигурации)</param>
        /// <param name="id_role">Идентификатор роли(группы), к которой принадлежит пользователь</param>
        /// <param name="id_user">Идентификатор пользователя</param>
        /// <returns>Объект для доступа к значениям профиля пользователя</returns>
        protected virtual HProfiles createProfiles(int iListenerId, int id_role, int id_user)
        {
            return new HProfiles(iListenerId, id_role, id_user);
        }

        /// <summary>
        /// Объект для доступа к значениям параметров профиля пользователя
        /// </summary>
        protected static HProfiles m_profiles;

        /// <summary>
        /// Аргумент командной строки
        /// </summary>
        protected struct ARGUMENTS
        {
            /// <summary>
            /// Ключ, по которому происходит распознование аргумента
            /// </summary>
            public string m_key;
            /// <summary>
            /// Строка помощи по использованию аршумента
            /// </summary>
            public string m_help;
            /// <summary>
            /// Состояние (значения)аргумента
            /// </summary>
            public INDEX_REGISTRATION m_indxRegistration;
            /// <summary>
            /// Тип (значения)аргумента
            /// </summary>
            public Type m_type;
        }

        /// <summary>
        /// Перечисление - возможные (индексы) некоторых данных о пользователе
        /// </summary>
        public enum INDEX_REGISTRATION { ID, DOMAIN_NAME, ROLE, ID_TEC, COUNT_INDEX_REGISTRATION };
        /// <summary>
        /// Массив значений с данными о пользователе (предустановленные в ??? файле/БД конфигурации)
        /// </summary>
        public static object [] s_REGISTRATION_INI = new object [(int)INDEX_REGISTRATION.COUNT_INDEX_REGISTRATION];
        /// <summary>
        /// Перечисление - возможные состояния значений аргументов
        /// </summary>
        protected enum STATE_REGISTRATION {
            /// <summary>
            /// Неизвестный источник
            /// </summary>
            UNKNOWN = -1,
            /// <summary>
            /// Получены из командной строки
            /// </summary>
            CMD,
            /*INI,*/
            /// <summary>
            /// Получены из "окружения" (Environment)
            /// </summary>
            ENV,
                COUNT_STATE_REGISTRATION
        };
        /// <summary>
        /// Массив с известными аргументами
        /// </summary>
        protected static ARGUMENTS[] m_Arguments = new ARGUMENTS[] //Длина = COUNT_INDEX_REGISTRATION
        {
            new ARGUMENTS() { m_key = @"iusr", m_indxRegistration = INDEX_REGISTRATION.ID, m_type = typeof(Int32), m_help = string.Format(@"/{0} - формат: /{0}=ИДЕНТИФИКАТОР_ПОЛЬЗОВАТЕЛЯ, , например: /{0}=59", @"iusr") }
            , new ARGUMENTS() { m_key = @"udn", m_indxRegistration = INDEX_REGISTRATION.DOMAIN_NAME, m_type = typeof(string), m_help = @"/udn - формат: /udn=ИМЯ_РАБСТАНЦИИ_ПОЛНОЕ::ИМЯ_ПОЛЬЗОВАТЕЛЯ_ПОЛНОЕ, например: /udn=NE2844.ne.ru::NE\vkaskad" }
            , new ARGUMENTS() { m_key = @"irole", m_indxRegistration = INDEX_REGISTRATION.ROLE, m_type = typeof(Int32), m_help = string.Format(@"/{0} - формат: /{0}=ИДЕНТИФИКАТОР_РОЛИ_ПОЛЬЗОВАТЕЛЯ, например: /{0}=3", @"irole") }
            , new ARGUMENTS() { m_key = @"itec", m_indxRegistration = INDEX_REGISTRATION.ID_TEC, m_type = typeof(Int32), m_help = string.Format(@"/{0} - формат: /{0}=ИДЕНТИФИКАТОР_ТЭЦ , например: /{0}=59", @"itec") }
            ,
        };

        protected static object [] s_DataRegistration;
        protected static STATE_REGISTRATION[] s_StateRegistration;

        private bool isRegistration {
            get {
                bool bRes = true;

                if (! (s_StateRegistration == null))
                    for (int i = 0; i < s_StateRegistration.Length; i++)
                        if (s_StateRegistration[i] == STATE_REGISTRATION.UNKNOWN) {
                            bRes = false;
                            break;
                        }
                        else ;
                else
                    bRes = false;

                return bRes;
            }
        }

        /// <summary>
        /// Массив методов регистрации(получения данных о пользователе) в различных средах
        ///  , в соответствии с 'STATE_REGISTRATION'
        /// </summary>
        private DelegateObjectFunc [] f_arRegistration; // = { registrationCmdLine, registrationINI, registrationEnv };

        /// <summary>
        /// Конструктор - основной (с аргументами)
        /// </summary>
        /// <param name="iListenerId">Идентификатор подписки к источнику данных (БД конфигурации)</param>
        /// <param name="mode">Режим регистрации</param>
        public HUsers(int iListenerId, MODE_REGISTRATION mode = MODE_REGISTRATION.USER_DOMAINNAME)
        {
            object argFReg = null;

            Logging.Logg().Action(string.Format(@"HUsers::HUsers () - ... кол-во аргументов ком./строки = {0}; MashineName::DomainUserName={1}::{2}\{3}"
                , + (Environment.GetCommandLineArgs().Length - 1), MachineName, Environment.UserDomainName , Environment.UserName)
                , Logging.INDEX_MESSAGE.NOT_SET);

            try {
                s_modeRegistration = mode;

                f_arRegistration = new DelegateObjectFunc[(int)STATE_REGISTRATION.COUNT_STATE_REGISTRATION];
                f_arRegistration[(int)STATE_REGISTRATION.CMD] = new DelegateObjectFunc (registrationCmdLine);
                //f_arRegistration[(int)STATE_REGISTRATION.INI] = new DelegateObjectFunc (registrationINI);
                f_arRegistration[(int)STATE_REGISTRATION.ENV] = new DelegateObjectFunc(registrationEnv);

                s_DataRegistration = new object[(int)INDEX_REGISTRATION.COUNT_INDEX_REGISTRATION];
                s_StateRegistration = new STATE_REGISTRATION[(int)INDEX_REGISTRATION.COUNT_INDEX_REGISTRATION];

                ClearValues();
            } catch (Exception e) {
                Logging.Logg().Exception(e, @"HUsers::HUsers ()...", Logging.INDEX_MESSAGE.NOT_SET);
            }

            Logging.Logg().Debug(@"HUsers::HUsers () - ... очистили значения ...", Logging.INDEX_MESSAGE.NOT_SET);

            for (int i = 0; i < (int)STATE_REGISTRATION.COUNT_STATE_REGISTRATION; i++) {
                argFReg = null;

                if (i == (int)STATE_REGISTRATION.ENV)
                    argFReg = iListenerId;
                else
                    ;

                f_arRegistration[i](argFReg);
            }
        }

        /// <summary>
        /// Прочитать значения параметров профиля полбзователя из БД и разместить их в статические переменные для последующего доступа
        /// </summary>
        /// <param name="iListenerId">Идентификатор подписки к источнику данных</param>
        [Obsolete("Use 'Read' instead. The name is more appropriate than the current one")]
        public static void Update (int iListenerId)
        {
            Read(iListenerId);
        }

        /// <summary>
        /// Прочитать значения параметров профиля полбзователя из БД и разместить их в статические переменные для последующего доступа
        /// </summary>
        /// <param name="iListenerId">Идентификатор подписки к источнику данных</param>
        public static void Read (int iListenerId)
        {
            m_profiles.Read (iListenerId, (int)s_DataRegistration [(int)INDEX_REGISTRATION.ROLE], (int)s_DataRegistration [(int)INDEX_REGISTRATION.ID], false);
        }

        /// <summary>
        /// Очистиь значения регистрации
        /// </summary>
        protected void ClearValues () {
            int i = -1;
            for (i = 0; i < s_DataRegistration.Length; i++)
                s_DataRegistration[i] = null;

            for (i = 0; i < s_StateRegistration.Length; i++)
                s_StateRegistration[i] = STATE_REGISTRATION.UNKNOWN;
        }

        /// <summary>
        /// Метод получения данных регистрации из командной строки (по указанным значениям известных аргументов)
        /// </summary>
        /// <param name="par">Аргументы командной строки</param>
        private void registrationCmdLine(object par)
        {
            Logging.Logg().Debug(@"HUsers::HUsers () - ... registrationCmdLine () - вХод ...", Logging.INDEX_MESSAGE.NOT_SET);

            int i = -1 // счетчики циклов
                , j = -1;
            //Приоритет CMD_LINE
            string[] args = Environment.GetCommandLineArgs();

            //Есть ли параметры в CMD_LINE
            if (args.Length > 1)
            {
                if ((args.Length == 2)
                    && ((args[1].Equals(@"/?") == true)
                        || (args[1].Equals(@"?") == true)
                        || (args[1].Equals(@"/help") == true)
                        || (args[1].Equals(@"-help") == true)
                        || (args[1].Equals(@"help") == true)
                    ))
                { //Выдать сообщение-подсказку...
                    Console.WriteLine(@"Обрабатываются следующие аргументы:");
                    for (i = 1; i < m_Arguments.Length; i++)
                        Console.WriteLine(m_Arguments[i].m_help);
                } else
                    ;

                for (i = 0; i < m_Arguments.Length; i++) {
                    for (j = 1; j < args.Length; j++)
                        if (!(args[j].IndexOf(m_Arguments[i].m_key) < 0)) {
                            //Параметр найден
                            try {
                                switch (m_Arguments[i].m_indxRegistration) {
                                    case INDEX_REGISTRATION.ID:
                                    case INDEX_REGISTRATION.ID_TEC:
                                    case INDEX_REGISTRATION.ROLE:
                                        s_DataRegistration[i] = Convert.ToInt32(args[j].Substring(args[j].IndexOf('=') + 1, args[j].Length - (args[j].IndexOf('=') + 1)));
                                        break;
                                    case INDEX_REGISTRATION.DOMAIN_NAME:
                                        s_DataRegistration[i] = args[j].Substring(args[j].IndexOf('=') + 1, args[j].Length - (args[j].IndexOf('=') + 1));
                                        break;
                                    default:
                                        throw new Exception(string.Format(@"HUsers::registrationCmdLine () - интерпретация параметра {0}...", m_Arguments[i].m_key));
                                        break;
                                }

                                s_StateRegistration[i] = STATE_REGISTRATION.CMD;
                            } catch (Exception e) {
                                Logging.Logg().Exception(e, string.Format(@"HUsers::registrationCmdLine () - интерпретация параметра {0}...", m_Arguments[i].m_key), Logging.INDEX_MESSAGE.NOT_SET);
                            }

                            break;
                        } else {
                        }
                    // проверить найден ли известный для обработки аргумент в командной строке
                    if (!(j < args.Length)) {
                        Logging.Logg().Debug(string.Format(@"HUsers::HUsers () - значение для аргумента {0} в командной строке не задано...", m_Arguments[i].m_key)
                            , Logging.INDEX_MESSAGE.NOT_SET);
                    }
                    else {                        
                    }
                }
            } else {
            }
        }

        ///// <summary>
        ///// Функция запроса для поиска пользователя
        ///// </summary>
        //private void registrationINI(object par)
        //{
        //    Logging.Logg().Debug(@"HUsers::HUsers () - ... registrationINI () - вХод ...", Logging.INDEX_MESSAGE.NOT_SET);

        //    Logging.Logg().Debug(@"HUsers::HUsers () - ... registrationINI () - размер массива параметров INI = " + s_REGISTRATION_INI.Length, Logging.INDEX_MESSAGE.NOT_SET);

        //    //Следующий приоритет INI
        //    if (m_bRegistration == false) {
        //        bool bValINI = false;
        //        for (int i = 1; i < s_DataRegistration.Length; i++)
        //        {
        //            Logging.Logg().Debug(@"HUsers::HUsers () - ... registrationINI () - обработка параметра [" + i + @"]", Logging.INDEX_MESSAGE.NOT_SET);

        //            try
        //            {
        //                if (s_StateRegistration[i] == STATE_REGISTRATION.UNKNOWN)
        //                {
        //                    bValINI = false;
        //                    //Logging.Logg().Debug(@"HUsers::HUsers () - ... registrationINI () - состояние параметра = " + m_StateRegistration[i].ToString());

        //                    //Logging.Logg().Debug(@"HUsers::HUsers () - ... registrationINI () - объект параметра = " + s_REGISTRATION_INI[i].ToString());

        //                    //Logging.Logg().Debug(@"HUsers::HUsers () - ... registrationINI () - тип параметра = " + s_REGISTRATION_INI[i].GetType().Name);

        //                    switch (s_REGISTRATION_INI[i].GetType().Name)
        //                    {
        //                        case @"String":
        //                            bValINI = ((string)s_REGISTRATION_INI[i]).Equals(string.Empty);
        //                            break;
        //                        case @"Int32":
        //                            bValINI = ((Int32)s_REGISTRATION_INI[i]) < 0;
        //                            break;
        //                        default:
        //                            break;
        //                    }

        //                    if (bValINI == false)
        //                    {
        //                        s_DataRegistration[i] = s_REGISTRATION_INI[i];

        //                        s_StateRegistration[i] = STATE_REGISTRATION.INI;
        //                    }
        //                    else
        //                        ;
        //                }
        //                else
        //                    ;
        //            } 
        //            catch (Exception e)
        //            {
        //                Logging.Logg().Exception(e, @"HUsers::HUsers () - ... registrationINI () - параметр не обработан [" + i + @"]", Logging.INDEX_MESSAGE.NOT_SET);
        //            }
        //        }
        //    }
        //    else
        //    {
        //    }
        //}

        /// <summary>
        /// Режим проверки аутентификации
        /// </summary>
        public enum MODE_REGISTRATION {
            /// <summary>
            /// Только по наименование учетной записи в домене
            /// </summary>
            USER_DOMAINNAME,
            /// <summary>
            /// Только по наименованию рабочей станции
            /// </summary>
            MACHINE_DOMAINNAME,
            /// <summary>
            /// Смешанный: и по наименование учетной записи в домене, и по наименованию рабочей станции
            /// </summary>
            MIXED
        }

        /// <summary>
        /// Предусановленный режим аутентификации
        /// </summary>
        private static MODE_REGISTRATION s_modeRegistration = MODE_REGISTRATION.USER_DOMAINNAME;
        /// <summary>
        /// Разделитель наименования рабочей станции и наименования учетной записи в домене при смешанном режиме аутентификации
        /// </summary>
        private const string DELIMETER_DOMAINNAME = @"::";

        /// <summary>
        /// Получить ограничения для запроса перечня пользовтелей
        /// </summary>
        private string whereQueryUsers
        {
            get
            {
                string strRes = string.Empty;
                string[] parts = null;

                if (!(s_StateRegistration[(int)INDEX_REGISTRATION.ID] == STATE_REGISTRATION.UNKNOWN))
                // идентификатор известен
                    strRes = string.Format(@"ID={0}", s_DataRegistration[(int)INDEX_REGISTRATION.ID]);
                else
                // идентификатор не указан, будем выбироать из таблицы зарегистрированных пользователей
                    switch (s_modeRegistration)
                    {
                        case MODE_REGISTRATION.MACHINE_DOMAINNAME:
                            strRes = string.Format(@"COMPUTER_NAME='{0}'", s_DataRegistration[(int)INDEX_REGISTRATION.DOMAIN_NAME]);
                            break;
                        case MODE_REGISTRATION.USER_DOMAINNAME:
                        default:
                            strRes = string.Format(@"DOMAIN_NAME='{0}'", s_DataRegistration[(int)INDEX_REGISTRATION.DOMAIN_NAME]);
                            break;
                        case MODE_REGISTRATION.MIXED:
                            parts = ((string)s_DataRegistration[(int)INDEX_REGISTRATION.DOMAIN_NAME]).Split(new string[] { DELIMETER_DOMAINNAME }, StringSplitOptions.RemoveEmptyEntries);
                            if ((!(parts == null))
                                && (parts.Length == 2))
                                strRes = string.Format(@"COMPUTER_NAME='{0}' AND DOMAIN_NAME='{1}'", parts[0], parts[1]);
                            else
                                ;
                            break;
                    }

                return strRes;
            }
        }

        /// <summary>
        /// Получить доменное наименование пользователя для аутентификации, учитывая установленный режим аутентификации
        /// </summary>
        /// <param name="dbDomainName">Наименование доменной учетной записи пользователя</param>
        /// <param name="dbMashineName">Наименование доменной учетной записи рабочей станции</param>
        /// <returns>Полное наименование  с учетом режим аутентификации</returns>
        private string getUserDomainNameEnvironment (string dbDomainName, string dbMashineName)
        {
            string strRes = string.Empty;

            switch (s_modeRegistration)
            {
                case MODE_REGISTRATION.MIXED:
                    strRes = string.Join(DELIMETER_DOMAINNAME, new string[] { dbMashineName.Trim(), dbDomainName.Trim() });
                    break;
                case MODE_REGISTRATION.USER_DOMAINNAME:
                default:
                    strRes = dbDomainName.Trim();
                    break;
                case MODE_REGISTRATION.MACHINE_DOMAINNAME:
                    strRes = dbMashineName.Trim();
                    break;
            }

            return strRes;
        }

        /// <summary>
        /// Сравнить данные, указанные в аргументе и полученные в процессе регистрации
        /// </summary>
        /// <param name="dbDomainName">Наименование доменной учетной записи пользователя</param>
        /// <param name="dbMashineName">Наименование доменной учетной записи рабочей станции</param>
        /// <returns>Признак результата сравнения</returns>
        private bool equalsDomainName(string dbDomainName, string dbMashineName)
        {
            bool bRes = false;
            string strTesting = getUserDomainNameEnvironment(dbDomainName, dbMashineName);

            bRes = strTesting.Equals((string)s_DataRegistration[(int)INDEX_REGISTRATION.DOMAIN_NAME], StringComparison.CurrentCultureIgnoreCase);

            return bRes;
        }

        /// <summary>
        /// Наименование рабочей станции
        /// </summary>
        public static string MachineName
        {
            get {
                string strRes = string.Empty;
        
                IPAddress[] listAddress = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList;

                //IPAddress.Parse (???)
                foreach (IPAddress address in listAddress) {
                    try { strRes = Dns.GetHostEntry(address).HostName; }
                    catch (Exception e) {; }

                    if (string.IsNullOrEmpty(strRes) == false)
                        break;
                    else
                        ;
                }

                return strRes;
            }
        }
        /// <summary>
        /// Доменные имена пользователя/компьютера
        /// !!! - используется ТОЛЬКО для информации
        /// </summary>
        public static string UserDomainName {
            get {
                string usrDomainName = s_modeRegistration == MODE_REGISTRATION.MIXED ?
                    (string)s_DataRegistration[(int)INDEX_REGISTRATION.DOMAIN_NAME] :
                        MachineName + DELIMETER_DOMAINNAME + (string)s_DataRegistration[(int)INDEX_REGISTRATION.DOMAIN_NAME];

                return s_StateRegistration[(int)INDEX_REGISTRATION.DOMAIN_NAME] == STATE_REGISTRATION.CMD ?
                    string.Format(@"[{0}]={1}", STATE_REGISTRATION.CMD.ToString(), usrDomainName) :
                        usrDomainName;
            }
        }

        /// <summary>
        /// Сообщение при отсутствии пользователя в БД конфигурации
        /// </summary>
        private static string messageUserIDNotFind
        {
            get
            {
                string strRes = string.Empty;

                switch (s_modeRegistration)
                {
                    case MODE_REGISTRATION.MACHINE_DOMAINNAME:
                        strRes = @"Доменное имя рабочей станции";
                        break;
                    case MODE_REGISTRATION.USER_DOMAINNAME:
                    default:
                        strRes = @"Доменное имя пользователя";
                        break;
                    case MODE_REGISTRATION.MIXED:
                        strRes = @"Доменное имя рабочей станции и пользователя";
                        break;
                }

                if (strRes.Equals(string.Empty) == false)
                    strRes += @" не найдено в БД конфигурации";
                else
                    ;

                return strRes;
            }
        }

        /// <summary>
        /// Перечисление - возможные ошибки ???аутентификации
        /// </summary>
        public enum ERROR_CODE : short {
            /// <summary>
            /// Нет соединения с БД конфигурации
            /// </summary>
            NOT_CONNECT_CONFIGDB = -4
            /// <summary>
            /// Учетная запись пользователя не найдена
            /// </summary>
            , UDN_NOT_FOUND
            /// <summary>
            /// Запрос результата аутентификации не выполнен
            /// </summary>
            , QUERY_FAILED
            ,
        }

        /// <summary>
        /// Запуск проверки пользователя 
        /// </summary>
        private void registrationEnv(object par)
        {
            int idListener = (int)par //idListener = ((int [])par)[0]
                , i = -1
                ;

            //Следующий приоритет DataBase
            if (isRegistration == false) {
                Logging.Logg().Debug(string.Format(@"HUsers::HUsers () - ... registrationEnv () - s_StateRegistration [{0}] = {1}", INDEX_REGISTRATION.DOMAIN_NAME, s_StateRegistration[(int)INDEX_REGISTRATION.DOMAIN_NAME].ToString()), Logging.INDEX_MESSAGE.NOT_SET);

                try {
                    if (s_StateRegistration[(int)INDEX_REGISTRATION.ID] == STATE_REGISTRATION.CMD)
                    // идентификатор указан в командной строке
                        ;
                    else
                    // если идентификатор не указан в командной строке
                        if (s_StateRegistration[(int)INDEX_REGISTRATION.DOMAIN_NAME] == STATE_REGISTRATION.UNKNOWN) {                            
                            //Определить из ENV
                            //Проверка ИМЯ_ПОЛЬЗОВАТЕЛЯ
                            switch (s_modeRegistration) {
                                case MODE_REGISTRATION.MACHINE_DOMAINNAME:
                                    s_DataRegistration[(int)INDEX_REGISTRATION.DOMAIN_NAME] = MachineName;
                                    break;                            
                                case MODE_REGISTRATION.USER_DOMAINNAME:
                                default:
                                    s_DataRegistration[(int)INDEX_REGISTRATION.DOMAIN_NAME] = Environment.UserDomainName + @"\" + Environment.UserName;
                                    break;
                                case MODE_REGISTRATION.MIXED:
                                    s_DataRegistration[(int)INDEX_REGISTRATION.DOMAIN_NAME] = MachineName + DELIMETER_DOMAINNAME + Environment.UserDomainName + @"\" + Environment.UserName;
                                    break;
                            }

                            s_StateRegistration[(int)INDEX_REGISTRATION.DOMAIN_NAME] = STATE_REGISTRATION.ENV;

                            Logging.Logg().Debug(string.Format(@"HUsers::HUsers () - ... registrationEnv () - s_StateRegistration [{0}] = {1}", INDEX_REGISTRATION.DOMAIN_NAME, s_StateRegistration[(int)INDEX_REGISTRATION.DOMAIN_NAME]), Logging.INDEX_MESSAGE.NOT_SET);
                        }
                        else {
                        }
                } catch (Exception e) {
                    Logging.Logg().Exception(e, @"HUsers::HUsers () - ... registrationEnv () ... Проверка ИМЯ_ПОЛЬЗОВАТЕЛЯ ... ", Logging.INDEX_MESSAGE.NOT_SET);
                    throw e;
                }

                int err = -1;
                DataTable dataUsers;

                DbConnection connDB = DbSources.Sources().GetConnection((int)par, out err);

                if ((! (connDB == null)) && (err == 0))
                {
                    //Проверка ИМЯ_ПОЛЬЗОВАТЕЛЯ
                    GetUsers(ref connDB, whereQueryUsers, string.Empty, out dataUsers, out err);

                    Logging.Logg().Debug(@"HUsers::HUsers () - ... registrationEnv () - найдено пользователей = " + dataUsers.Rows.Count, Logging.INDEX_MESSAGE.NOT_SET);

                    if ((err == 0) && (dataUsers.Rows.Count > 0))
                    {//Найдена хотя бы одна строка
                        i = 0;

                        if ((s_StateRegistration[(int)INDEX_REGISTRATION.ID] == STATE_REGISTRATION.UNKNOWN)
                            || (s_StateRegistration[(int)INDEX_REGISTRATION.DOMAIN_NAME] == STATE_REGISTRATION.ENV))
                            for (i = 0; i < dataUsers.Rows.Count; i++)
                            {
                                //Проверка IP-адрес
                                //for (indxIP = 0; indxIP < listIP.Length; indxIP ++) {
                                //    if (listIP[indxIP].Equals(System.Net.IPAddress.Parse (dataUsers.Rows[i][@"IP"].ToString())) == true) {
                                //        //IP найден
                                //        break;
                                //    }
                                //    else
                                //        ;
                                //}

                                //Проверка ИМЯ_ПОЛЬЗОВАТЕЛЯ
                                if (equalsDomainName(dataUsers.Rows[i][@"DOMAIN_NAME"].ToString(), dataUsers.Rows[i][@"COMPUTER_NAME"].ToString()) == true) break; else;
                            }
                        else
                            setDataRegistration(INDEX_REGISTRATION.DOMAIN_NAME, getUserDomainNameEnvironment(dataUsers.Rows[i][@"DOMAIN_NAME"].ToString(), dataUsers.Rows[i][@"COMPUTER_NAME"].ToString()), STATE_REGISTRATION.CMD);

                        if (i < dataUsers.Rows.Count)
                        {
                            setDataRegistration(INDEX_REGISTRATION.ID, dataUsers.Rows[i]["ID"], STATE_REGISTRATION.ENV);
                            setDataRegistration(INDEX_REGISTRATION.ROLE, dataUsers.Rows[i]["ID_ROLE"], STATE_REGISTRATION.ENV);
                            setDataRegistration(INDEX_REGISTRATION.ID_TEC, dataUsers.Rows[i]["ID_TEC"], STATE_REGISTRATION.ENV);                            
                        }
                        else
                            throw new HException((int)ERROR_CODE.UDN_NOT_FOUND, messageUserIDNotFind);
                    }
                    else
                    {//Не найдено ни одной строки
                        if (connDB == null)
                            throw new HException((int)ERROR_CODE.NOT_CONNECT_CONFIGDB, "Нет соединения с БД конфигурации");
                        else
                            if (err == 0)
                                throw new HException((int)ERROR_CODE.UDN_NOT_FOUND, messageUserIDNotFind);
                            else
                                throw new HException((int)ERROR_CODE.QUERY_FAILED, "Ошибка получения списка пользователей из БД конфигурации");
                    }
                } else {//Нет возможности для проверки
                    if (connDB == null)
                        throw new HException((int)ERROR_CODE.NOT_CONNECT_CONFIGDB, "Нет соединения с БД конфигурации");
                    else //???
                        if (! (err == 0))
                            throw new HException((int)ERROR_CODE.NOT_CONNECT_CONFIGDB, "Нет соединения с БД конфигурации");
                        else
                            ;
                }
            }
            else {
                Logging.Logg().Debug(string.Format(@"HUsers::HUsers () - ... registrationEnv () - isRegistration = {0}", isRegistration.ToString()), Logging.INDEX_MESSAGE.NOT_SET);
            }

            try {
                m_profiles = createProfiles (idListener, (int)s_DataRegistration[(int)INDEX_REGISTRATION.ROLE], (int)s_DataRegistration[(int)INDEX_REGISTRATION.ID]);
            } catch (Exception e) {
                throw new HException(-6, e.Message);
            }
        }

        private static void setDataRegistration(INDEX_REGISTRATION indxReg, object value, STATE_REGISTRATION newState)
        {
            int iReg = (int)indxReg;

            if (s_StateRegistration[(int)indxReg] == STATE_REGISTRATION.UNKNOWN) {
                if (value.GetType().IsPrimitive == true)
                    s_DataRegistration[(int)indxReg] = Convert.ToInt32 (value);
                else
                    s_DataRegistration[(int)indxReg] = value;
                s_StateRegistration[(int)indxReg] = newState;
            } else
                ;
        }

        //protected abstract void Registration (DataRow rowUser)  { }

        protected void Initialize (string addingMsg) {
            string strMes = string.Empty
                , strListIP = string.Empty;

            System.Net.IPAddress[] listIP = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList;
            int indxIP = -1;
            for (indxIP = 0; indxIP < listIP.Length; indxIP ++) {
                strListIP += @", ip[" + indxIP + @"]=" + listIP[indxIP].ToString ();
            }            

            strMes = string.Format(@"Пользователь= {0}, (id={1}), id_tec={2}, {3}, {4}; Version(Дата/время)={5}"
                , DomainName, Id, allTEC, addingMsg, strListIP, ProgramBase.AppProductVersion);

            Logging.Logg().Action(strMes, Logging.INDEX_MESSAGE.NOT_SET);
        }

        /// <summary>
        /// Функция получения строки запроса пользователя
        ///  /// <returns>Строка строку запроса</returns>
        /// </summary>
        private static string getUsersRequest(string where, string orderby)
        {
            string strQuery = string.Empty;
            //strQuer//strQuery =  "SELECT * FROM users WHERE DOMAIN_NAME='" + Environment.UserDomainName + "\\" + Environment.UserName + "'";
            //strQuery =  "SELECT * FROM users WHERE DOMAIN_NAME='NE\\ChrjapinAN'";
            strQuery = "SELECT * FROM users";
            if ((!(where == null)) && (where.Length > 0))
                strQuery += " WHERE " + where;
            else
                ;

            if ((!(orderby == null)) && (orderby.Length > 0))
                strQuery += " ORDER BY " + orderby;
            else
                ;

            return strQuery;
        }

        /*public void GetUsers(string where, string orderby, out DataTable users, out int err)
        {
            err = 0;
            users = new DataTable ();

            users = DbTSQLInterface.Select(connSettConfigDB, getUsersRequest(where, orderby), out err);
        }

        public static void GetUsers(ConnectionSettings connSett, string where, string orderby, out DataTable users, out int err)
        {
            err = 0;
            users = new DataTable();

            users = DbTSQLInterface.Select(connSett, getUsersRequest(where, orderby), out err);
        }*/

        /// <summary>
        /// Функция запроса для поиска пользователя
        /// </summary>
        public static void GetUsers(ref DbConnection conn, string where, string orderby, out DataTable users, out int err)
        {
            err = 0;
            users = null;
            
            if (! (conn == null)) {
                users = new DataTable();
                Logging.Logg().Debug(@"HUsers::GetUsers () - запрос для поиска пользователей = [" + getUsersRequest(where, orderby) + @"]", Logging.INDEX_MESSAGE.NOT_SET);
                users = DbTSQLInterface.Select(ref conn, getUsersRequest(where, orderby), null, null, out err);
            } else {
                err = -1;
            }
        }

        /// <summary>
        /// Возвратить роли(группы польтзователей, объединеных по функц./признаку) из БД
        /// </summary>
        public static void GetRoles(ref DbConnection conn, string where, string orderby, out DataTable roles, out int err)
        {
            err = 0;
            roles = null;
            string query = string.Empty;

            if (! (conn == null))
            {
                roles = new DataTable();
                query = @"SELECT * FROM ROLES";

                if ((where.Equals(null) == true) || (where.Equals(string.Empty) == true))
                    query += @" WHERE ID < 500";
                else
                    query += @" WHERE " + where;

                roles = DbTSQLInterface.Select(ref conn, query, null, null, out err);
            }
            else
            {
                err = -1;
            }
        }

        /// <summary>
        /// Возвратить ИД пользователя
        /// </summary>
        public static int Id
        {
            get
            {
                return (s_DataRegistration == null) ? 0 : ((!((int)INDEX_REGISTRATION.ID_TEC < s_DataRegistration.Length)) || (s_DataRegistration[(int)INDEX_REGISTRATION.ID] == null)) ? 0 : (int)s_DataRegistration[(int)INDEX_REGISTRATION.ID];
            }
        }

        /// <summary>
        /// Возвратить доменное имя
        /// </summary>
        public static string DomainName
        {
            get
            {
                return (string)s_DataRegistration[(int)INDEX_REGISTRATION.DOMAIN_NAME];
            }
        }

        /// <summary>
        /// Возвращает ИД ТЭЦ  для текущего пользователя
        ///  , признак прав доступа к значениям подразделений
        ///  (0 - разрешены все ТЭЦ)
        /// </summary>
        public static int allTEC
        {
            get
            {
                return (s_DataRegistration == null) ? 0 : ((!((int)INDEX_REGISTRATION.ID_TEC < s_DataRegistration.Length)) || (s_DataRegistration[(int)INDEX_REGISTRATION.ID_TEC] == null)) ? 0 : (int)s_DataRegistration[(int)INDEX_REGISTRATION.ID_TEC];
            }
        }

        /// <summary>
        /// Возвратить значение параметра профиля для текущего пользователя, если его тип логический
        /// </summary>
        /// <param name="id">Идентификатор параметра профиля</param>
        /// <returns>Значение параметра для текущего пользователя</returns>
        public static bool IsAllowed (int id) { return bool.Parse(GetAllowed (id)); }

        /// <summary>
        /// Возвратить таблицу со описанием параметров профиля
        /// </summary>
        public static DataTable GetTableProfileUnits { get { return HProfiles.GetTableUnits; } }

        /// <summary>
        /// Возвратить значение параметра для текущего пользователя, если его тип отличен от логического
        /// </summary>
        /// <param name="id">Идентификатор параметра профиля</param>
        /// <returns>Значение параметра для текущего пользователя</returns>
        public static string GetAllowed(int id) { return (string)HProfiles.GetAllowed(id); }

        /// <summary>
        /// Возвратить значение параметра для указанного в аргументе пользователя, если его тип отличен от логического
        /// </summary>
        /// <param name="dbConn">Ссылка на объект соединения с БД конфигурации</param>
        /// <param name="role">Идентификатор роли(группы), к которой принадлежит пользователь</param>
        /// <param name="user">Идентификатор пользователя</param>
        /// <param name="id">Идентификатор параметра профиля</param>
        /// <returns>Значение параметра для текущего пользователя</returns>
        public static string GetAllowed(ref DbConnection dbConn, int role, int user,int id)
        {
            return (string)HProfiles.GetAllowed(ref dbConn, role, user, id);
        }

        /// <summary>
        /// Возвратить значение параметра для указанного в аргументе пользователя, если его тип отличен от логического
        /// </summary>
        /// <param name="iListenerId">Идентификатор подписчика объекта обращения к данным</param>
        /// <param name="role">Идентификатор роли(группы), к которой принадлежит пользователь</param>
        /// <param name="user">Идентификатор пользователя</param>
        /// <param name="id">Идентификатор параметра профиля</param>
        /// <returns>Значение параметра для текущего пользователя</returns>
        public static string GetAllowed (int iListenerId, int role, int user, int id)
        {
            int err = -1;

            DbConnection dbConn = null;

            dbConn = DbSources.Sources ().GetConnection (iListenerId, out err);

            return err == 0
                ? (string)HProfiles.GetAllowed (ref dbConn, role, user, id)
                    : string.Empty;
        }

        /// <summary>
        /// Установить значение для параметра профиля текущего пользователя
        /// </summary>
        /// <param name="iListenerId">Идентификатор подпичсика </param>
        /// <param name="id">Идентификатор параметра профиля</param>
        /// <param name="val">Новое значение для параметра</param>
        public static void SetAllowed(int iListenerId, int id, string val)
        {
            HProfiles.SetAllowed(iListenerId, id, val);
        }
    }
}
