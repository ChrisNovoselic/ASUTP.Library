using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms; //Для MessageBox

namespace HClassLibrary
{
    public class FIleConnSett
    {
        public delegate void DelegateOnEventFileConnSettSave(eventFileConnSettSave e);
        public event DelegateOnEventFileConnSettSave EventFileConnSettSave;
        public class eventFileConnSettSave : EventArgs {
            public char [] hash;
            public int length;
            public eventFileConnSettSave(char [] data, int l) { hash = data; length = l; }
        }

        public enum MODE { FILE, SETTINGS };
        private MODE m_mode;
        private bool mayToProtected;

        private string m_arg = string.Empty;

        public FIleConnSett(string arg, MODE mode)
        {
            m_arg = arg;
            m_mode = mode;
        }

        /// <summary>
        /// Разбор зашифрованного файла с параметрами соединения
        /// </summary>
        /// <param name="connSetts">выходной список с объектами с параметрами соединения</param>
        /// <param name="msgErr">Текст сообщения при наличии ошибки</param>
        /// <returns>Признак выполнения: 0 - успех</returns>
        private int ParseSettingsFile(out List <ConnectionSettings> connSetts, out string msgErr)
        {
            connSetts = new List<ConnectionSettings> ();
            msgErr = string.Empty;

            mayToProtected = true;

            char[] file = new char[1024];
            int count = -1;
            if (m_mode == MODE.FILE) {
                    StreamReader sr = new StreamReader(m_arg);
                    count = sr.ReadBlock(file, 0, 1024);
                    sr.Close();
            } else {
                file = m_arg.ToCharArray();
                count = file.Length;
            }

            StringBuilder sb = new StringBuilder(1024);
            int i = 0, j = 0, k = 3,
                countParts = 0;

            if (! (count > 0)) {
                mayToProtected = false;
            } else {
                sb = Crypt.Crypting().Decrypt(file, count, out msgErr);
                if (msgErr.Length > 0)
                    mayToProtected = false;
                else
                    ;
            }

            if (mayToProtected == true)
            {
                int pos1 = 0, pos2 = 0, port;
                bool valid;
                string st = sb.ToString()
                    , ignore
                    ;

                i = 0;

                pos1 = st.IndexOf('#', 0);
                if (pos1 > 0)
                {
                    countParts = System.Int32.Parse(st.Substring(0, pos1));
                    pos1++;

                    //foreach (ConnectionSettings cs in m_connectionSettings)
                    while (i < countParts)
                    {
                        connSetts.Add(new ConnectionSettings ());
                        connSetts[connSetts.Count - 1].id = ConnectionSettings.UN_ENUMERABLE_ID - i;
                        //connSetts[connSetts.Count - 1].port = 1433;
                        //SERVER-IP
                        pos2 = st.IndexOf(';', pos1);
                        if (pos2 < 0)
                        {
                            //MessageBox.Show(this, "Файл с насртойками имеет неправильный формат!\nОбратитесь к поставщику программы.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            mayToProtected = false;
                            break;
                        }
                        else
                            ;
                        connSetts[i].server = st.Substring(pos1, pos2 - pos1);
                        //SERVER-INSTANCE
                        pos1 = pos2 + 1;
                        pos2 = st.IndexOf(';', pos1);
                        if (pos2 < 0)
                        {
                            //MessageBox.Show(this, "Файл с насртойками имеет неправильный формат!\nОбратитесь к поставщику программы.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            mayToProtected = false;
                            break;
                        }
                        else
                            ;
                        connSetts[i].instance = st.Substring(pos1, pos2 - pos1);
                        //PORT
                        pos1 = pos2 + 1;
                        pos2 = st.IndexOf(';', pos1);
                        if (pos2 < 0)
                        {
                            //MessageBox.Show(this, "Файл с насртойками имеет неправильный формат!\nОбратитесь к поставщику программы.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            mayToProtected = false;
                            break;
                        }
                        else
                            ;
                        valid = int.TryParse(st.Substring(pos1, pos2 - pos1), out port);
                        if (!valid)
                        {
                            //MessageBox.Show(this, "В файле настроек неправильно задан порт!\nОбратитесь к поставщику программы.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            mayToProtected = false;
                            break;
                        }
                        else
                            ;
                        connSetts[i].port = port;
                        //DB_NAME
                        pos1 = pos2 + 1;
                        pos2 = st.IndexOf(';', pos1);
                        if (pos2 < 0)
                        {
                            //MessageBox.Show(this, "Файл с насртойками имеет неправильный формат!\nОбратитесь к поставщику программы.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            mayToProtected = false;
                            break;
                        }
                        else
                            ;
                        connSetts[i].dbName = st.Substring(pos1, pos2 - pos1);
                        //UID
                        pos1 = pos2 + 1;
                        pos2 = st.IndexOf(';', pos1);
                        if (pos2 < 0)
                        {
                            //MessageBox.Show(this, "Файл с насртойками имеет неправильный формат!\nОбратитесь к поставщику программы.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            mayToProtected = false;
                            break;
                        }
                        else
                            ;
                        connSetts[i].userName = st.Substring(pos1, pos2 - pos1);
                        //PASSWORD
                        pos1 = pos2 + 1;
                        pos2 = st.IndexOf(';', pos1);
                        if (pos2 < 0)
                        {
                            msgErr = "Файл с насртойками имеет неправильный формат!\nОбратитесь к поставщику программы.";
                            mayToProtected = false;
                            break;
                        }
                        else
                            ;
                        connSetts[i].password = st.Substring(pos1, pos2 - pos1);
                        ////IGNORE
                        //pos1 = pos2 + 1;
                        //pos2 = st.IndexOf(';', pos1);
                        //if (pos2 < 0)
                        //{
                        //    msgErr = "Файл с насртойками имеет неправильный формат!\nОбратитесь к поставщику программы.";
                        //    mayToProtected = false;
                        //    break;
                        //}
                        //else
                        //    ;
                        //ignore = st.Substring(pos1, pos2 - pos1);
                        //if (ignore != "1" && ignore != "0")
                        //{
                        //    msgErr = "В файле настроек неправильно задано игнорирование!\nОбратитесь к поставщику программы.";
                        //    mayToProtected = false;
                        //    break;
                        //}
                        //else
                        //    ;
                        //connSetts[i].ignore = (ignore == "1");

                        ////m_connectionSettingsEdit[i].ignore = cs.ignore = (ignore == "0");

                        //Следующий элемент
                        pos1 = pos2 + 1;

                        i++;
                    }

                    if (mayToProtected == false)
                        for (int c = 0; c < connSetts.Count; c++)
                            connSetts[c].SetDefault();
                    else
                        ;
                }
                else
                    mayToProtected = false; //Не найдено количество блоков соединений
            }
            else
                mayToProtected = false;

            if (mayToProtected == true)
                return 0; //Успех
            else
                return 1; //Ошибка
        }

