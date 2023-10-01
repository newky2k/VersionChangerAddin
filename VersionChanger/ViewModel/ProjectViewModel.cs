using DSoft.VersionChanger.Controls;
using DSoft.VersionChanger.Data;
using DSoft.VersionChanger.Extensions;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Xml.Linq;

namespace DSoft.VersionChanger.ViewModel
{
    public class ProjectViewModel : BaseViewModel
    {
        #region Fields
        private ProjectVersionCollection mItems;
        private string _version;
        private string _fileVersion;
        private Solution _currentSolution;
        private bool _separateVersions = false;
        private bool isOs;
        private bool showAndroid;
        private string cocoaShortVersion;
        private DTE _application;
        private string androidBuild;
        private bool selectAll = true;
        private bool _updateClickOnce;
        private bool _forceSemVer;
        private string preRelease;
        private bool _updateNuget;
        private bool _showUnloadedWarning;
        private List<FailedProject> _failedProjects;

        private string _assemblyMajor;
        private string _assemblyRevision;
        private string _assemblyMinor;
        private string _assemblyBuild;
        private string _filter;

        private string _assemblyFileMajor;
        private string _assemblyFileRevision;
        private string _assemblyFileMinor;
        private string _assemblyFileBuild;

        private AssemblyVersionOptions _versionOptions = new AssemblyVersionOptions();

        private int _totalProjects;
        private int _currentProject;
        private string _currentProjectName;
        #endregion

        public event EventHandler LoadingProgressUpdated = delegate { };

        #region Properties

        #region AssemblyVersion




        public string AssemblyMinor
        {
            get { return _assemblyMinor; }
            set
            {

                _assemblyMinor = (string.IsNullOrWhiteSpace(value)) ? "0" : value;

                PropertyDidChange(nameof(AssemblyMinor));

                RecalculateVersion();
            }
        }


        public string AssemblyBuild
        {
            get { return _assemblyBuild; }
            set
            { _assemblyBuild = (string.IsNullOrWhiteSpace(value)) ? "0" : value; PropertyDidChange(nameof(AssemblyBuild)); RecalculateVersion(); }
        }


        public string AssesmblyRevision
        {
            get { return _assemblyRevision; }
            set { _assemblyRevision = (string.IsNullOrWhiteSpace(value)) ? "0" : value; PropertyDidChange(nameof(AssesmblyRevision)); RecalculateVersion(); }
        }


        public string AssemblyMajor
        {
            get { return _assemblyMajor; }
            set { _assemblyMajor = (string.IsNullOrWhiteSpace(value)) ? "0" : value; PropertyDidChange(nameof(AssemblyMajor)); RecalculateVersion(); RecalculateVersion(); }
        }

        public string AssemblyFileMinor
        {
            get { return _assemblyFileMinor; }
            set { _assemblyFileMinor = (string.IsNullOrWhiteSpace(value)) ? "0" : value; PropertyDidChange(nameof(AssemblyFileMinor)); RecalculateFileVersion(); }
        }


        public string AssemblyFileBuild
        {
            get { return _assemblyFileBuild; }
            set { _assemblyFileBuild = (string.IsNullOrWhiteSpace(value)) ? "0" : value; PropertyDidChange(nameof(AssemblyFileBuild)); RecalculateFileVersion(); }
        }


        public string AssesmblyFileRevision
        {
            get { return _assemblyFileRevision; }
            set { _assemblyFileRevision = (string.IsNullOrWhiteSpace(value)) ? "0" : value; PropertyDidChange(nameof(AssesmblyFileRevision)); RecalculateFileVersion(); }
        }


        public string AssemblyFileMajor
        {
            get { return _assemblyFileMajor; }
            set { _assemblyFileMajor = (string.IsNullOrWhiteSpace(value)) ? "0" : value; PropertyDidChange(nameof(AssemblyFileMajor)); RecalculateFileVersion(); }
        }

        #endregion

