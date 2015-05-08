using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;

using HClassLibrary;

namespace HClassLibrary
{
    public abstract class HHandler : object
    {
        /// <summary>
        /// Объект для синхронизации изменения списка событий
        /// </summary>
        protected Object m_lockState;
        /// <summary>
        /// Объект потока обработки событий очереди
        /// </summary>
        private Thread taskThread;
        /// <summary>
        /// Объект синхронизации, разрешающий начало обработки очереди событий
        /// </summary>
        private Semaphore semaState;
        /// <summary>
        /// Индексы для массива объектов синхронизации
        /// </summary>
        public enum INDEX_WAITHANDLE_REASON { SUCCESS, ERROR, BREAK, COUNT_INDEX_WAITHANDLE_REASON }
        /// <summary>
        /// Массив объектов синхронизации
        /// </summary>
        protected WaitHandle[] m_waitHandleState;
        /// <summary>
        /// Признак состояния потока
        /// </summary>
        public volatile int threadIsWorking;
        /// <summary>
        /// Признак 
        /// </summary>
        private volatile bool newState;
        /// <summary>
        /// Список событий (состояний) для обработки (или очередь)
        /// </summary>
        private volatile List<int /*StatesMachine*/> states;
        /// <summary>
        /// Признак активности потока
        /// </summary>
        private bool actived;
        /// <summary>
        /// Свойство - Признак активности потока
        /// </summary>
        public bool m_bIsActive { get { return actived; } }
        /// <summary>
        /// Конструктор - основной
        /// </summary>
        public HHandler()
        {
            //Установка "культуры" для корректной обработки значений, зависимых от настроек АРМ
            Thread.CurrentThread.CurrentCulture =
            Thread.CurrentThread.CurrentUICulture =
                ProgramBase.ss_MainCultureInfo;

            Initialize ();
        }
        /// <summary>
        /// Инициализация
        /// </summary>
        protected virtual void Initialize()
        {
            actived = false; //НЕ активен
            threadIsWorking = -1; //Не активен

            m_lockState = new Object();
            //Список событий - пустой
            states = new List<int /*StatesMachine*/>();
        }
        /// <summary>
        /// Изменение состояния потока
        /// </summary>
        /// <param name="active">Значение нового сотояния</param>
        /// <returns>Признак изменения состояния</returns>
        public virtual bool Activate(bool active)
        {
            bool bRes = true;

            if (active == true) threadIsWorking++; else ;

            if (actived == active)
            {
                bRes = false;
            }
            else
            {
                actived = active;
            }

            return bRes;
        }
        /// <summary>
        /// Инициализация объектов синхронизации
        /// </summary>
        protected virtual void InitializeSyncState()
        {
            if (m_waitHandleState == null)
                m_waitHandleState = new WaitHandle[1];
            else
                ;

            m_waitHandleState[(int)INDEX_WAITHANDLE_REASON.SUCCESS] = new AutoResetEvent(true);
        }
        /// <summary>
        /// Старт потоковой функции обработки событий
        /// </summary>
        public virtual void Start()
        {
            if (threadIsWorking < 0)
            {
                threadIsWorking = 0;
                taskThread = new Thread(new ParameterizedThreadStart(ThreadFunction));
                taskThread.Name = "Обработка событий для объекта " + this.GetType ().AssemblyQualifiedName;
                taskThread.IsBackground = true;
                taskThread.CurrentCulture =
                taskThread.CurrentUICulture =
                    ProgramBase.ss_MainCultureInfo;

                semaState = new Semaphore(1, 1);

                InitializeSyncState();

                semaState.WaitOne();
                taskThread.Start();
            }
            else
                ;
        }
        /// <summary>
        /// Останов потоковой функции обработки событий
        /// </summary>
        public virtual void Stop()
        {
            bool joined;
            threadIsWorking = -1;
            //Очисить очередь событий
            ClearStates();
            //Прверить выполнение потоковой функции
            if ((!(taskThread == null)) && (taskThread.IsAlive == true))
            {
                //Выход из потоковой функции
                try { semaState.Release(1); }
                catch (Exception e)
                {
                    Logging.Logg().Exception(e, Logging.INDEX_MESSAGE.NOT_SET, "HAdmin::StopThreadSourceData () - semaState.Release(1)");
                }
                //Ожидать завершения потоковой функции
                joined = taskThread.Join(666);
                //Проверить корректное завершение потоковой функции
                if (joined == false)
                    //Завершить аварийно потоковую функцию
                    taskThread.Abort();
                else
                    ;
            }
            else ;
        }
        /// <summary>
        /// Начать обработку списка событий
        /// </summary>
        /// <param name="throwMes">Сообщение при ошибке</param>
        public void Run(string throwMes)
        {
            try
            {
                semaState.Release(1);
            }
            catch (Exception e)
            {
                Logging.Logg().Exception(e, Logging.INDEX_MESSAGE.NOT_SET, throwMes + @" - semaState.Release(1)");
            }
        }
        /// <summary>
        /// Проверить 
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        protected bool isLastState (int state)
        {
            return states.IndexOf(state) == (states.Count - 1);
        }
        /// <summary>
        /// Добавить состояние в список
        /// </summary>
        /// <param name="state">Добавляемое состояние</param>
        public void AddState(int state)
        {
            states.Add(state);
        }
        /// <summary>
        /// Очистить список состояний (событий)
        /// </summary>
        public virtual void ClearStates()
        {
            newState = true;
            states.Clear();
        }        
        /// <summary>
        /// Получить результат обработки события
        /// </summary>
        /// <param name="state">Событие для получения результата</param>
        /// <param name="error">Признак ошибки при получении результата</param>
        /// <param name="outobj">Результат запроса</param>
        /// <returns>Признак получения результата</returns>
        protected abstract int StateCheckResponse(int state, out bool error, out object outobj);
        /// <summary>
        /// Запросить результат для события
        /// </summary>
        /// <param name="state">Событие запроса</param>
        /// <returns>Признак отправления результата</returns>
        protected abstract int StateRequest(int state);
        /// <summary>
        /// Обработка УСЕШНО полученного результата
        /// </summary>
        /// <param name="state">Состояние для результата</param>
        /// <param name="obj">Значение результата</param>
        /// <returns>Признак обработки результата</returns>
        protected abstract int StateResponse(int state, object obj);
        /// <summary>
        /// Обработка КРИТИЧЕСКОЙ ошибки при получении результата
        /// </summary>
        /// <param name="state">Состояние запроса</param>
        /// <param name="req">Признак получения ответа при запросе</param>
        /// <param name="res">Признак...</param>
        protected abstract void StateErrors(int state, int req, int res);
        /// <summary>
        /// Обработка НЕ КРИТИЧной ошибки при получении результата
        /// </summary>
        /// <param name="state">Состояние запроса</param>
        /// <param name="req">Признак получения ответа при запросе</param>
        /// <param name="res">Признак...</param>
        protected abstract void StateWarnings(int state, int req, int res);
        /// <summary>
        /// Потоковая функция
        /// </summary>
        /// <param name="data">Параметр при старте потоковой функции</param>
        private void ThreadFunction(object data)
        {
            int index;
            int /*StatesMachine*/ currentState;
            bool bRes = false;

            while (!(threadIsWorking < 0))
            {
                bRes = false;
                bRes = semaState.WaitOne();

                index = 0;

                lock (m_lockState)
                {
                    if (states.Count == 0)
                        continue;
                    else
                        ;

                    currentState = states[index];
                    newState = false;
                }

                while (true)
                {
                    int requestIsOk = 0;
                    bool error = true;
                    int dataPresent = -1;
                    object objRes = null;
                    for (int i = 0; i < DbInterface.MAX_RETRY && (!(dataPresent == 0)) && (newState == false); i++)
                    {
                        if (error)
                        {
                            requestIsOk = StateRequest(currentState);
                            if (!(requestIsOk == 0))
                                break;
                            else
                                ;
                        }
                        else
                            ;

                        error = false;
                        for (int j = 0; j < DbInterface.MAX_WAIT_COUNT && (!(dataPresent == 0)) && (error == false) && (newState == false); j++)
                        {
                            System.Threading.Thread.Sleep(DbInterface.WAIT_TIME_MS);
                            dataPresent = StateCheckResponse(currentState, out error, out objRes);
                        }
                    }

                    if (requestIsOk == 0)
                    {
                        int responseIsOk = 0;
                        if ((dataPresent == 0) && (error == false) && (newState == false))
                            responseIsOk = StateResponse(currentState, objRes);
                        else
                            responseIsOk = -1;

                        if (((!(responseIsOk == 0)) || (!(dataPresent == 0)) || (error == true)) && (newState == false))
                        {
                            if (responseIsOk < 0)
                            {
                                StateErrors(currentState, requestIsOk, responseIsOk);
                                lock (m_lockState)
                                {
                                    if (newState == false)
                                    {
                                        states.Clear();
                                        break;
                                    }
                                    else
                                        ;
                                }
                            }
                            else
                                StateWarnings(currentState, requestIsOk, responseIsOk);
                        }
                        else
                            ;
                    }
                    else
                    {
                        //14.04.2015 ???
                        //StateErrors(currentState, requestIsOk, -1);

                        lock (m_lockState)
                        {
                            if (newState == false)
                            {
                                states.Clear();
                                break;
                            }
                            else
                                ;
                        }
                    }

                    index++;

                    lock (m_lockState)
                    {
                        if (index == states.Count)
                            break;
                        else
                            ;

                        if (newState)
                            break;
                        else
                            ;
                        currentState = states[index];
                    }
                }

                //Закончена обработка всех событий
                completeHandleStates();
            }
            if (bRes == true)
                try
                {
                    semaState.Release(1);
                }
                catch (Exception e)
                { //System.Threading.SemaphoreFullException
                    Logging.Logg().Exception(e, Logging.INDEX_MESSAGE.NOT_SET, "HHandler::ThreadFunction () - semaState.Release(1)");
                }
            else
                ;
        }

        /// <summary>
        /// Установить признак окончания обработки всех событий
        /// </summary>
        protected void completeHandleStates()
        {
            try { ((AutoResetEvent)m_waitHandleState[0]).Set(); }
            catch (Exception e)
            {
                Logging.Logg().Exception(e, Logging.INDEX_MESSAGE.NOT_SET, "HHandler::ThreadFunction () - m_waitHandleState[0]).Set()");
            }
        }

        protected void abortThreadGetValues(INDEX_WAITHANDLE_REASON reason)
        {
            if (m_waitHandleState.Length > (int)reason)
            {
                ((ManualResetEvent)m_waitHandleState[(int)reason]).Set();
            }
            else
                ;
        }
    }
}
