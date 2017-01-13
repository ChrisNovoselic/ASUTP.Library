using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.IO;

using System.Xml;
using System.Reflection;

namespace HClassLibrary
{
    public class FileINI
    {
        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WritePrivateProfileString(String Section, String Key, String Value, String FilePath);
        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.I4)]
        public static extern int GetPrivateProfileString(String Section, String Key, String Default, StringBuilder retVal, int Size, String FilePath);
        /// <summary>
        /// Перечисление - режим обработки принадлежности секций
        ///  при UNNAMED наименование исполняемого файла игнорируется
        ///  при INSTANCE (режим по умолчанию) используется собственное наименование приложения
        ///  при CUSTOM д.б. указано наименование исполняемого файла в качестве принадлежности секции
        /// </summary>
        public enum MODE_SECTION_APPLICATION : short { UNNAMED = -1, INSTANCE, CUSTOM }
        /// <summary>
        /// Режим обработки принадлежности секции для текущего экземпляра объекта
        /// </summary>
        private MODE_SECTION_APPLICATION _modeSectionApp;
        /// <summary>
        /// Принадлежность секции в режиме 'CUSTOM'
        /// </summary>
        private string m_strSecAppCustom;
        /// <summary>
        /// Перечисление - известный формат файлов конфигурации
        /// </summary>
        private enum TYPE {UNKNOWN = -1, INI, XML};
        /// <summary>
        /// Формат обрабатываемого файоа конфигурации текущим экземпляром
        /// </summary>
        private TYPE _type;
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
            get {
                return _modeSectionApp == MODE_SECTION_APPLICATION.INSTANCE ?
                    "(" + ProgramBase.AppName + ")" :
                    _modeSectionApp == MODE_SECTION_APPLICATION.UNNAMED ? string.Empty :
                    _modeSectionApp == MODE_SECTION_APPLICATION.CUSTOM ?
                        m_strSecAppCustom : string.Empty;
            }
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
                secApp = _modeSectionApp == MODE_SECTION_APPLICATION.INSTANCE ? sec.Split(new char[] { s_chSecDelimeters[(int)INDEX_DELIMETER.SEC_PART_APP] }, StringSplitOptions.None)[1] :
                    _modeSectionApp == MODE_SECTION_APPLICATION.UNNAMED ? string.Empty :
                    _modeSectionApp == MODE_SECTION_APPLICATION.CUSTOM ?
                        m_strSecAppCustom : string.Empty;
                //Получить результат при сравнении
                bRes = secApp.Equals (SEC_APP);
            } catch (Exception e) {
                Logging.Logg().Exception(e, @"FileINI::isSecApp() - ошибка разбора секции...", Logging.INDEX_MESSAGE.NOT_SET);
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

        private void setType()
        {
            _type = TYPE.UNKNOWN;

            string[] lines = System.IO.File.ReadAllLines(m_NameFileINI, Encoding.GetEncoding(1251));

            if (lines.Length > 1) {
                switch (lines[0].Replace('<', ' ').Replace('?', ' ').Replace('[', ' ').Trim().Split(' ')[0]) {
                    case @"Main":
                        _type = TYPE.INI;
                        break;
                    case @"xml":
                        _type = TYPE.XML;
                        break;
                    default:
                        break;
                }
            } else
                ;
        }

        /// <summary>
        /// Конструктор - основной
        /// </summary>
        /// <param name="nameFile">Имя файла конфигурации</param>
        /// <param name="bReadAuto">Признак автоматического считывания всех параметров при создании</param>
        public FileINI(string nameFile, bool bReadAuto, MODE_SECTION_APPLICATION mode = MODE_SECTION_APPLICATION.INSTANCE)
        {
            string msgErr = string.Empty;

            _modeSectionApp = mode;
            m_strSecAppCustom = string.Empty;

            m_NameFileINI = System.Environment.CurrentDirectory + "\\" + nameFile;
            if (!(File.Exists(m_NameFileINI) == false)) {
                setType();

                m_values = new Dictionary<string, Dictionary<string, string>>();

                switch (_type) {
                    case TYPE.XML:
                        setDictFromXML(bReadAuto);
                        break;
                    case TYPE.INI:
                        setDictFromINI(bReadAuto);
                        break;
                    default:
                        msgErr = string.Format("FileINI::ctor () - путь_к_файлу={0}, режим={1}...", m_NameFileINI, mode.ToString());
                        Logging.Logg().Error(msgErr
                            , Logging.INDEX_MESSAGE.NOT_SET);
                        new Exception(msgErr);
                        break;
                }
            } else {
                Logging.Logg().Error("Не найден файл настроек", Logging.INDEX_MESSAGE.NOT_SET);
            }
        }

        private string getSecName(string secName)
        {
            string strRes = secName;

            string strReplacement = string.Empty;
            int indxStart = strRes.IndexOf('(')
                , indxEnd = strRes.IndexOf(')');

            if ((!(indxStart < 0))
                && (!(indxEnd < 0))
                && (indxEnd - indxStart > @".exe".Length))
                switch (_modeSectionApp) {
                    case MODE_SECTION_APPLICATION.UNNAMED:
                    // вырезать принадлежность
                        strRes = strRes.Replace(strRes.Substring(indxStart, indxEnd - indxStart + 1), string.Empty);
                        break;
                    case MODE_SECTION_APPLICATION.CUSTOM:
                    // заменить принадлежность
                        strRes = strRes.Replace(strRes.Substring(indxStart, indxEnd - indxStart + 1), string.Format(@"{0}{2}{1}", @"(", @")", m_strSecAppCustom));
                        break;
                    case MODE_SECTION_APPLICATION.INSTANCE:
                    default:
                        break;
                }
            else
                throw new Exception (string.Format(@"FileINI::getSecName () - не найдена принадлежность, секция={0}", secName));

            return strRes;
        }

        private void setDictFromINI(bool bReadAuto)
        {
            string[] lines = null
            //Пара ключ-значение
                , pair = null;
            //Признак - строка начало секции
            bool bSec = false
            //Признак - следующая строка - продолжение текущей
                , bNewLine = true;

            //Проверить необходимость автоматического заполнения словаря
            if (bReadAuto == true) {
                string sec = string.Empty
                    , sec_shr = string.Empty;

                //Прочитать все строки ??? ProgramBase.ss_MainCultureInfo
                //EncodingInfo[]arEncInfo = Encoding.GetEncodings();
                lines = System.IO.File.ReadAllLines(m_NameFileINI, Encoding.GetEncoding(1251));
                
                try {
                    //Разобрать по-строчно
                    foreach (string line in lines) {
                        //Не обрабатывать "пустые" строки
                        if (line.Length == 0)
                            continue;
                        else
                            ;

                        //Logging.Logg().Debug(@"FileINI::ctor () - строка: " + line, Logging.INDEX_MESSAGE.NOT_SET);

                        bSec = line[0] == '[';
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
                        if (bSec == true) {
                            //Получить наименование секции
                            sec = line.Substring(1, line.Length - 2);
                            sec = getSecName(sec);
                            //Проверить принадлежность к текущему приложению
                            if (isSecApp(sec) == true) {
                                //sec_shr = sec.Split(new char[] { s_chSecDelimeters[(int)INDEX_DELIMETER.SEC_PART] }, StringSplitOptions.None)[0];
                                //Проверить наличие секции в словаре
                                //if (values.ContainsKey (sec_shr) == false)
                                if (m_values.ContainsKey(sec) == false) {
                                    //Добавить секцию
                                    //values.Add (sec_shr, new Dictionary<string,string> ());
                                    m_values.Add(sec, new Dictionary<string, string>());
                                }
                                else
                                    ;
                            } else {
                                //Очистить секцию для предотвращения обработки параметров (внутри секции)
                                sec =
                                sec_shr =
                                    string.Empty;
                            }
                        } else {
                        //Обработка параметра (ключ=значение)
                            //Проверить наличие секции
                            if ((sec.Equals(string.Empty) == false)
                                || (sec_shr.Equals(string.Empty) == false)
                            )
                            {
                                //Проверить наличие секции в словаре
                                //if (values.ContainsKey (sec_shr) == true)
                                if (m_values.ContainsKey(sec) == true) {
                                    if (bNewLine == true) {
                                        ////Вариант №1
                                        //pair = line.Split(s_chSecDelimeters[(int)INDEX_DELIMETER.PAIR]);
                                        //Вариант №2
                                        pair = new string[2];
                                        int indxPair = line.IndexOf(s_chSecDelimeters[(int)INDEX_DELIMETER.PAIR]);
                                        pair[0] = line.Substring(0, indxPair);
                                        pair[1] = line.Substring(indxPair + 1, line.Length - indxPair - 1);

                                        if (!(pair[1].IndexOf(s_chSecDelimeters[(int)INDEX_DELIMETER.PAIR]) < 0))
                                            Logging.Logg().Warning(@"FileINI::ctor () - в строке [" + line + @"] используются зарезервированные символы: '" + s_chSecDelimeters[(int)INDEX_DELIMETER.PAIR] + @"'", Logging.INDEX_MESSAGE.NOT_SET);
                                        else
                                            ;

                                        if (pair.Length == 2)
                                        //Добавить параметр для секции
                                            //values[sec_shr].Add (pair[0], pair[1]);
                                            m_values[sec].Add(pair[0], pair[1]);
                                        else
                                            throw new Exception(@"FileINI::ctor () - ...");
                                    } else {
                                        pair[1] += line;
                                    }

                                    bNewLine = !line[line.Length - 1].Equals('_');

                                    if (bNewLine == false)
                                        m_values[sec][pair[0]] = m_values[sec][pair[0]].Substring(0, m_values[sec][pair[0]].LastIndexOf(' '));
                                    else
                                        ;
                                } else
                                    throw new Exception(@"FileINI::ctor () - ...");
                            } else
                                ;
                        }
                    }
                } catch (Exception e) {
                    Logging.Logg().Exception(e, @"FileINI::ctor () - ...", Logging.INDEX_MESSAGE.NOT_SET);
                }
            } else
                ;
        }

        private void setDictFromXML(bool bReadAuto)
        {
            XmlDocument setup = new XmlDocument();

            try {
                setup = new XmlDocument();
                setup.Load(m_NameFileINI);
                foreach (XmlNode nodes in setup["SETUP"])
                {
                    if (nodes.FirstChild != null)
                    {
                        if (nodes.Attributes.Count > 0)
                            foreach(XmlAttribute atrib in nodes.Attributes)
                                if (atrib.Value == Assembly.GetEntryAssembly().FullName.Split(',')[0] + ".exe")
                                {
                                    foreach (XmlNode node in nodes)
                                    {
                                        if (node.Attributes.Count > 0)
                                        {
                                            Dictionary<string, string> dict = new Dictionary<string, string>();

                                            foreach (XmlAttribute atr in node.Attributes)
                                            {
                                                if (atr.Name != "add")
                                                    dict.Add(atr.Name, atr.Value);
                                            }

                                            m_values.Add(node.Name + " (" + atrib.Value + ")", dict);
                                        }
                                    }
                                }
                    }
                }
            } catch (Exception e) {
                Logging.Logg().Exception(e, e.Message, Logging.INDEX_MESSAGE.NOT_SET);
            }
        }

        /// <summary>
        /// Получить значение из главной секции по ключу
        /// </summary>
        /// <param name="key">Ключ в главной секции</param>
        /// <returns>Значение параметра по ключу</returns>
        public string GetMainValueOfKey(string key) {
            return GetSecValueOfKey(SEC_SHR_MAIN, key);
        }
        /// <summary>
        /// Установить значение по ключу в главной секции
        /// </summary>
        /// <param name="key">Ключ для сохранения значения</param>
        /// <param name="val">Значение для сохранения</param>
        public void SetMainValueOfKey(string key, string val)
        {
            //WriteString(SEC_MAIN, key, val);
            SetSecValueOfKey (SEC_SHR_MAIN, key, val);
        }
        /// <summary>
        /// Установить значение по ключу в указанной секции
        /// </summary>
        /// <param name="sec_shr">Краткое наименование секции</param>
        /// <param name="key">Ключ для сохранения значения</param>
        /// <param name="val">Значение для сохранения</param>
        public void SetSecValueOfKey(string sec_shr, string key, string val)
        {
            //Полное наименование секции в файле конфигурации
            string sec = sec_shr + s_chSecDelimeters[(int)INDEX_DELIMETER.SEC_PART_APP] + SEC_APP;
            //Проверить наличие секции (по словарю считанных значений)
            if (isSec(sec_shr) == false)
                //Если нет - добавить
                m_values.Add (sec, new Dictionary<string,string> ());
            else
                ;
            //Проверить наличие ключа в секции (по словарю считанных значений)
            if (m_values[sec].ContainsKey(key) == false)
                //Если нет - добавить
                m_values[sec].Add (key, val);
            else
                //Есть - изменить на новое значение
                m_values[sec][key] = val;
            //Сохранить в файле
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
        /// <summary>
        /// Возвратить словарь всех значений для секции
        /// </summary>
        /// <param name="sec_shr"></param>
        /// <returns></returns>
        protected Dictionary<string, string> getSecValues(string sec_shr)
        {
            //Полное наименование секции в файле конфигурации
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
        public FileINI(string nameFile, bool bReadAuto, string[] par, string[] val, MODE_SECTION_APPLICATION mode = MODE_SECTION_APPLICATION.INSTANCE)
            : this (nameFile, bReadAuto, mode)
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
            bool bRes = false;

            if (_type == TYPE.INI)
            {
                string strVal =
                    //Value
                    Encoding.UTF8.GetString(Encoding.Convert(Encoding.ASCII, Encoding.UTF8, Encoding.ASCII.GetBytes(Value), 0, Value.Length), 0, Value.Length)
                    ;

                bRes = WritePrivateProfileString(Section, Key, strVal, m_NameFileINI);

                Logging.Logg().Debug(@"FileINI::WriteString (sec=" + Section + @", key=" + Key + @") - val=" + Value + @", в файл=" + m_NameFileINI + @" [res=" + bRes.ToString() + @"] - ...", Logging.INDEX_MESSAGE.NOT_SET);
            }
            else
            {
                if (_type == TYPE.XML)
                {
                    bool bSave = false;
                    XmlDocument setup = new XmlDocument();
                    setup.Load(m_NameFileINI);
                    foreach (XmlNode nodes in setup["SETUP"])
                    {
                        if (bSave == false)
                        {
                            if (nodes.FirstChild != null)
                                if (nodes.Attributes.Count > 0)
                                    foreach (XmlAttribute atr in nodes.Attributes)
                                        if (bSave == false)
                                        {
                                            if (atr.Value == Assembly.GetEntryAssembly().FullName.Split(',')[0] + ".exe")
                                                foreach (XmlNode node in nodes)
                                                    if (bSave == false)
                                                    {
                                                        if (node.Name + " (" + atr.Value + ")" == Section)
                                                            if (node.Attributes.Count > 0)
                                                                foreach (XmlAttribute atrSec in node.Attributes)
                                                                    if (atrSec.Name == Key)
                                                                    {
                                                                        atrSec.Value = Value;
                                                                        bSave = true;
                                                                        break;
                                                                    }
                                                    }
                                                    else
                                                        break;
                                        }
                                        else
                                            break;
                        }
                        else
                            break;
                    }

                    if(bSave==true)
                        setup.Save(m_NameFileINI);
                }
            }
        }

        public void WriteInt(String Section, String Key, int Value)
        {
            string s = Value.ToString();
            WriteString(Section, Key, s);
        }
    }
}