using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemeticThreatClient.Data
{
    public enum ViewType
    {
        Main,
        Auth,
        Reg
    }
    internal interface IMainWindowCodeBehind
    {
        bool Authorized {  get; set; }
        void LoadView(ViewType viewType);
    }
}
