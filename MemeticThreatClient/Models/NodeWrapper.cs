using MemeticThreatClient.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemeticThreatClient.Models
{
    internal class NodeWrapper : Node<IDataInstance>
    {
        //
        public static ISelectedItemObserver _observer;
        public NodeWrapper() : base() { }
        public NodeWrapper(IDataInstance value) : base(value) { }
        public string ValueString
        {
            get => Value.Name;
        }

        private bool _IsSelected;
        public bool IsSelected
        {
            get => _IsSelected;
            set
            {
                if (_IsSelected != value)
                {
                    _IsSelected = value;
                    if (_IsSelected)
                        _observer.SelectedItem = this.Value;
                    else
                        _observer.SelectedItem = null;
                }
            }
        }
    }
}