        /// <summary>
        /// Gets or sets the items.
        /// </summary>
        /// <value>
        /// The items.
        /// </value>
        public ProjectVersionCollection Items
        {
            get
            {
                this._ItemsView = CollectionViewSource.GetDefaultView(mItems);
                this._ItemsView.Filter = ProjectFilter;
                return mItems;
            }
            set
            {
                mItems = value;

                PropertyDidChange("Items");
            }
        }

        private ICollectionView _ItemsView { get; set; }

        /// <summary>
        /// Gets or sets the new version.
        /// </summary>
        /// <value>
        /// The version.
        /// </value>
        public string AssemblyVersion
        {
            get
            {
                if (_version == null)
                {
                    var highVersion = Items.HighestVersion;


                    _version = (highVersion != null) ? highVersion.ToString() : "1.0.0.0";
                }

                return _version;
            }
            set
            {
                _version = value;

                Version outVersion = null;

                if (System.Version.TryParse(_version, out outVersion))
                {
                    if (ShowIos)
                    {
                        cocoaShortVersion = CocoaAppVersion.ToShortVersion(outVersion);
                        PropertyDidChange("CocoaShortVersion");
                    }

                    if (ShowAndroid)
                    {
                        androidBuild = AndroidAppVersion.ToBuild(outVersion);
                        PropertyDidChange("AndroidBuild");
                    }


                }

                PropertyDidChange(nameof(AssemblyVersion));
                Validate(nameof(AssemblyVersion));
            }
        }

        private void Validate(string propertyName)
        {
            switch (propertyName)
            {
                case nameof(AssemblyVersion):
                    {
                        string msg = null;

                        Version outVersion;
                        if (!System.Version.TryParse(_version, out outVersion))
                        {
                            msg = "Invalid version number";
                        }

                        UpdateOrAddError(nameof(AssemblyVersion), msg);

                    }
                    break;
            }
        }

        public string FileVersion
        {
            get
            {
                if (!_separateVersions)
                    return String.Empty;

                if (_fileVersion == null)
                {
                    var highVersion = Items.HighestFileVersion;


                    _fileVersion = (highVersion != null) ? highVersion.ToString() : "1.0.0.0";
                }

                return _fileVersion;
            }
            set
            {
                _fileVersion = value;

                PropertyDidChange("FileVersion");
            }
        }

        public bool UpdateClickOnce
        {
            get { return _updateClickOnce; }
            set
            {
                _updateClickOnce = value;
                SettingsControl.SetBooleanValue(value, "UpdateClickOnce");
                PropertyDidChange("UpdateClickOnce");

            }
        }

        public bool SeparateVersions
        {
            get
            {
                return _separateVersions;
            }
            set
            {
                if (_separateVersions != value)
                {
                    _separateVersions = value;
                    SettingsControl.SetBooleanValue(value, "SeparateVersions");
                    PropertyDidChange("SeparateVersions");
                    LoadAssFileVersion();
                }
            }
        }

        public bool ForceSemVer
        {
            get { return _forceSemVer; }
            set
            {
                _forceSemVer = value;
                SettingsControl.SetBooleanValue(value, "ForceSemVer");

                if (_forceSemVer == true)
                    EnableRevision = false;

                PropertyDidChange("ForceSemVer");
                PropertyDidChange("ShowRevision");
                PropertyDidChange("ShowSemVer");
                PropertyDidChange("EnableRevisionEnabled");
            }
        }

        public bool ShowSemVer
        {
            get { return _forceSemVer; }
        }

        public bool ShowIos
        {
            get { return isOs; }
            set { isOs = value; PropertyDidChange("ShowIos"); }
        }

        public bool ShowUnloadedWarning
        {
            get { return _showUnloadedWarning; }
            set { _showUnloadedWarning = value; PropertyDidChange(nameof(ShowUnloadedWarning)); }
        }

        public bool ShowProjectErrorWarning
        {
            get
            {
                return FailedProjects.Count > 0;
            }
        }

