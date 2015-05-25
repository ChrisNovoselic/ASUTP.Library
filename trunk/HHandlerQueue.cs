using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;

namespace HClassLibrary
{
    public abstract class HHandlerQueue : HHandler
    {
        public class HDataHost
        {
            public IDataHost m_objRecieved;
            public List<int> m_states;
            private object[] m_pars;
            public object [] Pars (int state) { return (m_pars as object [])[m_states.IndexOf (state)] as object []; }

            public HDataHost(IDataHost obj, object []objPars)
            {
                m_objRecieved = obj;
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

        private Queue<HDataHost> m_queue;

        public HHandlerQueue()
            : base ()
        {
            m_queue = new Queue<HDataHost>();
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
        
        public override void Start()
        {
            base.Start();

            if (threadQueueIsWorking < 0)
            {
                threadQueueIsWorking = 0;
                taskThreadQueue = new Thread(new ParameterizedThreadStart(ThreadQueue));
                taskThreadQueue.Name = "Обработка очереди для объекта " + this.GetType().AssemblyQualifiedName;
                taskThreadQueue.IsBackground = true;
                taskThreadQueue.CurrentCulture =
                taskThreadQueue.CurrentUICulture =
                    ProgramBase.ss_MainCultureInfo;

                semaQueue = new Semaphore(1, 1);

                //InitializeSyncState();
                //Установить в "несигнальное" состояние
                m_waitHandleState[(int)INDEX_WAITHANDLE_REASON.SUCCESS].WaitOne(System.Threading.Timeout.Infinite);

                semaQueue.WaitOne();
                taskThreadQueue.Start();
            }
            else
                ;
        }

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
                    Logging.Logg().Exception(e, Logging.INDEX_MESSAGE.NOT_SET, "HHandler::Stop () - semaState.Release(1)");
                }
                //Ожидать завершения потоковой функции
                joined = taskThreadQueue.Join(666);
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
                m_queue.Enqueue(new HDataHost(obj, pars));
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
        private int addStates(HDataHost dataHost)
        {
            int iRes = 0;
            
            foreach (int state in dataHost.m_states)
                AddState(state);

            return iRes;
        }
        /// <summary>
        /// Возвратить объект очереди событий не удаляя его
        /// </summary>
        public HDataHost Peek { get { return m_queue.Peek (); } }
        /// <summary>
        /// Потоковая функция очереди обработки объектов с событиями
        /// </summary>
        /// <param name="par"></param>
        private void ThreadQueue(object par)
        {
            bool bRes = false;
            HDataHost dataHost = null;

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
                    dataHost = Peek;

                    lock (m_lockState)
                    {
                        //Очистить все состояния
                        ClearStates ();
                        //Добавить все состояния
                        addStates (dataHost);
                    }

                    //Обработать все состояния
                    Run (@"HHandler::ThreadQueue ()");

                    //Ожидать обработки всех состояний
                    m_waitHandleState[(int)INDEX_WAITHANDLE_REASON.SUCCESS].WaitOne (System.Threading.Timeout.Infinite);

                    lock (m_lockQueue)
                    {
                        //Удалить объект очереди событий (обработанный)
                        m_queue.Dequeue();
                    }
                }
            }
            //Освободить ресурс ядра ОС
            if (bRes == true)
                try
                {
                    semaQueue.Release(1);
                }
                catch (Exception e)
                { //System.Threading.SemaphoreFullException
                    Logging.Logg().Exception(e, Logging.INDEX_MESSAGE.NOT_SET, "HHandler::ThreadQueue () - semaState.Release(1)");
                }
            else
                ;
        }
    }
}
