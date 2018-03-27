using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;

using ASUTP.Database;
using ASUTP;
using ASUTP.Core;

/// <summary>
/// Пространство для классов по организации обработки состояний/событий
/// </summary>
namespace ASUTP.Helper
{
    /// <summary>
    /// Класс - базовый для обработки состояний/событий
    /// </summary>
    public abstract class HHandler : object, IHHandler {
        /// <summary>
        /// Объект для синхронизации изменения списка событий
        /// </summary>
        protected Object m_lockState;
        /// <summary>
        /// Объект потока обработки событий очереди
        /// </summary>
        private Thread taskThreadState;
        /// <summary>
        /// Объект синхронизации, разрешающий начало обработки очереди событий
        /// </summary>
        private Semaphore semaState;
        /// <summary>
        /// Индексы для массива объектов синхронизации
        /// </summary>
        public enum INDEX_WAITHANDLE_REASON {
            SUCCESS
            , ERROR
            , BREAK
                , COUNT_INDEX_WAITHANDLE_REASON }
        /// <summary>
        /// Массив объектов синхронизации
        /// </summary>
        private WaitHandle[] m_waitHandleState;
        /// <summary>
        /// Признак состояния потока
        /// </summary>
        private volatile int threadStateIsWorking;
        /// <summary>
        /// Текущий индекс массива обрабатываемых состояний
        ///  используется только в "потоковой" функции
        /// </summary>
        private volatile int _indexCurState;
        /// <summary>
        /// Текущий индекс массива обрабатываемых состояний
        /// </summary>
        public int IndexCurState { get { return _indexCurState; } }
        /// <summary>
        /// Признак прерывания текущего цикла обработки состояний
        /// </summary>
        private volatile bool newState;
        /// <summary>
        /// Перечисление - возможные признаки для состояния
        /// </summary>
        protected enum FLAG { New = -3, Request, Error, Ok, Warning }
        /// <summary>
        /// Список событий (состояний) для обработки (или очередь)
        /// </summary>
        private volatile List<int> states;

        private List<FLAG> flags;
        /// <summary>
        /// Свойство - Признак активности потока
        /// </summary>
        public bool Actived {
            get {
                return threadStateIsWorking > 0 && threadStateIsWorking % 2 == 1;
            }
        }
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
            //actived = false; //НЕ активен
            threadStateIsWorking = -1; //Не активен

