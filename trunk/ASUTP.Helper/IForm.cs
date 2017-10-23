using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASUTP.Helper {
    public interface IFormMainBase {
        void Close (bool bForce);
    }

    public interface IFormWait {
        void StopWaitForm (bool bExit = false);
    }
}
