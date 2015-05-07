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
        /// Объект для синхронизации изменения списка состояний
        /// </summary>
        protected Object m_lockState;

        protected Thread taskThread;
        protected Semaphore semaState;
        public enum INDEX_WAITHANDLE_REASON { SUCCESS, ERROR, BREAK, COUNT_INDEX_WAITHANDLE_REASON }
        protected WaitHandle[] m_waitHandleState;
        //protected AutoResetEvent evStateEnd;
        public volatile int threadIsWorking;
        protected volatile bool newState;
        protected volatile List<int /*StatesMachine*/> states;

        private bool actived;
        public bool m_bIsActive { get { return actived; } }

        public HHandler()
        {
            Initialize ();
        }

        protected virtual void Initialize()
        {
            actived = false;
            threadIsWorking = -1;

            m_lockState = new Object();

            states = new List<int /*StatesMachine*/>();
        }

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

        protected virtual void InitializeSyncState()
        {
            if (m_waitHandleState == null)
                m_waitHandleState = new WaitHandle[1];
            else
                ;

            m_waitHandleState[(int)INDEX_WAITHANDLE_REASON.SUCCESS] = new AutoResetEvent(true);
        }

        public virtual void Start()
        {
            Thread.CurrentThread.CurrentCulture =
            Thread.CurrentThread.CurrentUICulture =
                ProgramBase.ss_MainCultureInfo;

            if (threadIsWorking < 0)
            {
                threadIsWorking = 0;
                taskThread = new Thread(new ParameterizedThreadStart(TecView_ThreadFunction));
                taskThread.Name = "Интерфейс к РДГ";
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

        public virtual void Stop()
        {
            bool joined;
            threadIsWorking = -1;

            ClearStates();

            if ((!(taskThread == null)) && taskThread.IsAlive)
            {
                try { semaState.Release(1); }
                catch (Exception e)
                {
                    Logging.Logg().Exception(e, Logging.INDEX_MESSAGE.NOT_SET, "HAdmin::StopThreadSourceData () - semaState.Release(1)");
                }

                joined = taskThread.Join(666);
                if (joined == false)
                    taskThread.Abort();
                else
                    ;
            }
            else ;
        }

        public virtual void ClearStates()
        {
            newState = true;
            states.Clear();
        }
        
        public abstract void ClearValues();

        protected abstract int StateCheckResponse(int state, out bool error, out object outobj);

        protected abstract int StateRequest(int state);

        protected abstract int StateResponse(int state, object obj);

        protected abstract void StateErrors(int state, int req, int res);

        protected abstract void StateWarnings(int state, int req, int res);

        private void TecView_ThreadFunction(object data)
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
                    Logging.Logg().Exception(e, Logging.INDEX_MESSAGE.NOT_SET, "HAdmin::TecView_ThreadFunction () - semaState.Release(1)");
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
                Logging.Logg().Exception(e, Logging.INDEX_MESSAGE.NOT_SET, "TecView_ThreadFunction () - m_waitHandleState[0]).Set()");
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
