using ASUTP.Core;
using ASUTP.Database;
using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace ASUTP.Forms {
    //public class HException : Exception
    //{
    //    public HException(string msg) : base(msg) { }
    //}

    /// <summary>
    /// Базовая форма для главной формы приложения
    ///  , обрабатывает файл конфигурации с параметрами для соединения с БД
    ///  , отображает сообщение при аварийном завершении работы
    /// </summary>
    public abstract class FormMainBase : Form, ASUTP.Helper.IFormMainBase
    {
        /// <summary>
        /// Форма, индицирующая продолжительное выполнение операции
        /// </summary>
        private FormWait m_formWait;
        /// <summary>
        /// Объект для работы с шифрованным файлом с параметрами соединения с БД (конфигурации)
        /// </summary>
        protected static FIleConnSett s_fileConnSett;
        /// <summary>
        /// Объект для синхронизации доступа к счетчику кол-ва отображаемых наследуемых форм
        /// </summary>
        private static object lockCounter = new object();
        /// <summary>
        /// СЧетчик кол-ва наследуемых отображаемых форм
        /// </summary>
        private static int formCounter = 0;
        /// <summary>
        /// Делегат для вызова на отображение окна 'FormWait'
        /// </summary>        
        protected DelegateFunc delegateStartWait;
        /// <summary>
        /// Делегат для снятия с отображения окна 'FormWait'
        /// </summary>
        protected DelegateFunc delegateStopWait;
        /// <summary>
        /// Делегат для обработки события периодического обновления строки состояния наследуемой формы
        /// </summary>
        protected DelegateFunc delegateEvent;
        /// <summary>
        /// Делегат для обработки события - применение параметров (с обновлением) графической интерпретации данных
        /// </summary>        
        protected DelegateIntFunc delegateUpdateActiveGui;
        /// <summary>
        /// Делегат для обработки события - скрыть форму с параметрами графической интерпретации данных
        /// </summary>
        protected DelegateFunc delegateHideGraphicsSettings;
        /// <summary>
        /// Делегат для обработки события - применение параметров
        /// </summary>
        protected DelegateFunc delegateParamsApply;
        /// <summary>
        /// Идентификатор основного источника данных
        /// </summary>
        public static int s_iMainSourceData = -1;

        /// <summary>
        /// Конструктор - основной (без параметров)
        /// </summary>
        protected FormMainBase()
        {
            InitializeComponent();

            m_formWait = FormWait.This;

            this.Shown += new EventHandler(FormMainBase_Shown);
            //this.HandleCreated += new EventHandler(FormMainBase_HandleCreated);
            this.HandleDestroyed += new EventHandler(FormMainBase_HandleDestroyed);
            this.FormClosed += new FormClosedEventHandler(FormMainBase_FormClosed);

            delegateStartWait = new DelegateFunc(startWait);
            delegateStopWait = new DelegateFunc(stopWait);
        }

        /// <summary>
        /// Инициализация индивидуальных параметров формы
        /// </summary>
        private void InitializeComponent()
        {
            //TODO:
        }

        /// <summary>
        /// Инициировать аварийное завершение работы
        /// </summary>
        /// <param name="msg">Сообщение при исключении (аврийном завершении работы)</param>
        protected void Abort(string msg)
        {
            throw new Exception(msg);
        }

        /// <summary>
        /// Инициировать (при необходимости) аврийное завершение
        ///  , отобразить сообщение
        /// </summary>
        /// <param name="msg">Текст сообщения</param>
        /// <param name="bThrow">Признак инициирования аварийного завершения</param>
        /// <param name="bSupport">Признак отображения контактной информации техн./поддержки</param>
        protected virtual void Abort(string msg, bool bThrow = false, bool bSupport = true)
        {
            this.Activate();

            string msgThrow = msg + @".";
            if (bSupport == true)
                msgThrow += Environment.NewLine + @"Обратитесь к оператору тех./поддержки по тел. 4444 или по тел. 289-03-37.";
            else
                ;

            if (bThrow == true)
                Abort(msgThrow);
            else
                MessageBox.Show(this, msgThrow, "Ошибка в работе программы!", MessageBoxButtons.OK, MessageBoxIcon.Stop);
        }

        /// <summary>
        /// Запустить (отобразить) форму 'FormWait'
        /// </summary>
        private void startWait()
        {
            //Logging.Logg().Debug(@"FormMainBase::startWait (WindowState=" + this.WindowState + @") - ...", Logging.INDEX_MESSAGE.NOT_SET);

            if (!(this.WindowState == FormWindowState.Minimized))
                //m_formWait.StartWaitForm (this)
                m_formWait.StartWaitForm(this.Location, this.Size)
                ;
            else
                ;
        }

        /// <summary>
        /// Остановить (скрыть) форму 'FormWait' 
        /// </summary>
        private void stopWait()
        {
            //Logging.Logg().Debug(@"FormMainBase::stopWait (WindowState=" + this.WindowState + @") - ...", Logging.INDEX_MESSAGE.NOT_SET);

            m_formWait.StopWaitForm();
        }

        /// <summary>
        /// Рекурсивная функция поиска элемента меню в указанном пункте меню
        /// </summary>
        /// <param name="miParent">Пункт меню в котором осуществляется поиск</param>
        /// <param name="text"></param>
        /// <returns>Результат - пукт меню с текстом для поиска</returns>
        private ToolStripMenuItem findMainMenuItemOfText(ToolStripMenuItem miParent, string text)
        {
            //Результат 
            ToolStripMenuItem itemRes = null;

            if (miParent.Text == text)
                itemRes = miParent;
            else
                //Цикл по всем элементам пункта меню
                foreach (ToolStripItem mi in miParent.DropDownItems)
                    if (mi is ToolStripMenuItem)
                        if (mi.Text == text)
                        {
                            itemRes = mi as ToolStripMenuItem;
                            break;
                        }
                        else
                            //Проверить наличие подменю
                            if (((ToolStripMenuItem)mi).DropDownItems.Count > 0)
                            {
                                //Искать элемент в подменю
                                itemRes = findMainMenuItemOfText(mi as ToolStripMenuItem, text);

                                if (!(itemRes == null))
                                    break;
                                else
                                    ;
                            }
                            else
                                ;
                    else
                        ;

            return itemRes;
        }

        /// <summary>
        /// Поиск в главном меню элемента с именнем
        /// </summary>
        /// <param name="text">Текст пункта (под)меню для поиска</param>
        /// <returns>Результат - пукт меню с текстом для поиска</returns>
        public ToolStripMenuItem FindMainMenuItemOfText(string text)
        {
            ToolStripMenuItem itemRes = null;

            foreach (ToolStripMenuItem mi in MainMenuStrip.Items)
            {
                itemRes = findMainMenuItemOfText(mi, text);

                if (!(itemRes == null))
                    break;
                else
                    ;
            }

            return itemRes;
        }

        private void removeMainMenuItem(ToolStripMenuItem findItem)
        {
            ToolStripMenuItem ownerItem = null;

            ownerItem = findItem.OwnerItem as ToolStripMenuItem;

            if (!(ownerItem == null))
            {
                ownerItem.DropDownItems.Remove(findItem);

                if (ownerItem.DropDownItems.Count == 0)
                    removeMainMenuItem(ownerItem);
                else
                    ;
            }
            else
                if (ownerItem == null)
                    MainMenuStrip.Items.Remove(findItem);
                else
                    ;
        }

        /// <summary>
        /// Удалить п.главного меню приложения (по тексту)
        /// </summary>
        /// <param name="text">Текст (под)пункта меню</param>
        /// <returns>Признак выполнения удаления (-1 - ошибка, 0 - элемент не найден, 1 - пункт меню удален)</returns>
        public int RemoveMainMenuItemOfText(string text)
        {
            int iRes = 0; //-1 - ошибка, 0 - элемент не найден, 1 - пункт меню удален
            ToolStripMenuItem findItem = null;            

            foreach (ToolStripMenuItem mi in MainMenuStrip.Items)
            {
                findItem = findMainMenuItemOfText(mi, text);

                if (!(findItem == null))
                    break;
                else
                    ;
            }

            if (!(findItem == null))
            {
                removeMainMenuItem(findItem);
            }
            else
                ;

            return iRes;
        }

        /// <summary>
        /// Закрыть окно
        /// </summary>
        /// <param name="bForce">Признак немедленного закрытия окна</param>
        public virtual void Close(bool bForce) { base.Close(); }

        /// <summary>
        /// Обработчик события создания дескрипотра окна
        ///  для подсчета кол-ва отображаемых наследуемых форм
        ///  для своевременного вызова функции полного останова окна 'FormWait'
        /// </summary>
        /// <param name="obj">Объект, инициировавший событие - this</param>
        /// <param name="ev">Аргумент события</param>
        private void FormMainBase_Shown(object obj, EventArgs ev)
        {
            lock (lockCounter)
            {
                //Увеличить счетчик - актуализировать количество наследуемых И отображаемых форм
                formCounter++;

                //Console.WriteLine(@"FormMainBase::InitializeComponent () - formCounter=" + formCounter);
            }
        }

        //private void FormMainBase_HandleCreated(object obj, EventArgs ev)
        //{
        //    Logging.Logg().Debug(@"FormMainBase::FormMainBase_HandleCreated () ...", Logging.INDEX_MESSAGE.NOT_SET);
        //}

        private void FormMainBase_HandleDestroyed(object obj, EventArgs ev)
        {
            //Logging.Logg().Debug(@"FormMainBase::FormMainBase_HandleDestroyed () - formCounter=" + (formCounter - 1) + @"...", Logging.INDEX_MESSAGE.NOT_SET);
            
            lock (lockCounter)
            {
                //Декрементировать счетчик - актуализировать количество наследуемых И отображаемых форм
                formCounter--;

                //Console.WriteLine(@"FormMainBase::InitializeComponent () - formCounter=" + formCounter);
            }
        }

        /// <summary>
        /// Обработчик события - закрытие формы
        ///  для подсчета кол-ва отображаемых наследуемых форм
        /// </summary>
        /// <param name="obj">Объект, инициировавший событие - this</param>
        /// <param name="ev">Аргумент события</param>
        private void FormMainBase_FormClosed(object obj, FormClosedEventArgs ev)
        {
            lock (lockCounter)
            {
                //Проверить кол-во отображаемых наследуемых форм
                if ((formCounter - 1) == 0)
                    //Полный останов 'FormWait'
                    m_formWait.StopWaitForm(true);
                else
                    ;
            }
        }
    }
}