        /// <summary>
        /// Чтение зашифрованного файла конфигурации с параметрами соединения
        /// </summary>
        /// <param name="notUsed">не используется (-1), для совместимости с типом делегата</param>
        /// <param name="listConnSett">выходной список объектов с параметрами соедений</param>
        /// <param name="res">результат выполнения функции</param>
        /// <param name="mes">сообщение после выполнения операции</param>
        public void ReadSettingsFile (int notUsed, out List <ConnectionSettings> listConnSett, out int res, out string mes)
        {
            //MessageBox.Show (null, m_mode.ToString (), );
            //Console.WriteLine(@"FileConnSett::ReadSettingsFile () - mode=" + m_mode.ToString());

            if ((m_mode == MODE.FILE) && (File.Exists(m_arg) == false))
            {//Не найден файл
                listConnSett = new List<ConnectionSettings>();
                res = 1;
                mes = "Не найден файл: " + m_arg;
            }
            else
                res = ParseSettingsFile(out listConnSett, out mes);
        }

        /// <summary>
        /// Шифрование и сохранение параметров соедиений
        /// </summary>
        /// <param name="notUsed">не используется (-1), для совместимости с типом делегата</param>
        /// <param name="listConnSett"></param>
        /// <param name="err"></param>
        public void SaveSettingsFile(int notUsed, List<ConnectionSettings> listConnSett, out int err)
        {
            err = 1;

            StringBuilder sb = new StringBuilder(1024);

            sb.Append(listConnSett.Count.ToString() + '#');

            foreach (ConnectionSettings cs in listConnSett)
            {
                sb.Append(cs.server + ";");
                sb.Append(cs.instance + ";");
                sb.Append(cs.port.ToString() + ";");
                sb.Append(cs.dbName + ";");
                sb.Append(cs.userName + ";");
                sb.Append(cs.password + ";");
                //sb.Append(cs.ignore ? "1;" : "0;");
            }

            char[] file = Crypt.Crypting().Encrypt(sb, out err);

            if (err > 0)
            {
                if (m_mode == MODE.FILE) {
                    StreamWriter sw = new StreamWriter(m_arg, false);
                    sw.Write(file, 0, err);
                    sw.Close();
                } else {
                    EventFileConnSettSave(new eventFileConnSettSave (file, err));
                }

                err = 0; //Успешно
                mayToProtected = true;
            }
            else
            {
                err = 1; //Ошибка
                mayToProtected = false;
            }
        }
    }
}