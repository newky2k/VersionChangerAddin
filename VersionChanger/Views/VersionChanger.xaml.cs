using DSoft.VersionChanger.ViewModel;
using EnvDTE;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace DSoft.VersionChanger.Views
{
    /// <summary>
    /// Interaction logic for VersionChanger.xaml
    /// </summary>
    public partial class VersionChanger : DialogWindow
    {
        private ProjectViewModel _viewModel;

        public VersionChanger(DTE applicationObject)
        {
            InitializeComponent();

            _viewModel = new ProjectViewModel(applicationObject);

            this.DataContext = _viewModel;

			_viewModel.LoadingProgressUpdated += MViewModel_LoadingProgressUpdated;

            OnUseSemVerChecked(this, null);
            OnUseSeperateVersionsChanged(this, null);
        }

		private void MViewModel_LoadingProgressUpdated(object sender, EventArgs e)
		{
            Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { this.UpdateLayout(); }));

        }

        private void OnBeginClicked(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                _viewModel.ProcessUpdates();

                this.DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Version Changer");
            }
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {

        }

        private void OnToggleAllClicked(object sender, RoutedEventArgs e)
        {
            _viewModel.SelectAll = !_viewModel.SelectAll;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _viewModel.IsBusy = true;

            Task.Run(() =>
            {
                ThreadHelper.JoinableTaskFactory.Run(async delegate {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                    if (_viewModel.IsLoaded == false)
                    {
                        _viewModel.LoadProjects();
                    }

                    if (_viewModel.Items.Count == 0 && _viewModel.Errors.Count == 0)
                    {
                        MessageBox.Show("There were no compatible projects found in the solution");

                        this.DialogResult = true;
                    }
                });

                
            });
           
        }

        private void FilterClick(object sender, RoutedEventArgs e)
        {
            _viewModel.FilterProjects();
        }

        private void OnUseSemVerChecked(object sender, RoutedEventArgs e)
        {
            hdrVersionSuffix.Visibility = _viewModel.ShowSemVer ? Visibility.Visible : Visibility.Hidden;
        }

		private void OnClickLogo(object sender, RoutedEventArgs e)
		{
            var url = "https://www.lodatek.com";

            System.Diagnostics.Process.Start(url);
		}

		private void OnUseSeperateVersionsChanged(object sender, RoutedEventArgs e)
		{
            hdrFileVersion.Visibility = _viewModel.SeparateVersions ? Visibility.Visible : Visibility.Hidden;

        }
	}
}
