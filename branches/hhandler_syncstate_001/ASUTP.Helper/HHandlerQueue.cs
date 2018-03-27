using ASUTP.Core;
using ASUTP.Database;
using ASUTP.PlugIn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;

namespace ASUTP.Helper {
    public interface IHHandlerQueue
    {
        bool Activate(bool active);
        void Push(IDataHost obj, object[] pars);
        void Start();
        void Stop();
    }

    public abstract class HHandlerQueue : HHandler, IHHandlerQueue
    {
        protected class ItemQueue
        {
            private HHandler _owner;
            /// <summary>
            /// Объект, добавививший в очередь событие(состочние), используется для обратной связи
            /// </summary>
            public IDataHost m_dataHostRecieved;
            public List<int> m_states; //??? Дублирование _owner.states
            private object[] m_pars;
            /// <summary>
            /// Параметры, переданные в качестве аргументов к событию(состоянию)
            /// </summary>
            public object[] Pars
            {
                get { 
                    object [] arObjRes = null;
                    
                    try {
                        arObjRes = (m_pars as object[])[_owner.IndexCurState] as object[];
                    } catch (Exception e) {
                        Logging.Logg().Exception(e, @"HHandlerQueue.ItemQueue::Pars - ...", Logging.INDEX_MESSAGE.NOT_SET);
                    }

                    return arObjRes;
                }
            }
            /// <summary>
            /// Конструктор - основной с параметрами
            /// </summary>
            /// <param name="owner">Объект - владелец элемента очереди</param>
            /// <param name="obj">Объект, добавивший событие в очередь</param>
            /// <param name="objPars">Массив аргументов события</param>
            public ItemQueue(HHandler owner, IDataHost obj, object[] objPars)
                : this(obj, objPars)
            {
                _owner = owner;
            }
            /// <summary>
            /// Конструктор - дполнительный (с параметрами)
            /// </summary>
            /// <param name="obj">Объект, добавивший событие в очередь</param>
            /// <param name="objPars">Массив аргументов события</param>
            public ItemQueue(IDataHost obj, object[] objPars)
            {
                m_dataHostRecieved = obj;
                object []pars = (objPars as object[])[0] as object[];
                m_states = new List <int>();
                m_pars = new object[pars.Length];
                object []par;
                int i = -1
                    , j = -1;

                for (i = 0; i < pars.Length; i ++)
                {
                    par = pars[i] as object [];
                    //Состояние для обработки
                    m_states.Add ((int)par[0]);
                    //Параметры состояния при обработке
                    if (par.Length > 1)
                    {
                        m_pars[i] = new object[par.Length - 1];
                        for (j = 1; j < par.Length; j ++)
                            (m_pars[i] as object [])[j - 1] = par[j];
                    }
                    else
                        ;
                }
            }
        }
        /// <summary>
        /// Объект для синхронизации изменения списка событий
        /// </summary>
        protected Object m_lockQueue;
        /// <summary>
        /// Объект потока обработки событий очереди
        /// </summary>
        private Thread taskThreadQueue;
        /// <summary>
        /// Признак состояния потока
        /// </summary>
        public volatile int threadQueueIsWorking;
        /// <summary>
        /// Объект синхронизации, разрешающий начало обработки очереди событий
        /// </summary>
        private Semaphore semaQueue;

        private Queue<ItemQueue> m_queue;

        public HHandlerQueue()
            : base ()
        {
            m_queue = new Queue<ItemQueue>();
        }
        /// <summary>
        /// Инициализация
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();
            
            threadQueueIsWorking = -1; //Не активен

