using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.IO;

namespace HClassLibrary
{
    public class FileINI
    {
        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WritePrivateProfileString(String Section, String Key, String Value, String FilePath);
        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.I4)]
        public static extern int GetPrivateProfileString(String Section, String Key, String Default, StringBuilder retVal, int Size, String FilePath);

        /// <summary>
        /// Перечисление для индексирования символов-разделителей
        /// </summary>
        public enum INDEX_DELIMETER { SEC_PART_APP, SEC_PART_TARGET, PAIR, VALUES, PAIR_VAL, VALUE };
        /// <summary>
        /// Символы-разделители
        /// </summary>
        public static char [] s_chSecDelimeters = {' ', '-', '=', ';', ',', ':'};
        /// <summary>
        /// Наименовние файла конфигурации
        /// </summary>
        public string m_NameFileINI = string.Empty;
        /// <summary>
        /// Словарь со всеми значениями из файла конфигурации
        /// </summary>
        private Dictionary<string, Dictionary<string, string>> m_values;
        /// <summary>
        /// Наименование (краткое) главной секции файла конфигурации
        /// </summary>
        private string SEC_SHR_MAIN
        {
            get { return "Main"; }
        }
        /// <summary>
        /// Часть секции - признак принадлежности текущему приложению
        /// </summary>
        private string SEC_APP
        {
            get { return "(" + ProgramBase.AppName + ")"; }
        }
        /// <summary>
        /// Наименование главной секции файла конфигурации
        /// </summary>
        private string SEC_MAIN
        {
            get { return SEC_SHR_MAIN + s_chSecDelimeters[(int)INDEX_DELIMETER.SEC_PART_APP] + SEC_APP; }
        }
        /// <summary>
        /// Проверить наличие главной секции
        /// </summary>
        protected bool isMainSec
        {
            get { return isSec(SEC_SHR_MAIN); }
        }
        /// <summary>
        /// Проверить наличие секции
        /// </summary>
        /// <param name="sec_shr">Нименвание секции (краткое)</param>
        /// <returns>Признак наличия секции</returns>
        protected bool isSec (string sec_shr)
        {
            //Logging.Logg().Debug(@"FileINI::isSec (sec_shr=" + sec_shr + @") - ...", Logging.INDEX_MESSAGE.NOT_SET);
            //Logging.Logg().Debug(@"FileINI::isSec () - SEC_APP = " + SEC_APP + @"...", Logging.INDEX_MESSAGE.NOT_SET);
            return m_values.ContainsKey(sec_shr + s_chSecDelimeters[(int)INDEX_DELIMETER.SEC_PART_APP] + SEC_APP);
        }
        /// <summary>
        /// Проверка принадлежности секции текущему приложению
        /// </summary>
        /// <param name="sec">Наименование секции</param>
        /// <returns>Признак принадлежности</returns>
        private bool isSecApp (string sec)
        {
            bool bRes = false;
            string secApp = string.Empty;

            try {
                //Получить часть секции - признак приложения
                secApp = sec.Split(new char[] { s_chSecDelimeters[(int)INDEX_DELIMETER.SEC_PART_APP] }, StringSplitOptions.None)[1];
                //Получить результат при сравнении
                bRes = secApp.Equals (SEC_APP);
            }
            catch (Exception e)
            {
                Logging.Logg().Exception(e, Logging.INDEX_MESSAGE.NOT_SET, @"FileINI::isSecApp() - ошибка разбора секции...");
            }

            return bRes;
        }
        /// <summary>
        /// Проверка наличия ключа в главной секции
        /// </summary>
        /// <param name="key">Ключ для проверки</param>
        /// <returns>Признак наличия ключа</returns>
        protected bool isMainSecKey(string key)
        {
            return isSecKey(SEC_SHR_MAIN, key);
        }
        /// <summary>
        /// Проверка наличия ключа в секции
        /// </summary>
        /// <param name="sec_shr">Часть "смысловая" наименования секции</param>
        /// <param name="key">Ключ для проверки</param>
        /// <returns>Признак наличия ключа</returns>
        protected bool isSecKey(string sec_shr, string key)
        {
            return isSec(sec_shr) == true ? m_values[sec_shr + s_chSecDelimeters[(int)INDEX_DELIMETER.SEC_PART_APP] + SEC_APP].ContainsKey(key) : false;
        }
        /// <summary>
        /// Конструктор - основной
        /// </summary>
        /// <param name="nameFile">Имя файла конфигурации</param>
        /// <param name="bReadAuto">Признак автоматического считывания всех параметров при создании</param>
        public FileINI(string nameFile, bool bReadAuto)
        {
            m_values = new Dictionary<string, Dictionary<string, string>>();

            m_NameFileINI = System.Environment.CurrentDirectory + "\\" + nameFile;
            if (File.Exists(m_NameFileINI) == false)
            {
                File.Create(m_NameFileINI);
                //throw new Exception ("Не удалось найти файл инициализации (полный путь: " + m_NameFileINI + ")");
            }
            else
                //Проверить необходимость автоматического заполнения словаря
                if (bReadAuto == true)
                {
                    string sec = string.Empty
                        , sec_shr = string.Empty;

                    //Прочитать все строки ??? ProgramBase.ss_MainCultureInfo
                    //EncodingInfo[]arEncInfo = Encoding.GetEncodings();
                    string[] lines = System.IO.File.ReadAllLines(m_NameFileINI, Encoding.GetEncoding(1251));

                    try
                    {
                        //Разобрать по-строчно
                        foreach (string line in lines)
                        {
                            //Не обрабатывать "пустые" строки
                            if (line.Length == 0)
                                continue;
                            else
                                ;

                            //Logging.Logg().Debug(@"FileINI::ctor () - строка: " + line, Logging.INDEX_MESSAGE.NOT_SET);

                            bool bSec = line[0] == '[';
                            //Не обрабатывать строки, начинающиеся не с "буквы"
                            if (Char.IsLetter(line[0]) == false)
                                //Строки, начинающиеся с '[' - обрабатывать
                                if (bSec == false)
                                    continue;
                                else
                                    ;
                            else
                                ;

                            //Проверить признак секции
                            if (bSec == true)
                            {
                                //Получить наименование секции
                                sec = line.Substring (1, line.Length - 2);
                                //Проверить принадлежность к текущему приложению
                                if (isSecApp (sec) == true)
                                {
                                    //sec_shr = sec.Split(new char[] { s_chSecDelimeters[(int)INDEX_DELIMETER.SEC_PART] }, StringSplitOptions.None)[0];
                                    //Проверить наличие секции в словаре
                                    //if (m_values.ContainsKey (sec_shr) == false)
                                    if (m_values.ContainsKey(sec) == false)
                                    {
                                        //Добавить секцию
                                        //m_values.Add (sec_shr, new Dictionary<string,string> ());
                                        m_values.Add(sec, new Dictionary<string, string>());
                                    }
                                    else
                                        ;
                                }
                                else
                                {
                                    //Очистить секцию для предотвращения обработки параметров (внутри секции)
                                    sec =
                                    sec_shr =
                                        string.Empty;
                                }
                            }
                            else
                            {//Обработка параметра (ключ=значение)
                                //Проверить наличие секции
                                if ((sec.Equals (string.Empty) == false)
                                    || (sec_shr.Equals (string.Empty) == false)
                                )
                                {
                                    //Проверить наличие секции в словаре
                                    //if (m_values.ContainsKey (sec_shr) == true)
                                    if (m_values.ContainsKey(sec) == true)
                                    {
                                        string[] pair = line.Split(s_chSecDelimeters[(int)INDEX_DELIMETER.PAIR]);
                                        if (pair.Length == 2)
                                            //Добавить параметр для секции
                                            //m_values[sec_shr].Add (pair[0], pair[1]);
                                            m_values[sec].Add(pair[0], pair[1]);
                                        else
                                            throw new Exception(@"FileINI::ctor () - ...");
                                    }
                                    else
                                        throw new Exception (@"FileINI::ctor () - ...");
                                }
                                else
                                    ;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Logging.Logg ().Exception (e, Logging.INDEX_MESSAGE.NOT_SET, @"FileINI::ctor () - ...");
                    }
                }
                else
                    ;
        }        
        /// <summary>
        /// Получить значение из главной секции по ключу
        /// </summary>
        /// <param name="key">Ключ в главной секции</param>
        /// <returns>Значение параметра по ключу</returns>
        public string GetMainValueOfKey(string key) {
            return GetSecValueOfKey(SEC_SHR_MAIN, key);
        }

        public void SetSecValueOfKey(string sec_shr, string key, string val)
        {
            string sec = sec_shr + s_chSecDelimeters[(int)INDEX_DELIMETER.SEC_PART_APP] + SEC_APP;

            WriteString (sec, key, val);
        }

        /// <summary>
        /// Получить значение из указанной секции по ключу
        /// </summary>
        /// <param name="sec_shr">Секция в кторой размещен парметр с ключом</param>
        /// <param name="key">Ключ для получения значения</param>
        /// <returns>Значение параметра по ключу</returns>
        public string GetSecValueOfKey(string sec_shr, string key)
        {
            string sec = sec_shr + s_chSecDelimeters[(int)INDEX_DELIMETER.SEC_PART_APP] + SEC_APP;
            //Logging.Logg().Debug(@"FileINI::GetSecValueOfKey (sec_shr=" + sec_shr + @", key=" + key + @") - sec=" + sec + @"...", Logging.INDEX_MESSAGE.NOT_SET);
            //Logging.Logg().Debug(@"FileINI::GetSecValueOfKey () - isSec (sec_shr)=" + isSec(sec_shr).ToString() + @"...", Logging.INDEX_MESSAGE.NOT_SET);
            return isSec (sec_shr) == true ? m_values[sec].ContainsKey(key) == true ? m_values[sec][key] : string.Empty : string.Empty;
        }
        protected Dictionary<string, string> getSecValues(string sec_shr)
        {
            string sec = sec_shr + s_chSecDelimeters[(int)INDEX_DELIMETER.SEC_PART_APP] + SEC_APP;
            return isSec(sec_shr) == true ? m_values[sec] : null;
        }
        /// <summary>
        /// Конструктор - дополн. (при создании добавляет в словарь указаныые параметры ключ-значение)
        /// </summary>
        /// <param name="nameFile">Наименование файла</param>
        /// <param name="bReadAuto">Признак автоматического считывания всех параметров при создании</param>
        /// <param name="par">Массив ключей параметров</param>
        /// <param name="val">Массив значений параметров</param>
        public FileINI(string nameFile, bool bReadAuto, string[] par, string[] val) : this (nameFile, bReadAuto)
        {
            string key = string.Empty;

            if (par.Length == val.Length) {
                for (int i = 0; i < par.Length; i ++)
                    AddMainPar (par[i], val[i]);
            }
            else
                throw new Exception (@"FileINI::с параметрами...");
        }

        public void AddMainPar(string par, string val)
        {
            AddSecPar(SEC_SHR_MAIN, par, val);
        }

        public void AddSecPar (string sec_shr, string par, string val) {
            string sec = sec_shr +  @" (" + ProgramBase.AppName + ")";

            if (m_values.ContainsKey (sec) == false)
                m_values.Add (sec, new Dictionary<string,string> ());
            else
                ;    
            if (m_values[sec].ContainsKey(par) == false)
            {
                m_values [sec].Add(par, ReadString(sec, par, string.Empty));
                if (m_values[sec][par].Equals(string.Empty) == true)
                {
                    m_values [sec][par] = val;
                    WriteString(sec, par, val);
                }
                else
                    ;
            }
            else
                ; //Такой параметр уже есть
            
        }

        public String ReadString(String Section, String Key, String Default)
        {
            StringBuilder StrBu = new StringBuilder(255);
            GetPrivateProfileString(Section, Key, Default, StrBu, 255, m_NameFileINI);
            return StrBu.ToString();
        }

        public int ReadInt(String Section, String Key, int Default)
        {
            int value;
            string s;
            s = ReadString(Section, Key, "");
            if (s == "")
                value = Default;
            else
                if (!int.TryParse(s, out value))
                    value = Default;
            return value;
        }

        public void WriteString(String Section, String Key, String Value)
        {
            bool bRes = WritePrivateProfileString(Section, Key, Value, m_NameFileINI);

            Logging.Logg().Debug(@"FileINI::WriteString (sec=" + Section + @", key=" + Key + @") - val=" + Value + @", в файл=" + m_NameFileINI + @" [res=" + bRes.ToString () + @"] - ...", Logging.INDEX_MESSAGE.NOT_SET);
        }

        public void WriteInt(String Section, String Key, int Value)
        {
            string s = Value.ToString();
            WriteString(Section, Key, s);
        }
    }
}