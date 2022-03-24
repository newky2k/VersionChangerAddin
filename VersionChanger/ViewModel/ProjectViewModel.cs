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
        private String mVersion;
        private String mFileVersion;
        private Solution mCurrentSolution;
        private bool mSeparateVersions = false;
        private bool isOs;
        private bool showAndroid;
        private string cocoaShortVersion;
        private DTE mApplication;
        private string androidBuild;
        private bool selectAll = true;
        private bool mUpdateClickOnce;
        private bool forceSemVer;
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
        #endregion

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
                if (mVersion == null)
                {
                    var highVersion = Items.HighestVersion;


                    mVersion = (highVersion != null) ? highVersion.ToString() : "1.0.0.0";
                }

                return mVersion;
            }
            set
            {
                mVersion = value;

                Version outVersion = null;

                if (System.Version.TryParse(mVersion, out outVersion))
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
                        if (!System.Version.TryParse(mVersion, out outVersion))
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
                if (!mSeparateVersions)
                    return String.Empty;

                if (mFileVersion == null)
                {
                    var highVersion = Items.HighestFileVersion;


                    mFileVersion = (highVersion != null) ? highVersion.ToString() : "1.0.0.0";
                }

                return mFileVersion;
            }
            set
            {
                mFileVersion = value;

                PropertyDidChange("FileVersion");
            }
        }

        public bool UpdateClickOnce
        {
            get { return mUpdateClickOnce; }
            set
            {
                mUpdateClickOnce = value;
                SettingsControl.SetBooleanValue(value, "UpdateClickOnce");
                PropertyDidChange("UpdateClickOnce");

            }
        }

        public bool SeparateVersions
        {
            get
            {
                return mSeparateVersions;
            }
            set
            {
                if (mSeparateVersions != value)
                {
                    mSeparateVersions = value;
                    SettingsControl.SetBooleanValue(value, "SeparateVersions");
                    PropertyDidChange("SeparateVersions");
                    LoadAssFileVersion();
                }
            }
        }

        public bool ForceSemVer
        {
            get { return forceSemVer; }
            set
            {
                forceSemVer = value;
                SettingsControl.SetBooleanValue(value, "ForceSemVer");
                PropertyDidChange("ForceSemVer");
                PropertyDidChange("ShowRevision");
                PropertyDidChange("ShowSemVer");
            }
        }

        public bool ShowSemVer
        {
            get { return forceSemVer; }
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
            get { return !forceSemVer; }
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
        #endregion

        #region Constructor

        public ProjectViewModel(DTE application)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            mApplication = application;
            mCurrentSolution = application.Solution;

            this.Items = new ProjectVersionCollection();

            mSeparateVersions = SettingsControl.GetBooleanValue("SeparateVersions");
            mUpdateClickOnce = SettingsControl.GetBooleanValue("UpdateClickOnce");
            forceSemVer = SettingsControl.GetBooleanValue("ForceSemVer");
            _updateNuget = SettingsControl.GetBooleanValue("UpdateNuget");

            //LoadProjects();
        }
        #endregion

        #region Methods
        public void LoadProjects()
        {
			try
			{
                ThreadHelper.ThrowIfNotOnUIThread();

               

                using (var solutionProcessor = new SolutionProcessor(mCurrentSolution))
                {
                    var projVers = solutionProcessor.BuildVersions(mCurrentSolution);
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
                var theVersion = (forceSemVer) ? $"{AssemblyMajor ?? "0"}.{AssemblyMinor ?? "0"}.{AssemblyBuild ?? "0"}" : this.AssemblyVersion;

                newVersion = new Version(theVersion);

                var newVersionValue = (newVersion.Revision == -1) ? $"{newVersion.Major}.{newVersion.Minor}.{newVersion.Build}.0" : newVersion.ToString();

                if (mSeparateVersions)
                {
                    var vers = (forceSemVer) ? $"{AssemblyFileMajor ?? "0"}.{AssemblyFileMinor ?? "0"}.{AssemblyFileBuild ?? "0"}" : this.FileVersion;

                    fileVersion = new Version(vers);
                }


                var _serviceProvider = new ServiceProvider(mApplication as Microsoft.VisualStudio.OLE.Interop.IServiceProvider);
                var _sln = (IVsSolution)_serviceProvider.GetService(typeof(SVsSolution));

                if (_sln == null)
                    throw new Exception("Unable to find the solution");

                using (var solutionProcessor = new SolutionProcessor(mCurrentSolution))
                {
                    foreach (ProjectVersion ver in Items)
                    {
                        if (ver.Update)
                        {
                            if (ver.IsNewStyleProject == true)
                            {
                                solutionProcessor.UpdateProject(ver.RealProject, newVersion, fileVersion, forceSemVer ? preRelease : null);

                            }
                            else
                            {
                                solutionProcessor.UpdateFile(ver.ProjectItem, newVersion, fileVersion, forceSemVer ? preRelease : null);
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
                                        var stP = mApplication.Solution.Properties.Item("StartupProject").Value;

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
                                            mApplication.Solution.Properties.Item("StartupProject").Value = stPName;

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
            if (mSeparateVersions == true)
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