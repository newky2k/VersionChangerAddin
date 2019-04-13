using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using DSoft.VersionChanger.Controls;
using EnvDTE;
using MahApps.Metro;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Settings;
using Task = System.Threading.Tasks.Task;

namespace DSoft.VersionChanger
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class VersionChanger
    {
    
        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly VersionChangerPackage package;

        private readonly DTE applicationObject;

        /// <summary>
        /// Initializes a new instance of the <see cref="VersionChanger"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private VersionChanger(VersionChangerPackage package, OleMenuCommandService commandService, DTE application)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(GuidList.guidVersionChangerCmdSet, (int)PkgCmdIDList.cmdshowVersionChanger);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);

            applicationObject = application;
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static VersionChanger Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(VersionChangerPackage package)
        {
            // Switch to the main thread - the call to AddCommand in VersionChanger's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var applicationObject = (DTE)await package.GetServiceAsync(typeof(DTE));

            OleMenuCommandService commandService = await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;
            Instance = new VersionChanger(package, commandService, applicationObject);

            if (!SettingsControl.IsLoaded)
                SettingsControl.SettingsManager = new ShellSettingsManager(package);

            
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            //    string message = string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", this.GetType().FullName);
            //    string title = "VersionChanger";

            //    // Show a message box to prove we were here
            //VsShellUtilities.ShowMessageBox(
            //    this.package,
            //    message,
            //    title,
            //    OLEMSGICON.OLEMSGICON_INFO,
            //    OLEMSGBUTTON.OLEMSGBUTTON_OK,
            //    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

            //IVsUIShell uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));

            try
            {


                if (applicationObject?.Solution.Count != 0)
                {
                    var vcForm = new Views.VersionChanger(applicationObject);

                    vcForm.ShowModal();

                }
                else
                {
                    throw new Exception("Please open a solution first");
                }

            }
            catch (Exception ex)
            {
                VsShellUtilities.ShowMessageBox(
                this.package,
                ex.Message,
                "DSoft - Version Changer",
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }


    }
}
