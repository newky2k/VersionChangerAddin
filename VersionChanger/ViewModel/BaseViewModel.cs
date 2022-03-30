using DSoft.VersionChanger.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSoft.VersionChanger.ViewModel
{
    public class BaseViewModel : NotifyableObject
    {
		public bool IsLoaded { get; set; } = false;

		private bool _isBusy;

		public bool IsBusy
		{
			get { return _isBusy; }
			set { _isBusy = value; OnPropertyChanged(nameof(IsBusy)); }
		}

	}
}