        public string ProjectErrors
        {
            get
            {
                if (FailedProjects.Count == 0)
                    return string.Empty;

                var strBuilder = new StringBuilder();

                foreach (var fP in FailedProjects)
                {
                    strBuilder.AppendLine($"{fP.Name} - {fP.ErrorMessage}");
                }

                return strBuilder.ToString();
            }
        }

        public List<FailedProject> FailedProjects
        {
            get
            {
                if (_failedProjects == null)
                    _failedProjects = new List<FailedProject>();

                return _failedProjects;
            }
            set { _failedProjects = value; }
        }

        public bool SelectAll
        {
            get { return selectAll; }
            set
            {
                selectAll = value;
                PropertyDidChange("SelectAll");

                UpdateSelection();
            }
        }

        private void UpdateSelection()
        {
            foreach (var aItem in Items)
            {
                aItem.Update = selectAll;
            }
        }

        public bool ShowAndroid
        {
            get { return showAndroid; }
            set { showAndroid = value; PropertyDidChange("ShowAndroid"); }
        }

        public string CocoaShortVersion
        {
            get { return cocoaShortVersion; }
            set { cocoaShortVersion = value; PropertyDidChange("CocoaShortVersion"); }
        }

        public string AndroidBuild
        {
            get { return androidBuild; }
            set { androidBuild = value; PropertyDidChange("AndroidBuild"); }
        }

        public string PreRelase
        {
            get { return preRelease; }
            set { preRelease = value; PropertyDidChange("PreRelase"); }
        }

        public bool UpdateNuget
        {
            get { return _updateNuget; }
            set
            {
                _updateNuget = value;
                SettingsControl.SetBooleanValue(value, "UpdateNuget");
                PropertyDidChange("UpdateNuget");

            }
        }

        public bool ShowRevision
        {
            get { return !_forceSemVer; }
        }

        public string Filter
        {
            get { return _filter; }
            set
            {
                if (_filter != value)
                {
                    _filter = value;
                }
            }
        }

        #region Version Bools

        public bool UpdateAssemblyVersion
        {
            get { return _versionOptions.UpdateAssemblyVersion; }
            set
            {
                _versionOptions.UpdateAssemblyVersion = value;
                SettingsControl.SetBooleanValue(value, "UpdateAssemblyVersion");
                PropertyDidChange("UpdateAssemblyVersion");

            }
        }

        public bool UpdateVersionPrefix
        {
            get { return _versionOptions.UpdateAssemblyVersionPrefix; }
            set
            {
                _versionOptions.UpdateAssemblyVersionPrefix = value;
                SettingsControl.SetBooleanValue(value, "UpdateVersionPrefix");
                PropertyDidChange("UpdateVersionPrefix");

            }
        }

        public bool UpdateFileVersion
        {
            get { return _versionOptions.UpdateFileVersion; }
            set
            {
                _versionOptions.UpdateFileVersion = value;
                SettingsControl.SetBooleanValue(value, "UpdateFileVersion");
                PropertyDidChange("UpdateFileVersion");

            }
        }

        public bool UpdatePackageVersion
        {
            get { return _versionOptions.UpdatePackageVersion; }
            set
            {
                _versionOptions.UpdatePackageVersion = value;
                SettingsControl.SetBooleanValue(value, "UpdatePackageVersion");
                PropertyDidChange("UpdatePackageVersion");

            }
        }

        public bool UpdateVersion
        {
            get { return _versionOptions.UpdateVersion; }
            set
            {
                _versionOptions.UpdateVersion = value;
                SettingsControl.SetBooleanValue(value, "UpdateVersion");
                PropertyDidChange("UpdateVersion");

            }
        }

        public bool UpdateInformationalVersion
        {
            get { return _versionOptions.UpdateInformationalVersion; }
            set
            {
                _versionOptions.UpdateInformationalVersion = value;
                SettingsControl.SetBooleanValue(value, "UpdateInformationalVersion");
                PropertyDidChange("UpdateInformationalVersion");

            }
        }

