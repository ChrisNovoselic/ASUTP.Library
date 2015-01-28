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

        public string m_NameFileINI = string.Empty;

        public FileINI(string nameFile)
        {
            m_NameFileINI = System.Environment.CurrentDirectory + "\\" + nameFile;
            if (File.Exists(m_NameFileINI) == false)
            {
                File.Create(m_NameFileINI);
                //throw new Exception ("Не удалось найти файл инициализации (полный путь: " + m_NameFileINI + ")");
            }
            else
                ;

            m_values = new Dictionary<string,Dictionary<string,string>> ();
        }

        private Dictionary <string, Dictionary<string, string>> m_values;

        private string SEC_MAIN {
            get { return "Main (" + ProgramBase.AppName + ")"; }
        }

        public string GetValueOfKey(string key) {
            return GetValueOfKey (@"Main", key);
        }

        public string GetValueOfKey(string sec, string key)
        {
            return m_values[sec + @" (" + ProgramBase.AppName + ")"][key];
        }

        public FileINI(string nameFile, string[] par, string[] val) : this (nameFile)
        {
            string key = string.Empty;

            if (par.Length == val.Length) {
                m_values.Add (SEC_MAIN, new Dictionary<string,string> ());
                for (int i = 0; i < par.Length; i ++) {
                    key = par [i];
                    m_values [SEC_MAIN].Add(key, ReadString(SEC_MAIN, key, string.Empty));
                    if (m_values[SEC_MAIN][key].Equals(string.Empty) == true)
                    {
                        m_values [SEC_MAIN][key] = val [i];
                        WriteString(SEC_MAIN, key, val[i]);
                    }
                    else
                        ;
                }
            }
            else
                throw new Exception (@"FileINI::с параметрами...");
        }

        public void Add(string par, string val)
        {
            Add (@"Main", par, val);
        }

        public void Add (string sec_shr, string par, string val) {
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