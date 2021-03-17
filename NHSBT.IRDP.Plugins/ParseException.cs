using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NHSBT.IRDP.Plugins
{
    public class ParseException
    {
        public ParseException(ProxyClasses.Task _task)
        {
            Task = _task;
        }

        public ProxyClasses.Task Task { get; private set; }
    }
}
