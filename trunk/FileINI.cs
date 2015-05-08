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
        /// Наименование главной секции файла конфигурации
        /// </summary>
        private string SEC_MAIN
        {
            get { return SEC_SHR_MAIN + " (" + ProgramBase.AppName + ")"; }
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
        /// <summary>
        /// Получить значение из указанной секции по ключу
        /// </summary>
        /// <param name="sec">Секция в кторой размещен парметр с ключом</param>
        /// <param name="key">Ключ для получения значения</param>
        /// <returns>Значение параметра по ключу</returns>
        public string GetSecValueOfKey(string sec, string key)
        {
            return m_values[sec + @" (" + ProgramBase.AppName + ")"][key];
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
            WritePrivateProfileString(Section, Key, Value, m_NameFileINI);
        }

        public void WriteInt(String Section, String Key, int Value)
        {
            string s = Value.ToString();
            WriteString(Section, Key, s);
        }
    }
}