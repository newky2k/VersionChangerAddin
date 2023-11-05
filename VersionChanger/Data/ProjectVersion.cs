using EnvDTE;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;

namespace DSoft.VersionChanger.Data
{
    /// <summary>
    /// Project Version information
    /// </summary>
    public class ProjectVersion : NotifyableObject
    {
		#region Fields


		private bool m_Update  = true;
        private string _name;
        private string _path;
        private Version _assemblyVersion;
        private Version _fileVersion;
        private Version _packageVersion;
        private Version _version;
        private Version _versionPrefix;
        private Version _mauiDisplayVersion;
        private Version _mauiAppVersion;

        private string _versionSuffix;
        private string _informationalVersion;
        private bool _isNewStyleProject;
        private string _projectType;

        #endregion

        #region Properties

        /// <summary>
        /// Active version based on assemblyversion, versionprefix or version properties
        /// </summary>
        public Version ActiveVersion
        {
            get
            {
                var assemblyVersion = new List<Version>
                {
                    new Version("0.0.0")
                };

                if (_assemblyVersion != null)
                    assemblyVersion.Add(_assemblyVersion);

                if (_versionPrefix != null)
                    assemblyVersion.Add(_versionPrefix);

                if (_version != null)
                    assemblyVersion.Add(_version);

                assemblyVersion.Sort();
                assemblyVersion.Reverse();

                return assemblyVersion.First();
            }
        }

        /// <summary>
        /// Display the version information for the active version
        /// </summary>
        public string ActiveVersionValue
        {
            get
            {
                var strVersion = "1.0";

                if (ActiveVersion.Major >= 0)
                {
                    strVersion = $"{ActiveVersion.Major}";
                }

                if (ActiveVersion.Minor >= 0)
                {
                    strVersion = $"{strVersion}.{ActiveVersion.Minor}";
                }

                if (ActiveVersion.Build >= 0)
                {
                    strVersion = $"{strVersion}.{ActiveVersion.Build}";
                }

                if (ActiveVersion.Revision >= 0)
                {
                    strVersion = $"{strVersion}.{ActiveVersion.Revision}";
                }

                return strVersion;
            }
        }

        public bool IsNewStyleProject
        {
            get { return _isNewStyleProject; }
            set { _isNewStyleProject = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether it should be updated
        /// </summary>
        /// <value>
        ///   <c>true</c> if [update]; otherwise, <c>false</c>.
        /// </value>
        public bool Update
        {
            get
            {
                return m_Update;
            }

            set
            {
                m_Update = value;

                PropertyDidChange("Update");
                
            }
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name
        {
            get
            {
                return _name;
            }

            set
            {
                _name = value;

                PropertyDidChange("Name");
            }
        }

        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>
        /// The path.
        /// </value>
        public String Path
        {
            get
            {
                return _path;
            }

            set
            {
                _path = value;

                PropertyDidChange("Path");
            }
        }

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        /// <value>
        /// The version.
        /// </value>
        public Version AssemblyVersion
        {
            get
            {
                return _assemblyVersion;
            }

            set
            {
                _assemblyVersion = value;

                PropertyDidChange(nameof(AssemblyVersion));
            }
        }

        public Version PackageVersion
        {
            get
            {
                return _packageVersion;
            }

            set
            {
                _packageVersion = value;

                PropertyDidChange(nameof(PackageVersion));
            }
        }

        public Version Version
        {
            get
            {
                return _version;
            }

            set
            {
                _version = value;

                PropertyDidChange(nameof(Version));
            }
        }

        public Version VersionPrefix
        {
            get
            {
                return _versionPrefix;
            }

            set
            {
                _versionPrefix = value;

                PropertyDidChange(nameof(VersionPrefix));
            }
        }

        public Version MauiDisplayVersion
        {
            get
            {
                return _mauiDisplayVersion;
            }

            set
            {
                _mauiDisplayVersion = value;

                PropertyDidChange(nameof(MauiDisplayVersion));
            }
        }

        public Version MauiAppVersion
        {
            get
            {
                return _mauiAppVersion;
            }

            set
            {
                _mauiAppVersion = value;

                PropertyDidChange(nameof(MauiAppVersion));
            }
        }

        public string VersionSuffix
        {
            get
            {
                return _versionSuffix;
            }
            set
            {
                _versionSuffix = value;

                PropertyDidChange(nameof(VersionSuffix));
            }
        }

        public string InformationalVersion
        {
            get
            {
                return _informationalVersion;
            }
            set
            {
                _informationalVersion = value;

                PropertyDidChange(nameof(InformationalVersion));
            }
        }

        /// <summary>
        /// Gets or sets the file version.
        /// </summary>
        /// <value>
        /// The file version.
        /// </value>
        public Version FileVersion
        {
            get
            {
                if (_fileVersion == null) 
                    _assemblyVersion = new Version("1, 0, 0, 0");

                return _fileVersion;
            }

            set
            {
                _fileVersion = value;

                PropertyDidChange(nameof(FileVersion));
            }
        }

        public string FileVersionValue
        {
            get
            {

                var strVersion = "1.0";

                if (FileVersion.Major >= 0)
                {
                    strVersion = $"{FileVersion.Major}";
                }

                if (FileVersion.Minor >= 0)
                {
                    strVersion = $"{strVersion}.{FileVersion.Minor}";
                }

                if (FileVersion.Build >= 0)
                {
                    strVersion = $"{strVersion}.{FileVersion.Build}";
                }

                if (FileVersion.Revision >= 0)
                {
                    strVersion = $"{strVersion}.{FileVersion.Revision}";
                }

                return strVersion;

            }

        }

        /// <summary>
        /// Gets or sets the real project.
        /// </summary>
        /// <value>
        /// The real project.
        /// </value>
        public Project RealProject { get; set; }

        /// <summary>
        /// Gets or sets the project item.
        /// </summary>
        /// <value>
        /// The project item.
        /// </value>
        public ProjectItem ProjectItem { get; set; }

        /// <summary>
        /// Is iOS or Mac
        /// </summary>
        public bool IsCocoa { get; set; }

        /// <summary>
        /// Is android
        /// </summary>
        public bool IsAndroid { get; set; }

		/// <summary>
		/// Is UWP
		/// </summary>
		/// <value>
		///   <c>true</c> if this instance is uwp; otherwise, <c>false</c>.
		/// </value>
		public bool IsUWP { get; set; }

        public ProjectItem SecondaryProjectItem { get; set; }

       
        public string ProjectType
        {
            get { return _projectType; }
            set { _projectType = value; }
        }

        #endregion

        public ProjectVersion()
        {
            ProjectType = "Unknown";
        }
    }

}