        public bool EnableRevision
        {
            get { return _versionOptions.EnableRevision; }
            set
            {
                _versionOptions.EnableRevision = value;
                SettingsControl.SetBooleanValue(value, "EnableRevision");
                PropertyDidChange("EnableRevision");
            }
        }

        /// <summary>
        /// Enable revision as long as ForceSemVer is not ticked
        /// </summary>
        public bool EnableRevisionEnabled => !ForceSemVer;

        #endregion

        public string LoadingProjectsText
        {
            get { return $"Loading project {CurrentProject} of {TotalProjects}"; }
        }

        public int CurrentProject
        {
            get { return _currentProject; }
            set { _currentProject = value; PropertyDidChange(nameof(CurrentProject)); PropertyDidChange(nameof(LoadingProjectsText)); }
        }

        public string CurrentProjectName
        {
            get { return _currentProjectName; }
            set { _currentProjectName = value; PropertyDidChange(nameof(CurrentProjectName)); }
        }

        public int TotalProjects
        {
            get { return _totalProjects; }
            set { _totalProjects = value; PropertyDidChange(nameof(TotalProjects)); PropertyDidChange(nameof(LoadingProjectsText)); }
        }
        #endregion

        #region Constructor

        public ProjectViewModel(DTE application)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _application = application;
            _currentSolution = application.Solution;

            this.Items = new ProjectVersionCollection();

            _separateVersions = SettingsControl.GetBooleanValue("SeparateVersions");
            _updateClickOnce = SettingsControl.GetBooleanValue("UpdateClickOnce");
            _forceSemVer = SettingsControl.GetBooleanValue("ForceSemVer");
            _updateNuget = SettingsControl.GetBooleanValue("UpdateNuget");
            _versionOptions.EnableRevision = SettingsControl.GetBooleanValue("EnableRevision", true);

            if (_forceSemVer == true)
            {
                _versionOptions.EnableRevision = false;
            }

            _versionOptions.UpdateAssemblyVersion = SettingsControl.GetBooleanValue("UpdateAssemblyVersion", true);
            _versionOptions.UpdateAssemblyVersionPrefix = SettingsControl.GetBooleanValue("UpdateVersionPrefix", true);
            _versionOptions.UpdateFileVersion = SettingsControl.GetBooleanValue("UpdateFileVersion", true);
            _versionOptions.UpdatePackageVersion = SettingsControl.GetBooleanValue("UpdatePackageVersion", true);
            _versionOptions.UpdateVersion = SettingsControl.GetBooleanValue("UpdateVersion", true);
            _versionOptions.UpdateInformationalVersion = SettingsControl.GetBooleanValue("UpdateInformationalVersion", true);

        }

		#endregion

		#region Methods
		public void LoadProjects()
        {
			try
			{
                ThreadHelper.ThrowIfNotOnUIThread();

                

                using (var solutionProcessor = new SolutionProcessor(_currentSolution))
                {

                    solutionProcessor.OnLoadedProjects += (s, e) =>
                    {
                        TotalProjects = e;

                        LoadingProgressUpdated(this, null);
                    };

                    solutionProcessor.OnStartingProject += (s, e) =>
                    {
                        CurrentProject = e.Item1;
                        CurrentProjectName = e.Item2;

                        LoadingProgressUpdated(this, null);
                    };

                    var projVers = solutionProcessor.BuildVersions(_currentSolution);
                    ShowUnloadedWarning = solutionProcessor.DetectedUnloadedProjects;
                    FailedProjects = solutionProcessor.FailedProjects;

                    foreach (var item in projVers.OrderBy(projver => projver.Name))
                    {
                        this.Items.Add(item);
                    }


                    ShowIos = projVers.HasIosMac;
                    ShowAndroid = projVers.HasAndroid;

                    Version outVersion = null;

                    if (System.Version.TryParse(AssemblyVersion, out outVersion))
                    {
                        if (ShowIos)
                        {
                            CocoaShortVersion = CocoaAppVersion.ToShortVersion(outVersion);
                        }

                        if (ShowAndroid)
                        {
                            AndroidBuild = AndroidAppVersion.ToBuild(outVersion);
                        }
                    }
                }


                LoadAssVersion();

                LoadAssFileVersion();

                IsLoaded = true;

                IsBusy = false;
            }
			catch (Exception ex)
			{
                System.Windows.MessageBox.Show(ex.Message, "Error loading projects");
			}

        }

