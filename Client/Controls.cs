using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    class Controls: INotifyPropertyChanged
    {
        public string show;//显示
        public event PropertyChangedEventHandler PropertyChanged;
        public string Show
        {
            get { return show; }
            set
            {
                show = value;
                PropertyChanged(this, new PropertyChangedEventArgs("Show"));
            }
        }
    }
}
