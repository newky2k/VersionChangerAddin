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
        private ProjectViewModel mViewModel;

        private const float heightShort = 500;
        private const float heightTall = 500;

        public VersionChanger(DTE applicationObject)
        {
            InitializeComponent();

            mViewModel = new ProjectViewModel(applicationObject);

            this.DataContext = mViewModel;

			mViewModel.LoadingProgressUpdated += MViewModel_LoadingProgressUpdated;

            OnUseSemVerChecked(this, null);
            OnUseSeperateVersionsChanged(this, null);
        }

		private void MViewModel_LoadingProgressUpdated(object sender, EventArgs e)
		{
            Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { this.UpdateLayout(); }));

   //         Dispatcher.Invoke((Action)(() =>
			//{
   //             txtProjectsLoading.Text = mViewModel.LoadingProjectsText;
   //         }));

			//ThreadHelper.JoinableTaskFactory.Run(async delegate {
   //             await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

   //             await Task.Delay(1000);

               
                
   //         });

        }

        private void OnBeginClicked(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                mViewModel.ProcessUpdates();

                this.DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "DSoft - Version Changer");
            }
        }

        private void edtVersion_PreviewKeyDown(object sender, KeyEventArgs e)
        {

        }

        private void OnToggleAllClicked(object sender, RoutedEventArgs e)
        {
            mViewModel.SelectAll = !mViewModel.SelectAll;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            mViewModel.IsBusy = true;

			System.Threading.Tasks.Task.Run(() =>
            {
                ThreadHelper.JoinableTaskFactory.Run(async delegate {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                    if (mViewModel.IsLoaded == false)
                    {
                        mViewModel.LoadProjects();
                    }

                    if (mViewModel.Items.Count == 0 && mViewModel.Errors.Count == 0)
                    {
                        MessageBox.Show("There were no compatible projects found in the solution");

                        this.DialogResult = true;
                    }
                });

                
            });
           
        }

        private void FilterClick(object sender, RoutedEventArgs e)
        {
            mViewModel.FilterProjects();
        }

        private void OnUseSemVerChecked(object sender, RoutedEventArgs e)
        {
            hdrVersionSuffix.Visibility = mViewModel.ShowSemVer ? Visibility.Visible : Visibility.Hidden;
        }

		private void OnClickCopidal(object sender, RoutedEventArgs e)
		{
            var url = "https://www.copidal.com";

            System.Diagnostics.Process.Start(url);
		}

		private void OnUseSeperateVersionsChanged(object sender, RoutedEventArgs e)
		{
            hdrFileVersion.Visibility = mViewModel.SeparateVersions ? Visibility.Visible : Visibility.Hidden;

        }
	}
}