            m_lockQueue = new Object();            
        }
        /// <summary>
        /// Активировать очередь обработки событий
        /// </summary>
        /// <param name="active"></param>
        /// <returns></returns>
        public override bool Activate(bool active)
        {
            bool bRes = false;

            bRes = base.Activate(active);

            if (active == true) threadQueueIsWorking++; else ;

            //if (actived == active)
            //{
            //    bRes = false;
            //}
            //else
            //{
            //    actived = active;
            //}

            return bRes;
        }
        /// <summary>
        /// Запустить (сдедать возможным прием/обработку) очередь событий
        /// </summary>
        public override void Start()
        {
            base.Start();

            if (threadQueueIsWorking < 0)
            {
                threadQueueIsWorking = 0;
                taskThreadQueue = new Thread(new ParameterizedThreadStart(ThreadQueue));
                taskThreadQueue.Name = "Обработка очереди для объекта " + this.GetType().AssemblyQualifiedName;
                taskThreadQueue.IsBackground = true;

                semaQueue = new Semaphore(1, 1);

                //InitializeSyncState();
                //Установить в "несигнальное" состояние (т.к. 'SUCCESS' - AutoReset, а исходной состояние "сигнальное")
                WaitOne(INDEX_WAITHANDLE_REASON.SUCCESS, System.Threading.Timeout.Infinite, true);

                semaQueue.WaitOne();
                taskThreadQueue.Start();
            }
            else
                ;
        }
        /// <summary>
        /// Остановить (сделать невозможным прием/обработку) событий
        /// </summary>
        public override void Stop()
        {
            bool joined;
            threadQueueIsWorking = -1;
            //Очисить очередь событий
            ClearStates();
            //Прверить выполнение потоковой функции
            if ((!(taskThreadQueue == null)) && (taskThreadQueue.IsAlive == true))
            {
                //Выход из потоковой функции
                try { semaQueue.Release(1); }
                catch (Exception e)
                {
                    Logging.Logg().Exception(e, "HHandler::Stop () - semaState.Release(1)", Logging.INDEX_MESSAGE.NOT_SET);
                }
                //Ожидать завершения потоковой функции
                joined = taskThreadQueue.Join(Constants.WAIT_TIME_MS);
                //Проверить корректное завершение потоковой функции
                if (joined == false)
                    //Завершить аварийно потоковую функцию
                    taskThreadQueue.Abort();
                else
                    ;
            }
            else ;
            
            base.Stop();
        }
        /// <summary>
        /// Добавить объект в очередь обработки событий
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="pars"></param>
        public void Push(IDataHost obj, object []pars)
        {
            lock (m_lockQueue)
            {
                m_queue.Enqueue(new ItemQueue(this as HHandler, obj, pars));
                //Если этот объект единственный - начать обработку
                if (m_queue.Count == 1)
                    semaQueue.Release(1);
                else
                    ; //Если нет - обработка уже производится...
            }
        }
        /// <summary>
        /// Добавить все состояния объекта очереди событий
        /// </summary>
        /// <param name="dataHost">Объект очереди событий</param>
        /// <returns>Результат выполнения функции</returns>
        private int addStates(ItemQueue itemQueue)
        {
            int iRes = 0;

            foreach (int state in itemQueue.m_states)
                AddState(state);

            return iRes;
        }
        /// <summary>
        /// Возвратить объект очереди событий не удаляя его
        /// </summary>
        protected ItemQueue Peek { get { return m_queue.Peek (); } }
        /// <summary>
        /// Потоковая функция очереди обработки объектов с событиями
        /// </summary>
        /// <param name="par"></param>
        private void ThreadQueue(object par)
        {
            bool bRes = false;
            ItemQueue itemQueue = null;

            while (!(threadQueueIsWorking < 0))
            {
                bRes = false;
                //Ожидать когда появятся объекты для обработки
                bRes = semaQueue.WaitOne();

                while (true)
                {
                    lock (m_lockQueue)
                    {
                        if (m_queue.Count == 0)
                            //Прерват, если обработаны все объекты
                            break;
                        else
                            ;
                    }
                    //Получить объект очереди событий
                    itemQueue = Peek;

                    lock (m_lockState)
                    {
                        //Очистить все состояния
                        ClearStates ();
                        //Добавить все состояния в родительский класс
                        addStates(itemQueue);
                    }

                    //Обработать все состояния
                    Run (@"HHandler::ThreadQueue ()");

                    //Ожидать обработки всех состояний
                    WaitOne(INDEX_WAITHANDLE_REASON.SUCCESS, System.Threading.Timeout.Infinite, true);

                    lock (m_lockQueue)
                    {
                        //Удалить объект очереди событий (обработанный)
                        m_queue.Dequeue();
                    }
                }
            }
            //Освободить ресурс ядра ОС
            //??? "везде" 'true'
            if (bRes == false)
                try
                {
                    semaQueue.Release(1);
                }
                catch (Exception e)
                { //System.Threading.SemaphoreFullException
                    Logging.Logg().Exception(e, "HHandler::ThreadQueue () - semaState.Release(1)", Logging.INDEX_MESSAGE.NOT_SET);
                }
            else
                ;
        }
    }
}
