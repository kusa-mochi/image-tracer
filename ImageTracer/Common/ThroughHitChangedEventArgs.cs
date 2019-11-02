using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageTracer.Common
{
    public class ThroughHitChangedEventArgs : EventArgs
    {
        public bool NewValue { get; set; }
    }
}
