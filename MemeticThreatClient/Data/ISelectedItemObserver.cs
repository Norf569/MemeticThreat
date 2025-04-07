using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemeticThreatClient.Data
{
    internal interface ISelectedItemObserver
    {
        object SelectedItem { get; set; }
    }
}
