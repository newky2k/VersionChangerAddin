using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSoft.VersionChanger.Data
{
    public class NotifyableObject : INotifyPropertyChanged, INotifyDataErrorInfo
    {
        private Dictionary<string, string> _errors = new Dictionary<string, string>();

        public Dictionary<string,string> Errors
        {
            get { return _errors; }
            set { _errors = value; }
        }


        public bool HasErrors => Errors.Keys.Count() > 0;

        protected virtual void OnPropertyChanged(string property)
        {

            PropertyChanged(this, new PropertyChangedEventArgs(property));
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged = delegate { };

        protected void PropertyDidChange(string prop)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
            }
        }

        protected void ErrorDidChange(string prop)
        {
            ErrorsChanged(this, new DataErrorsChangedEventArgs(prop));
        }

        public void UpdateOrAddError(string prop, string value)
        {
            if (_errors.ContainsKey(prop))
            {
                if (value == null)
                    _errors.Remove(prop);
                else
                    _errors[prop] = value;
            }
            else
            {
                if (value != null)
                {
                    _errors.Add(prop, value);
                }
            }
        }
        public IEnumerable GetErrors(string propertyName)
        {
            if (_errors.ContainsKey(propertyName))
            {
                return new List<string>() { _errors[propertyName] };
            }

            return null;
        }
    }


}
