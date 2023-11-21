using ControlzEx.Theming;
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

            var backColor = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey);
  
            if (backColor.R * 0.2126 + backColor.G * 0.7152 + backColor.B * 0.0722 < 255 / 2)
            {
                // dark color
                ThemeManager.Current.ChangeTheme(this, "Dark.Green");

                var backBrush = new SolidColorBrush(Color.FromArgb(backColor.A, backColor.R, backColor.G, backColor.B));

                this.Background = backBrush;
            }
            else
            {
                // light color
                ThemeManager.Current.ChangeTheme(this, "Light.Green");

                this.Background = Brushes.WhiteSmoke;
            }  
        }

        private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        // Close
        private void CommandBinding_Executed_Close(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }

        private void MViewModel_LoadingProgressUpdated(object sender, EventArgs e)
		{
#pragma warning disable VSTHRD001 // Avoid legacy thread switching APIs
            Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { this.UpdateLayout(); }));
#pragma warning restore VSTHRD001 // Avoid legacy thread switching APIs
        }

        private void OnBeginClicked(object sender, RoutedEventArgs e)
        {
            _viewModel.IsBusy = true;

#pragma warning disable VSTHRD110 // Observe result of async calls
            System.Threading.Tasks.Task.Run(() =>
            {
                ThreadHelper.JoinableTaskFactory.Run(async delegate {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                    try
                    {
                        _viewModel.ProcessUpdates();

                        this.DialogResult = true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Version Changer");
                    }

                });


            });
#pragma warning restore VSTHRD110 // Observe result of async calls

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

#pragma warning disable VSTHRD110 // Observe result of async calls
            System.Threading.Tasks.Task.Run(() =>
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
#pragma warning restore VSTHRD110 // Observe result of async calls

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
            var url = @"https://github.com/newky2k";

            System.Diagnostics.Process.Start(url);
		}

		private void OnUseSeperateVersionsChanged(object sender, RoutedEventArgs e)
		{
            hdrFileVersion.Visibility = _viewModel.SeparateVersions ? Visibility.Visible : Visibility.Hidden;

        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _viewModel.SaveProjectSelection();
        }
    }
}
