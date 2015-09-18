using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace HClassLibrary
{
    //public class HException : Exception
    //{
    //    public HException(string msg) : base(msg) { }
    //}
    
    public abstract class FormMainBase : Form
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
        private static object lockCounter = new object ();
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

            this.HandleCreated += new EventHandler(FormMainBase_HandleCreated);
            this.FormClosed += new FormClosedEventHandler(FormMainBase_FormClosed);

            delegateStartWait = new DelegateFunc(startWait);
            delegateStopWait = new DelegateFunc(stopWait);
        }
        /// <summary>
        /// Инициализация индивидуальных параметров формы
        /// </summary>
        private void InitializeComponent()
        {
        }
        /// <summary>
        /// Инициировать аварийное завершение работы
        /// </summary>
        /// <param name="msg"></param>
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

            MessageBox.Show(this, msgThrow, "Ошибка в работе программы!", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            if (bThrow == true) Abort(msgThrow); else ;
        }
        /// <summary>
        /// Запустить (отобразить) форму 'FormWait'
        /// </summary>
        private void startWait()
        {
            m_formWait.StartWaitForm (this.Location, this.Size);
        }
        /// <summary>
        /// Остановить (скрыть) форму 'FormWait' 
        /// </summary>
        private void stopWait()
        {
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
                                //Искать элемент в подменю
                                findMainMenuItemOfText(mi as ToolStripMenuItem, text);
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

                if (! (itemRes == null))
                    break;
                else
                    ;
            }

            return itemRes;
        }
        /// <summary>
        /// Закрыть окно
        /// </summary>
        /// <param name="bForce">Признак немедленного закрытия окна</param>
        public virtual void Close (bool bForce) { base.Close (); }
        /// <summary>
        /// Обработчик события создания дескрипотра окна
        ///  для подсчета кол-ва отображаемых наследуемых форм
        ///  для своевременного вызова функции полного останова окна 'FormWait'
        /// </summary>
        /// <param name="obj">Объект, инициировавший событие - this</param>
        /// <param name="ev">Аргумент события</param>
        private void FormMainBase_HandleCreated (object obj, EventArgs ev)
        {
            lock (lockCounter)
            {
                //Увеличить счетчик
                formCounter ++;
            }
        }
        /// <summary>
        /// Обработчик события - закрытие формы
        ///  для подсчета кол-ва отображаемых наследуемых форм
        /// </summary>
        /// <param name="obj">Объект, инициировавший событие - this</param>
        /// <param name="ev">Аргумент события</param>
        private void  FormMainBase_FormClosed (object obj, FormClosedEventArgs ev)
        {
            lock (lockCounter)
            {
                //Декрементировать счетчик
                formCounter--;
                //Проверить кол-во отображаемых наследуемых форм
                if (formCounter == 0)
                    //Полный останов 'FormWait'
                    m_formWait.StopWaitForm (true);
                else
                    ;
            }
        }
    }
}