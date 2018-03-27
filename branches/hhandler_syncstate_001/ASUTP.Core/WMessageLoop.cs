//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace HClassLibrary
//{
//    interface IWMessageLoop
//    {
//        void ThreadStartedProc(object sender, MessagePacket msg);

//        void ThreadMessageProc(object sender, MessagePacket msg);

//        void ThreadClosingProc(object sender, MessagePacket msg);
//    }

//    class WMessageLoopException : Exception
//    {
//        public WMessageLoopException(string mesExcp, Exception innerExcp)
//            : base (mesExcp, innerExcp)
//        {
//        }
//    }

//    class MessagePacket
//    {
//    }

//    class WMessageLoop : IWMessageLoop
//    {
//        private delegate void ThreadStartedEventHandler(object sender, MessagePacket msg);
//        private delegate void ThreadMessageEventHandler(object sender, MessagePacket msg);
//        private delegate void ThreadClosingEventHandler(object sender, MessagePacket msg);

//        private event ThreadStartedEventHandler OnThreadStartedEvent;
//        private event ThreadMessageEventHandler OnThreadMessageEvent;
//        private event ThreadClosingEventHandler OnThreadClosingEvent;

//        private IWMessageLoop consumer;

//        public WMessageLoop(object consumer)
//        {
//            try {
//                this.consumer = (IWMessageLoop)consumer;
//            } catch (Exception exc) {
//                WMessageLoopException loopExc = null;
//                loopExc = new WMessageLoopException("IMessageLoop interface must be implemented", exc);
//                throw (loopExc);
//            }
//            OnThreadStartedEvent = new ThreadStartedEventHandler(this.consumer.ThreadStartedProc);
//            OnThreadMessageEvent = new ThreadMessageEventHandler(this.consumer.ThreadMessageProc);
//            OnThreadClosingEvent = new ThreadClosingEventHandler(this.consumer.ThreadClosingProc);
//        }

//        void IWMessageLoop.ThreadStartedProc(object sender, MessagePacket msg)
//        {
//            throw new NotImplementedException();
//        }

//        void IWMessageLoop.ThreadMessageProc(object sender, MessagePacket msg)
//        {
//            throw new NotImplementedException();
//        }

//        void IWMessageLoop.ThreadClosingProc(object sender, MessagePacket msg)
//        {
//            throw new NotImplementedException();
//        }

//        public void Start()
//        {
//            if (thread == null) {
//                CreateThread();
//            }
//            if (thread.IsAlive == false) {
//                waitOnStart = new AutoResetEvent(false);
//                thread.Start();
//                waitOnStart.WaitOne();
//                waitOnStart.Close();
//                waitOnStart = null;
//            } else {
//                MessageLoopException exc = null;
//                exc = new MessageLoopException("Thread " + this.Name + " is already running.");
//                throw (exc);
//            }
//        }

//        public void SendMessage(MessagePacket msg)
//        {
//            CheckIdValue(msg);

//            if (msg.MsgId == MessageId.MsgQuit) {
//                PostMessage(msg);
//            } else {
//                lock (msgList) {
//                    msgList.Insert(0, msg);
//                }
//                waitOnSend.WaitOne();
//            }
//        }

//        public void PostMessage(MessagePacket msg)
//        {
//            CheckIdValue(msg);
//            lock (msgList) {
//                msgList.Add(msg);
//            }
//            if (msg.MsgId == MessageId.MsgQuit) {
//                thread.Join();
//                thread = null;
//            }
//        }

//        private void ThreadMessageLoop()
//        {
//            waitOnStart.Set();
//            DispatchStart();
//            while (true) {
//                MessagePacket msg = null;
//                msg = GetMessage();
//                if (msg != null) {
//                    if (msg.MsgId == MessageId.MsgQuit) {
//                        break;
//                    } else {
//                        DispatchMessage(msg);
//                        waitOnSend.Set();
//                    }
//                }
//                //normally set to 30 - to demonstrate send messages 
//                Thread.Sleep(300);
//            }
//            DispatchClosing();
//        }
//    }
//}