        public void ProcessUpdates()
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                Version newVersion;
                Version fileVersion = null;

                //add support for dynamic build revision values
                var theVersion = (_forceSemVer) ? $"{AssemblyMajor ?? "0"}.{AssemblyMinor ?? "0"}.{AssemblyBuild ?? "0"}" : this.AssemblyVersion;

                newVersion = new Version(theVersion);

                var newVersionValue = (newVersion.Revision == -1) ? $"{newVersion.Major}.{newVersion.Minor}.{newVersion.Build}.0" : newVersion.ToString();

                if (_separateVersions)
                {
                    var vers = (_forceSemVer) ? $"{AssemblyFileMajor ?? "0"}.{AssemblyFileMinor ?? "0"}.{AssemblyFileBuild ?? "0"}" : this.FileVersion;

                    fileVersion = new Version(vers);
                }


                var _serviceProvider = new ServiceProvider(_application as Microsoft.VisualStudio.OLE.Interop.IServiceProvider);
                var _sln = (IVsSolution)_serviceProvider.GetService(typeof(SVsSolution));

                if (_sln == null)
                    throw new Exception("Unable to find the solution");

                using (var solutionProcessor = new SolutionProcessor(_currentSolution))
                {
                    foreach (ProjectVersion ver in Items)
                    {
                        if (ver.Update)
                        {
                            if (ver.IsNewStyleProject == true)
                            {
                                solutionProcessor.UpdateSdkProject(ver.RealProject,_versionOptions, newVersion, fileVersion, _forceSemVer ? preRelease : null);

                            }
                            else
                            {
                                solutionProcessor.UpdateFrameworkProject(ver.ProjectItem, _versionOptions, newVersion, fileVersion, _forceSemVer ? preRelease : null);
                            }


                            if (UpdateClickOnce)
                            {

                                IVsHierarchy hiearachy = null;
                                _sln.GetProjectOfUniqueName(ver.RealProject.FullName, out hiearachy);

                                Guid aGuid;

                                _sln.GetGuidOfProject(hiearachy, out aGuid);

                                IVsBuildPropertyStorage buildPropStorage = (IVsBuildPropertyStorage)hiearachy;

                                string propValue;
                                buildPropStorage.GetPropertyValue("ApplicationVersion", "Debug", (uint)_PersistStorageType.PST_PROJECT_FILE, out propValue);

                                if (!String.IsNullOrWhiteSpace(propValue))
                                {
                                    var xmldoc = XDocument.Load(ver.RealProject.FullName);

                                    XNamespace msbuild = "http://schemas.microsoft.com/developer/msbuild/2003";

                                    bool hasChanged = false;
                                    foreach (var resource in xmldoc.Descendants(msbuild + "ApplicationVersion"))
                                    {
                                        string curVersion = resource.Value;

                                        if (!curVersion.Equals(this.AssemblyVersion))
                                        {
                                            resource.Value = newVersionValue;

                                            hasChanged = true;
                                        }


                                    }

                                    if (hasChanged)
                                    {
                                        var stP = _application.Solution.Properties.Item("StartupProject").Value;

                                        var stPName = ver.RealProject.Name;

                                        var aFileName = ver.RealProject.FullName;

                                        var _sln2 = (IVsSolution4)_serviceProvider.GetService(typeof(SVsSolution));

                                        if (_sln2 == null)
                                            throw new Exception("Unable to access the solution");

                                        _sln2.UnloadProject(aGuid, (uint)_VSProjectUnloadStatus.UNLOADSTATUS_UnloadedByUser);

                                        xmldoc.Save(aFileName);

                                        IVsHierarchy hiearachy2 = null;
                                        _sln.GetProjectOfUniqueName(aFileName, out hiearachy2);

                                        Guid aGuid2;

                                        _sln.GetGuidOfProject(hiearachy2, out aGuid2);

                                        if (aGuid != aGuid2)
                                            Console.WriteLine("");

                                        _sln2.ReloadProject(aGuid);

                                        if (stP.Equals(stPName))
                                        {
                                            _application.Solution.Properties.Item("StartupProject").Value = stPName;

                                        }

                                    }


                                }




                            }

                            if (ver.SecondaryProjectItem != null)
                            {
                                if (ver.IsCocoa == true)
                                {
                                    var secFile = ver.SecondaryProjectItem.FileNames[0];

                                    var aUpdater = new CocoaAppVersion()
                                    {
                                        FilePath = secFile
                                    };

                                    aUpdater.VersionOne = cocoaShortVersion;
                                    aUpdater.VersionTwo = newVersionValue;
                                    aUpdater.Update();

                                }
                                else if (ver.IsAndroid == true)
                                {
                                    var secFile = ver.SecondaryProjectItem.FileNames[0];

                                    var aUpdater = new AndroidAppVersion()
                                    {
                                        FilePath = secFile
                                    };

                                    aUpdater.VersionOne = androidBuild;
                                    aUpdater.VersionTwo = newVersionValue;
                                    aUpdater.Update();
                                }
                                else if (ver.IsUWP == true)
                                {
                                    var secFile = ver.SecondaryProjectItem.FileNames[0];

                                    var uwpUpdater = new UWPVersion()
                                    {
                                        FilePath = secFile,
                                    };

                                    uwpUpdater.VersionOne = $"{Version.Parse(newVersionValue).ToString(3)}.0";
                                    uwpUpdater.Update();

                                }
                            }

                        }
                    }
                }


            }
            catch (Exception ex)
            {
                throw ex;
                //throw new Exception("One of the versions specified is not valid");
            }


        }

        public void FilterProjects()
        {
            PropertyDidChange("Items");
        }

        private void RecalculateVersion()
        {
            AssemblyVersion = $"{AssemblyMajor}.{AssemblyMinor}.{AssemblyBuild}.{AssesmblyRevision}";
        }

        private void RecalculateFileVersion()
        {
            FileVersion = $"{AssemblyFileMajor ?? "0"}.{AssemblyFileMinor ?? "0"}.{AssemblyFileBuild ?? "0"}.{AssesmblyFileRevision ?? "0"}";
        }

        private void LoadAssVersion()
        {
            var assmVersion = new Version(AssemblyVersion);

            AssemblyMajor = assmVersion.Major.ToString();
            AssemblyMinor = assmVersion.Minor.ToString();
            AssemblyBuild = assmVersion.Build.ToString();
            AssesmblyRevision = assmVersion.Revision.ToString();
        }

        private void LoadAssFileVersion()
        {
            if (_separateVersions == true)
            {
                var assmVersion = new Version(FileVersion);

                AssemblyFileMajor = assmVersion.Major.ToString();
                AssemblyFileMinor = assmVersion.Minor.ToString();
                AssemblyFileBuild = assmVersion.Build.ToString();
                AssesmblyFileRevision = assmVersion.Revision.ToString();
            }
            else
            {
                _assemblyFileMajor = string.Empty;
                _assemblyFileMinor = string.Empty;
                _assemblyFileBuild = string.Empty;
                _assemblyFileRevision = string.Empty;

                PropertyDidChange(nameof(AssemblyFileMajor));
                PropertyDidChange(nameof(AssemblyFileMinor));
                PropertyDidChange(nameof(AssemblyFileBuild));
                PropertyDidChange(nameof(AssesmblyFileRevision));
            }
        }

        private bool ProjectFilter(object item)
        {
            ProjectVersion proj = item as ProjectVersion;
            return string.IsNullOrEmpty(Filter) || proj.Name.CaseContains(Filter,StringComparison.InvariantCultureIgnoreCase);
        }
        #endregion

    }
}