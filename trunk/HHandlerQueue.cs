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
            public int[] m_states;
            public object[] m_pars;

            public HDataHost(IDataHost obj, object []objPars)
            {
                m_objRecieved = obj;
                object []pars = (objPars as object[])[0] as object[];
                m_states = new int[pars.Length];
                object []par;
                int i = -1
                    , j = -1;

                for (i = 0; i < pars.Length; i ++)
                {
                    par = pars[i] as object [];
                    //Состояние для обработки
                    m_states[i] = (int)par[0];
                    //Параметры состояния при обработке
                    if (par.Length > 1)
                    {
                        m_pars = new object[par.Length - 1];
                        for (j = 1; j < par.Length; j ++)
                            m_pars[j - 1] = par[j];
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

        public void Push(IDataHost obj, object []pars)
        {
            lock (m_lockQueue)
            {
                m_queue.Enqueue(new HDataHost(obj, pars));

                if (m_queue.Count == 1)
                    semaQueue.Release(1);
                else
                    ;
            }
        }

        public HDataHost Peek { get { return m_queue.Peek (); } }

        private void ThreadQueue(object par)
        {
            bool bRes = false;
            HDataHost dataHost = null;

            while (!(threadQueueIsWorking < 0))
            {
                bRes = false;
                bRes = semaQueue.WaitOne();

                while (true)
                {
                    lock (m_lockQueue)
                    {
                        if (m_queue.Count == 0)
                            break;
                        else
                            ;
                    }

                    dataHost = m_queue.Peek ();

                    lock (m_lockState)
                    {
                        ClearStates ();
                        //Добавить все состояния
                        foreach (int state in dataHost.m_states)
                            AddState (state);
                    }

                    //Обработать все состояния
                    Run (@"HHandler::ThreadQueue ()");

                    //Ожидать обработки всех состояний
                    m_waitHandleState[(int)INDEX_WAITHANDLE_REASON.SUCCESS].WaitOne (System.Threading.Timeout.Infinite);

                    lock (m_lockQueue)
                    {
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
