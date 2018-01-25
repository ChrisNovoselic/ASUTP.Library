namespace ASUTP.Helper {
    public interface IHHandler {
        bool Actived
        {
            get;
        }
        int IndexCurState
        {
            get;
        }
        bool IsFirstActivated
        {
            get;
        }
        bool IsStarted
        {
            get;
        }

        bool Activate (bool active);
        void AddState (int state);
        void ClearStates ();
        void Run (string throwMes);
        void Start ();
        void Stop ();
    }
}