            m_lockState = new Object();
            //Список событий - пустой
            states = new List<int>();
            flags = new List<FLAG>();
        }
        /// <summary>
        /// признак 1-ой активации
        /// </summary>
        public bool IsFirstActivated
        {
            get { return threadStateIsWorking == 1; }
        }
        /// <summary>
        /// Признак выполнения потоковой функции (был вызван метод 'Старт')
        /// </summary>
        public virtual bool IsStarted
        {
            get { return ! (threadStateIsWorking < 0); }
        }
        /// <summary>
        /// Изменение состояния потока
        /// </summary>
        /// <param name="active">Значение нового сотояния</param>
        /// <returns>Признак изменения состояния</returns>
        public virtual bool Activate(bool active)
        {
            bool bRes = !(Actived == active);

            if (bRes == true)
                //if (active == true)
                    threadStateIsWorking++;
                //else ;
            else
                ;

            return bRes;
        }
        /// <summary>
        /// Инициализация объектов синхронизации
        /// </summary>
        protected virtual void InitializeSyncState(int capacity = 1)
        {
            bool bInitRetry = Equals (m_waitHandleState, null) == false;

            if (bInitRetry == false)
                m_waitHandleState = new WaitHandle [capacity];
            else {
                Logging.Logg ().Warning ($"HHandler::InitializeSyncState () - повторный вызов инициализации объектов синхронизации глубина: [есть={m_waitHandleState.Length}, указана={capacity}] ..."
                    , Logging.INDEX_MESSAGE.NOT_SET);
            }

            ////??? 11.05.2015 true -> false
            //m_waitHandleState[(int)INDEX_WAITHANDLE_REASON.SUCCESS] = new AutoResetEvent(true);
            //!!! 27.03.2018
            initializeSyncState (INDEX_WAITHANDLE_REASON.SUCCESS, typeof(AutoResetEvent), true);
        }
        /// <summary>
        /// Инициализация объектов синхронизации (без учета обязательного 'SUCCESS')
        /// </summary>
        protected void AddSyncState (IEnumerable<INDEX_WAITHANDLE_REASON> indexes, IEnumerable<Type> syncTypes, IEnumerable<bool> bInitStates)
        {
            for (int i = 0; i < indexes.Count (); i++)
                AddSyncState (indexes.ElementAt (i), syncTypes.ElementAt (i), bInitStates.ElementAt (i));
        }
        /// <summary>
        /// Инициализация объектов синхронизации
        /// </summary>
        protected void AddSyncState (INDEX_WAITHANDLE_REASON indx, Type syncType, bool bInitState)
        {
            if (!(indx == INDEX_WAITHANDLE_REASON.SUCCESS))
                if (Equals (m_waitHandleState [(int)indx], null) == true)
                    initializeSyncState(indx, syncType, bInitState);
                else {
                    Logging.Logg ().Warning ($"HHandler::InitializeSyncState () - объект синхронизации <{indx}, type={m_waitHandleState [(int)indx].GetType().Name}> был создан ранее..."
                        , Logging.INDEX_MESSAGE.NOT_SET);
                }
            else
                Logging.Logg ().Warning ($"HHandler::InitializeSyncState () - объект синхронизации <{indx}> создается автоматически...", Logging.INDEX_MESSAGE.NOT_SET);
        }

