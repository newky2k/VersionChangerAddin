using EnvDTE;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private String m_Name;
        private String m_Path;
        private Version m_AssemblyVersion;
        private Version mFileVersion;
        private string _version;
        private string _versionSuffix;
        private bool isNewStyleProject;
		#endregion

		#region Properties


		public bool IsNewStyleProject
        {
            get { return isNewStyleProject; }
            set { isNewStyleProject = value; }
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
                return m_Name;
            }

            set
            {
                m_Name = value;

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
                return m_Path;
            }

            set
            {
                m_Path = value;

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
                if (m_AssemblyVersion == null) m_AssemblyVersion = new Version("0, 0, 0, 0");

                return m_AssemblyVersion;
            }

            set
            {
                m_AssemblyVersion = value;

                PropertyDidChange(nameof(AssemblyVersion));
            }
        }

        public string AssemblyVersionValue
        {
            get
            {
                return (AssemblyVersion.Revision <= 0) ? $"{AssemblyVersion.Major}.{AssemblyVersion.Minor}.{AssemblyVersion.Build}" : AssemblyVersion.ToString();
            }

        }

        public string PackageVersion
        {
            get
            {
                return _version;
            }
            set
            {
                _version = value;

                PropertyDidChange(nameof(PackageVersion));
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
                if (mFileVersion == null) 
                    m_AssemblyVersion = new Version("0, 0, 0, 0");

                return mFileVersion;
            }

            set
            {
                mFileVersion = value;

                PropertyDidChange(nameof(FileVersion));
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

        private string _ProjectType;

        public string ProjectType
        {
            get { return _ProjectType; }
            set { _ProjectType = value; }
        }

        #endregion

        public ProjectVersion()
        {
            ProjectType = "Unknown";
        }
    }

}