        private void initializeSyncState (INDEX_WAITHANDLE_REASON indx, Type syncType, bool bInitState)
        {
            m_waitHandleState [(int)indx] = (WaitHandle)Activator.CreateInstance (syncType, System.Reflection.BindingFlags.CreateInstance, new object [] { bInitState });
        }
        /// <summary>
        /// Старт потоковой функции обработки событий
        /// </summary>
        public virtual void Start()
        {
            if (threadStateIsWorking < 0)
            {
                threadStateIsWorking = 0;
                taskThreadState = new Thread(new ParameterizedThreadStart(ThreadStates));
                taskThreadState.Name = "Обработка событий для объекта " + this.GetType().AssemblyQualifiedName;
                taskThreadState.IsBackground = true;
                taskThreadState.CurrentCulture =
                taskThreadState.CurrentUICulture =
                    ProgramBase.ss_MainCultureInfo;

                semaState = new Semaphore(1, 1);

                InitializeSyncState();

                semaState.WaitOne();
                taskThreadState.Start();
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
            threadStateIsWorking = -1;
            //Очистить очередь событий
            ClearStates();
            //Прверить выполнение потоковой функции
            if ((!(taskThreadState == null)) && (taskThreadState.IsAlive == true))
            {
                //Выход из потоковой функции
                Run(@"HHandler::Stop ()");
                //Ожидать завершения потоковой функции
                joined = taskThreadState.Join(Constants.WAIT_TIME_MS);
                //Проверить корректное завершение потоковой функции
                if (joined == false)
                    //Завершить аварийно потоковую функцию
                    taskThreadState.Abort();
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
                if (semaState.WaitOne (0, true) == false)
                    semaState.Release(1);
                else ;
            }
            catch (Exception e)
            {
                Logging.Logg().Exception(e, throwMes + @" - semaState.Release(1)", Logging.INDEX_MESSAGE.NOT_SET);
            }
        }
        /// <summary>
        /// Проверить является ли указанное состояние последним в очереди
        /// </summary>
        /// <param name="state">Состояние для проверки</param>
        /// <returns>Признак выполнения условия</returns>
        protected bool isLastState (int state)
        {
            return states.Count > 0 ? states[states.Count - 1] == state : false;
        }
        /// <summary>
        /// Добавить состояние в список
        /// </summary>
        /// <param name="state">Добавляемое состояние</param>
        public void AddState(int state)
        {
            states.Add(state);
            flags.Add(FLAG.New);
        }

        private void clearStates(bool newState = false)
        {
            this.newState = newState;
            states.Clear();
            flags.Clear();
        }
        /// <summary>
        /// Очистить список состояний (событий)
        /// </summary>
        public virtual void ClearStates()
        {
            clearStates(true);
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
        protected abstract INDEX_WAITHANDLE_REASON StateErrors(int state, int req, int res);
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
        private void ThreadStates(object data)
        {
            int currentState;
            bool bRes = false;

            int requestSent = 0
                , responseReceived = 0;
            INDEX_WAITHANDLE_REASON reason = INDEX_WAITHANDLE_REASON.SUCCESS;

            while (!(threadStateIsWorking < 0))
            {
                bRes = false;
                bRes = semaState.WaitOne();

                try
                {
                    _indexCurState = 0;

                    lock (m_lockState)
                    {
                        if (states.Count == 0)
                            continue;
                        else
                            ;

                        currentState = states[_indexCurState];
                        newState = false;
                    }

                    while (true)
                    {
                        if (!(flags[_indexCurState] == FLAG.Ok)) {
                        // только для не обработанных состояний или состояний с ошибками/предупреждениями
                            requestSent =
                            responseReceived = 0;
                            reason = INDEX_WAITHANDLE_REASON.SUCCESS;

                            bool error = true;
                            int requestDone = -1;
                            object objRes = null;
                            for (int i = 0;
                                i < Constants.MAX_RETRY
                                    && (!(requestDone == 0))
                                    && (newState == false);
                                i++)
                            {
                                if (error)
                                {
                                    flags[_indexCurState] = FLAG.Request;
                                    requestSent = StateRequest(currentState);
                                    if (!(requestSent == 0))
                                        break;
                                    else
                                        ;
                                }
                                else
                                    ;

                                error = false;
                                for (int j = 0;
                                    j < Constants.MAX_WAIT_COUNT
                                        && (!(requestDone == 0))
                                        && (error == false)
                                        && (newState == false);
                                    j++)
                                {
                                    System.Threading.Thread.Sleep(Constants.WAIT_TIME_MS);
                                    requestDone = StateCheckResponse(currentState, out error, out objRes);
                                }
                            }

                            if (!(requestSent < 0))
                            {
                                if ((requestDone == 0)
                                    && (error == false)
                                    && (newState == false)) {
                                    flags[_indexCurState] = FLAG.Ok;
                                    responseReceived = StateResponse(currentState, objRes);
                                } else
                                    responseReceived = requestSent == 0 ? -1 : 1;

                                if (responseReceived == -102)
                                    //Для алгоритма сигнализации 'TecView::AlarmEventRegistred () - ...'
                                    reason = INDEX_WAITHANDLE_REASON.BREAK;
                                else
                                    if (((!(responseReceived == 0))
                                            || (!(requestDone == 0))
                                            || (error == true))
                                        && (newState == false))
                                    {
                                        if (responseReceived < 0)
                                        {
                                            flags[_indexCurState] = FLAG.Error;
                                            reason = StateErrors(currentState, requestSent, responseReceived);
                                            lock (m_lockState)
                                            {
                                                if (newState == false)
                                                {
                                                    clearStates();
                                                    break;
                                                }
                                                else
                                                    ;
                                            }
                                        }
                                        else {
                                            flags[_indexCurState] = FLAG.Warning;
                                            StateWarnings(currentState, requestSent, responseReceived);
                                        }
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
                                        clearStates();
                                        break;
                                    }
                                    else
                                        ;
                                }
                            }
                        } else
                        // состояние уже было обработано
                            ;

                        _indexCurState++;

                        lock (m_lockState)
                        {
                            if (_indexCurState == states.Count)
                                break;
                            else
                                ;

                            if (newState == true)
                                break;
                            else
                                ;
                            currentState = states[_indexCurState];
                        }
                    }

                    //Закончена обработка всех событий
                    completeHandleStates(reason);
                    //Текущий индекс вне дипазона
                    _indexCurState = -1;
                }
                catch (Exception e)
                {
                    Logging.Logg().Exception(e, @"HHandler::ThreadStates () - ...", Logging.INDEX_MESSAGE.NOT_SET);
                }
            }
            //Освободить ресурс ядра ОС
            //??? "везде" 'true'
            if (bRes == false)
                try
                {
                    semaState.Release(1);
                }
                catch (Exception e)
                { //System.Threading.SemaphoreFullException
                    Logging.Logg().Exception(e, "HHandler::ThreadStates () - semaState.Release(1)", Logging.INDEX_MESSAGE.NOT_SET);
                }
            else
                ;
        }

        /// <summary>
        /// Установить признак окончания обработки всех событий
        /// </summary>
        protected void completeHandleStates(INDEX_WAITHANDLE_REASON indxEv)
        {
            try
            {
                if (((int)indxEv == (int)INDEX_WAITHANDLE_REASON.SUCCESS)
                    || ((int)indxEv > (m_waitHandleState.Length - 1)))
                    ((AutoResetEvent)m_waitHandleState[(int)INDEX_WAITHANDLE_REASON.SUCCESS]).Set();
                else
                    ((ManualResetEvent)m_waitHandleState[(int)indxEv]).Set();

            }
            catch (Exception e)
            {
                Logging.Logg().Exception(e, "HHandler::ThreadFunction () - m_waitHandleState[0]).Set()", Logging.INDEX_MESSAGE.NOT_SET);
            }
        }

        //protected void abortThreadGetValues(INDEX_WAITHANDLE_REASON reason)
        //{
        //    if (m_waitHandleState.Length > (int)reason)
        //    {
        //        ((ManualResetEvent)m_waitHandleState[(int)reason]).Set();
        //    }
        //    else
        //        ;
        //}

        /// <summary>
        /// Ожидать сигнала объекта синхронизации с индексом
        /// </summary>
        /// <param name="indxEv">Индекс объекиа синхронизации</param>
        /// <param name="msec_wait">Период ожидания (System.Threading.Timeout.Infinite - бесконечно)</param>
        /// <param name="bExitContext">Признак выхода из домена синхронизации перед ожиданием</param>
        /// <returns>Результат ожидания</returns>
        public bool WaitOne (INDEX_WAITHANDLE_REASON indxEv, int msec_wait, bool bExitContext)
        {
            return m_waitHandleState [(int)indxEv].WaitOne (msec_wait, bExitContext);
        }

        public bool WaitOne (INDEX_WAITHANDLE_REASON indxEv, TimeSpan ts_wait, bool bExitContext)
        {
            return WaitOne(indxEv, (int)ts_wait.TotalMilliseconds, bExitContext);
        }

        public int WaitAny (int msec_wait, bool bExitContext)
        {
            return WaitHandle.WaitAny(m_waitHandleState, msec_wait, bExitContext);
        }

        public int WaitAny (TimeSpan ts_wait, bool bExitContext)
        {
            return WaitAny ((int)ts_wait.TotalMilliseconds, bExitContext);
        }

        public void Reset ()
        {
            for (INDEX_WAITHANDLE_REASON indx = INDEX_WAITHANDLE_REASON.ERROR; indx < INDEX_WAITHANDLE_REASON.COUNT_INDEX_WAITHANDLE_REASON; indx ++)
                Reset (indx);
        }

        public void Reset (INDEX_WAITHANDLE_REASON indx)
        {
            if (typeof (ManualResetEvent).IsAssignableFrom (m_waitHandleState [(int)indx].GetType ()) == true)
                (m_waitHandleState [(int)indx] as ManualResetEvent).Reset ();
            else
                ;
        }
    }
}
