using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Flavor;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DSoft.VersionChanger.Data
{
    /// <summary>
    /// Processes the solution
    /// </summary>
    public class SolutionProcessor : IDisposable
    {
        public bool DetectedUnloadedProjects { get; set; }

        private List<FailedProject> _failedProjects;

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

        private Solution _mainSolution;

        public SolutionProcessor(Solution MainSolution)
        {
            _mainSolution = MainSolution;
        }

        /// <summary>
        /// Builds the versions from the projects
        /// </summary>
        /// <param name="MainSolution">The main solution.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception"></exception>
        public ProjectVersionCollection BuildVersions(Solution MainSolution)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var versions = new ProjectVersionCollection();

            var projs = FindProjects(MainSolution);

            try
            {
                foreach (Project proj in projs)
                {
                    if (!String.IsNullOrEmpty(proj.FileName) 
                        && proj.ProjectItems != null)
                    {
                        
                        bool hasCocoa = false;
                        bool hasAndroid = false;

                        var projectTypeGuids = GetProjectTypeGuids(proj);
                        ProjectItem projectItem = null;

                        if (projectTypeGuids != null)
                        {
                            var ignorableTypes = new List<string>()
                            {
                                "{54435603-DBB4-11D2-8724-00A0C9A8B90C}"
                                , "{930C7802-8A8C-48F9-8165-68863BCCD9DD}"
                                , "{7CF6DF6D-3B04-46F8-A40B-537D21BCA0B4}" // Sandcast Help File Builder project 
                            };

                            var firstTypeId = projectTypeGuids.First().ToUpper();

                            //if the type of the project is on the ignore list then skip
                            if (ignorableTypes.Contains(firstTypeId))
                            {
                                continue;
                            }

                            var iOSTypes = new List<String> { "{FEACFBD2-3405-455C-9665-78FE426C6842}", "{EE2C853D-36AF-4FDB-B1AD-8E90477E2198}" };
                            var androidTypes = new List<String> { "{EFBA0AD7-5A72-4C68-AF49-83D382785DCF}", "{10368E6C-D01B-4462-8E8B-01FC667A7035}" };
                            var macTypes = new List<String> { "{A3F8F2AB-B479-4A4A-A458-A89E7DC349F1}", "{EE2C853D-36AF-4FDB-B1AD-8E90477E2198}" };

                            if (iOSTypes.Contains(projectTypeGuids.First()) || macTypes.Contains(projectTypeGuids.First()))
                            {
                                hasCocoa = true;      
                            }
                            else if (androidTypes.Contains(projectTypeGuids.First()))
                            {
                                hasAndroid = true;
                            }

                            projectItem = FindAssemblyInfoProjectItem(proj.ProjectItems);

                            if (projectItem == null)
                            {
                                var newFailedProject = new FailedProject()
                                {
                                    Name = proj.Name,
                                    Reason = Enum.FailureReason.MissingAssemblyInfo,
                                };

                                FailedProjects.Add(newFailedProject);

                                continue;
                            }

                           
                        }
                        else
                        {
                            projectItem = FindAssemblyInfoProjectItem(proj.ProjectItems);
                        }

                        var newVersion = LoadVersionNumber(proj, projectItem, hasCocoa, hasAndroid);

                        if (newVersion != null)
                        {
                            versions.Add(newVersion);

                            if (hasCocoa == true)
                                versions.HasIosMac = true;

                            if (hasAndroid == true)
                                versions.HasAndroid = true;
                        }

                    }

                }
            }
            catch (Exception)
            {
                throw;
            }

            return versions;
        }

        internal void UpdateProject(Project realProject, Version newVersion, Version fileVersion = null, string versionSuffix = null)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            
            foreach (Property aProp in realProject.Properties)
            {
                if (aProp.Name.ToLower().Equals("assemblyversion"))
                {
                    aProp.Value = (newVersion.Revision == -1) ? newVersion.ToString(3) : newVersion.ToString();
                }
                else if (aProp.Name.ToLower().Equals("fileversion"))
                {
                    var fVersion = (fileVersion == null) ? newVersion : fileVersion;

                    aProp.Value = (fVersion.Revision == -1) ? fVersion.ToString(3) : fVersion.ToString();
                }
                else if (aProp.Name.ToLower().Equals("version"))
                {
                    var str = (newVersion.Revision == -1) ? newVersion.ToString(3) : newVersion.ToString();
                    if (string.IsNullOrEmpty(versionSuffix) == false) str += $"-{versionSuffix}";
                    aProp.Value = str;
                }
                else if (aProp.Name.ToLower().Equals("versionsuffix"))
                {
                    aProp.Value = versionSuffix;
                }
                else if (aProp.Name.Equals("packageversion", StringComparison.OrdinalIgnoreCase))
                {
                    aProp.Value = (newVersion.Revision == -1) ? newVersion.ToString(3) : newVersion.ToString();

                }
            }

            realProject.Save();
        }

        /// <summary>
        /// Updates the file.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="newAssemblyVersion">The new assembly version.</param>
        /// <param name="newFileVersion">The new file version.</param>
        public void UpdateFile(ProjectItem item, Version newAssemblyVersion, Version newFileVersion = null, string versionSuffix = null)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!item.IsOpen)
                item.Open();

            var aDoc = item.Document;

            TextDocument editDoc = (TextDocument)aDoc.Object("TextDocument");
            var objEditPt = editDoc.StartPoint.CreateEditPoint();
            objEditPt.StartOfDocument();

            var endPpint = editDoc.EndPoint.CreateEditPoint();
            endPpint.EndOfDocument();

            string searchText = "AssemblyVersion";
            string searchText2 = "AssemblyFileVersion";
            string searchText3 = "AssemblyInformationalVersion";
            string searchVstart = "(\"";

            //if the file version is null, as seperate version have not been set
            if (newFileVersion == null)
            {
                newFileVersion = newAssemblyVersion;
            }

            var updatedVersion = false;
            var updatedFileVersion = false;
            var updatedVersionSuffix = false;
            var endLine = endPpint.Line;


            while (objEditPt.Line <= endPpint.Line)
            {
                var aLine = objEditPt.GetText(objEditPt.LineLength);

                if (!aLine.StartsWith("//")
                        && !aLine.StartsWith("'"))
                {

                    if (aLine.Contains(searchText))
                    {
                        //now get the version number
                        int locationStart = aLine.IndexOf(searchText);
                        var searchLength = searchText.Length;

                        string initail = aLine.Substring((locationStart + searchText.Length));
                        var openerlocationStart = initail.IndexOf(searchVstart);

                        searchLength += (openerlocationStart + searchVstart.Length);
                        //locationStart += openerlocationStart;

                        string firstBit = aLine.Substring(0, (locationStart + searchLength));
                        string remaining = aLine.Substring((locationStart + searchLength));
                        int locationEnd = remaining.IndexOf("\"");
                        string end = remaining.Substring(locationEnd);

                        var newVersionValue = (newAssemblyVersion.Revision == -1) ? $"{newAssemblyVersion.Major}.{newAssemblyVersion.Minor}.{newAssemblyVersion.Build}.0" : newAssemblyVersion.ToString();


                        var newLine = String.Format("{0}{1}{2}", firstBit, newVersionValue, end);

                        objEditPt.ReplaceText(objEditPt.LineLength, newLine, (int)vsEPReplaceTextOptions.vsEPReplaceTextKeepMarkers);

                        var aLine2 = objEditPt.GetText(objEditPt.LineLength);

                        //Console.WriteLine(aLine2);
                        updatedVersion = true;
                    }


                    if (aLine.Contains(searchText2))
                    {
                        int locationStart = aLine.IndexOf(searchText2);
                        var searchLength = searchText2.Length;

                        string initail = aLine.Substring((locationStart + searchText2.Length));
                        var openerlocationStart = initail.IndexOf(searchVstart);

                        searchLength += (openerlocationStart + searchVstart.Length);

                        string firstBit = aLine.Substring(0, (locationStart + searchLength));
                        string remaining = aLine.Substring((locationStart + searchLength));
                        int locationEnd = remaining.IndexOf("\"");
                        string end = remaining.Substring(locationEnd);

                        var newFileVersionValue = (newFileVersion.Revision == -1) ? $"{newFileVersion.Major}.{newFileVersion.Minor}.{newFileVersion.Build}.0" : newFileVersion.ToString();

                        var newLine = String.Format("{0}{1}{2}", firstBit, newFileVersionValue.ToString(), end);


                        objEditPt.ReplaceText(objEditPt.LineLength, newLine, (int)vsEPReplaceTextOptions.vsEPReplaceTextKeepMarkers);

                        var aLine2 = objEditPt.GetText(objEditPt.LineLength);

                        //Console.WriteLine(aLine2);

                        updatedFileVersion = true;

                    }

                    if (aLine.Contains(searchText3) && string.IsNullOrEmpty(versionSuffix) == false)
                    {
                        int locationStart = aLine.IndexOf(searchText3);
                        var searchLength = searchText3.Length;

                        string initail = aLine.Substring((locationStart + searchText3.Length));
                        var openerlocationStart = initail.IndexOf(searchVstart);

                        searchLength += (openerlocationStart + searchVstart.Length);

                        string firstBit = aLine.Substring(0, (locationStart + searchLength));
                        string remaining = aLine.Substring((locationStart + searchLength));
                        int locationEnd = remaining.IndexOf("\"");
                        string end = remaining.Substring(locationEnd);

                        var newFileVersionValue = (newFileVersion.Revision == -1) ? $"{newFileVersion.Major}.{newFileVersion.Minor}.{newFileVersion.Build}.0" : newFileVersion.ToString();

                        if (string.IsNullOrEmpty(versionSuffix) == false)
                        {
                            // overriding for semver
                            newFileVersionValue += $"-{versionSuffix}";
                        }

                        var newLine = String.Format("{0}{1}{2}", firstBit, newFileVersionValue.ToString(), end);


                        objEditPt.ReplaceText(objEditPt.LineLength, newLine, (int)vsEPReplaceTextOptions.vsEPReplaceTextKeepMarkers);

                        var aLine2 = objEditPt.GetText(objEditPt.LineLength);

                        //Console.WriteLine(aLine2);

                        updatedVersionSuffix = true;

                    }
                }

                if (updatedVersion
                    && updatedFileVersion
                    && (objEditPt.AtEndOfDocument || string.IsNullOrEmpty(versionSuffix) || updatedVersionSuffix))
                    break;

                objEditPt.LineDown();
                objEditPt.StartOfLine();

            }

            if (updatedVersionSuffix == false && string.IsNullOrEmpty(versionSuffix) == false)
            {
                var newFileVersionValue = (newFileVersion.Revision == -1) ? $"{newFileVersion.Major}.{newFileVersion.Minor}.{newFileVersion.Build}.0" : newFileVersion.ToString();
                newFileVersionValue += $"-{versionSuffix}";
                var newLine = String.Format("[assembly: AssemblyInformationalVersion(\"{0}\")]\r\n", newFileVersionValue);
                objEditPt.Insert(newLine);
            }

            item.Save();
            aDoc.Close();
            
            
        }

        /// <summary>
        /// Finds the projects in the solution
        /// </summary>
        /// <param name="solution">The solution.</param>
        /// <returns></returns>
        private ArrayList FindProjects(Solution solution)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            ArrayList projectst = new ArrayList();

            foreach (Project proj in solution.Projects)
            {

                if (string.Compare(EnvDTE.Constants.vsProjectKindUnmodeled, proj.Kind, System.StringComparison.OrdinalIgnoreCase) == 0)
                {
                    DetectedUnloadedProjects = true;

                    continue;
                }
                    

                if (proj.FullName == "")
                {
                    //folder
                    int count = proj.ProjectItems.Count;

                    AddSubProjects(proj, projectst);
                }
                else
                {
                    projectst.Add(proj);
                }

            }


            return projectst;
        }

        /// <summary>
        /// Adds the sub projects.
        /// </summary>
        /// <param name="Proj">The proj.</param>
        /// <param name="Items">The items.</param>
        private void AddSubProjects(Project Proj, ArrayList Items)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            //MessageBox.Show(proj.FullName);
            Debug.WriteLine(Proj.FullName);

            if (Proj.FullName == "")
            {
                //folder
                int count = Proj.ProjectItems.Count;

                foreach (ProjectItem proj2 in Proj.ProjectItems)
                {
                    if (proj2.SubProject != null)
                    {
                        AddSubProjects(proj2.SubProject, Items);
                    }
                    //MessageBox.Show(proj2.SubProject.ToString());
                }
            }
            else
            {
                Items.Add(Proj);
            }
        }

        /// <summary>
        /// Finds the assembly information project item.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <returns></returns>
        private ProjectItem FindAssemblyInfoProjectItem(ProjectItems items)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            return FindProjectItem(items, "assemblyinfo");
        }

        private ProjectItem FindProjectItem(ProjectItems items, string fileName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            //if (items == null)
            //    return null;

            foreach (ProjectItem aItem in items)
            {
                

                if (aItem.ProjectItems?.Count > 0)
                {
                    var aResult = FindProjectItem(aItem.ProjectItems, fileName);

                    if (aResult != null)
                        return aResult;

                }
                else if (aItem.Name.ToLower().Contains(fileName))
                {
                    return aItem;
                }

            }


            return null;
        }

        public string[] GetProjectTypeGuids(EnvDTE.Project proj)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string projectTypeGuids = string.Empty;
            object service = null;
            Microsoft.VisualStudio.Shell.Interop.IVsSolution solution = null;
            Microsoft.VisualStudio.Shell.Interop.IVsHierarchy hierarchy = null;
            IVsAggregatableProjectCorrected aggregatableProject = null;
            int result = 0;

            service = GetService(proj.DTE, typeof(SVsSolution));
            solution = (Microsoft.VisualStudio.Shell.Interop.IVsSolution)service;

            result = solution.GetProjectOfUniqueName(proj.UniqueName, out hierarchy);

            if (result == 0)
            {
                aggregatableProject = hierarchy as IVsAggregatableProjectCorrected;

                if (aggregatableProject != null)
                {
                    result = aggregatableProject.GetAggregateProjectTypeGuids(out projectTypeGuids);
                }
                
            }

            if (String.IsNullOrWhiteSpace(projectTypeGuids))
                return null;

            return projectTypeGuids.Split(';');


        }

        public object GetService(object serviceProvider, System.Type type)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            return GetService(serviceProvider, type.GUID);
        }

        public object GetService(object serviceProviderObject, System.Guid guid)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            object service = null;
            Microsoft.VisualStudio.OLE.Interop.IServiceProvider serviceProvider = null;
            IntPtr serviceIntPtr;
            int hr = 0;
            Guid SIDGuid;
            Guid IIDGuid;

            SIDGuid = guid;
            IIDGuid = SIDGuid;
            serviceProvider = (Microsoft.VisualStudio.OLE.Interop.IServiceProvider)serviceProviderObject;
            hr = serviceProvider.QueryService(SIDGuid, IIDGuid, out serviceIntPtr);

            if (hr != 0)
            {
                System.Runtime.InteropServices.Marshal.ThrowExceptionForHR(hr);
            }
            else if (!serviceIntPtr.Equals(IntPtr.Zero))
            {
                service = System.Runtime.InteropServices.Marshal.GetObjectForIUnknown(serviceIntPtr);
                System.Runtime.InteropServices.Marshal.Release(serviceIntPtr);
            }

            return service;
        }

        private ProjectVersion LoadVersionNumber(EnvDTE.Project project, ProjectItem projectItem, bool hasCocoa, bool hasAndroid)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            ProjectVersion result = null;


            result = (projectItem == null) ? ProcessNewStyleProject(project) : ProcessOldStyleProject(project, projectItem);

            if (result != null)
            {
                if (hasCocoa)
                {
                    result.IsCocoa = true;
                    result.ProjectType = "Xamarin";


                    var infoPlist = FindProjectItem(project.ProjectItems, "info.plist");
                    result.SecondaryProjectItem = infoPlist;
                }
                else if (hasAndroid)
                {
                    result.IsAndroid = true;
                    result.ProjectType = "Xamarin";

                    var aManifest = FindProjectItem(project.ProjectItems, "androidmanifest.xml");
                    result.SecondaryProjectItem = aManifest;

                }

            }

            return result;

        }

        /// <summary>
        /// Process the old style csproj file
        /// </summary>
        /// <param name="project"></param>
        /// <param name="projectItem"></param>
        /// <returns></returns>
        private ProjectVersion ProcessOldStyleProject(Project project, ProjectItem projectItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string version = String.Empty;
            string fileVersion = String.Empty;
            string versionSuffix = String.Empty;

            if (!projectItem.IsOpen)
                projectItem.Open();

            var aDoc = projectItem.Document;

            TextDocument editDoc = (TextDocument)aDoc.Object("TextDocument");

            var objEditPt = editDoc.StartPoint.CreateEditPoint();
            objEditPt.StartOfDocument();

            var endPpint = editDoc.EndPoint.CreateEditPoint();
            endPpint.EndOfDocument();

            string searchText = "AssemblyVersion";
            string searchText2 = "AssemblyFileVersion";
            string searchText3 = "AssemblyInformationalVersion";
            string searchVstart = "(\"";
            

            var ednLine = endPpint.Line;

            while (objEditPt.Line <= ednLine)
            {
                
                var aLine = objEditPt.GetText(objEditPt.LineLength);

                if (!aLine.StartsWith("//")
                        && !aLine.StartsWith("'"))
                {

                    if (aLine.Contains(searchText))
                    {
                        //find the AssemblyVersion 
                        int locationStart = aLine.IndexOf(searchText);
                        string remaining = aLine.Substring((locationStart + searchText.Length));

                        //find the start of the version definition
                        locationStart = remaining.IndexOf(searchVstart);
                        remaining = remaining.Substring((locationStart + searchVstart.Length));

                        int locationEnd = remaining.IndexOf("\"");
                        version = remaining.Substring(0, locationEnd);

                    }


                    if (aLine.Contains(searchText2))
                    {
                        int locationStart = aLine.IndexOf(searchText2);
                        string remaining = aLine.Substring((locationStart + searchText2.Length));

                        //find the start of the version definition
                        locationStart = remaining.IndexOf(searchVstart);
                        remaining = remaining.Substring((locationStart + searchVstart.Length));

                        int locationEnd = remaining.IndexOf("\"");
                        fileVersion = remaining.Substring(0, locationEnd);

                    }

                    if (aLine.Contains(searchText3))
                    {
                        int locationStart = aLine.IndexOf(searchText3);
                        string remaining = aLine.Substring((locationStart + searchText3.Length));

                        locationStart = remaining.IndexOf(searchVstart);
                        remaining = remaining.Substring((locationStart + searchVstart.Length));

                        int locationEnd = remaining.IndexOf("\"");
                        versionSuffix = remaining.Substring(0, locationEnd);

                    }
                }

                if (!String.IsNullOrWhiteSpace(version)
                    && !String.IsNullOrWhiteSpace(fileVersion)
                     && !string.IsNullOrEmpty(versionSuffix))
                    break;

                if (objEditPt.Line == ednLine)
                {
                    break;
                }
                else
                {
                    objEditPt.LineDown();
                    objEditPt.StartOfLine();
                }


            }

            aDoc.Close();
            aDoc = null;

            if (version != String.Empty)
            {
                
                var newVersion = new ProjectVersion();
                newVersion.Name = project.Name;
                newVersion.Path = projectItem.FileNames[0];
                newVersion.RealProject = project;
                newVersion.ProjectItem = projectItem;
                newVersion.ProjectType = "Standard";

                try
                {
                    newVersion.AssemblyVersion = new Version(version);
                }
                catch
                {
                    newVersion.AssemblyVersion = new Version("1.0");
                }

                if (fileVersion != String.Empty)
                {
                    try
                    {
                        newVersion.FileVersion = new Version(fileVersion);
                    }
                    catch
                    {
                        newVersion.FileVersion = newVersion.AssemblyVersion;
                    }
                }

                if (string.IsNullOrEmpty(versionSuffix) == false)
                {
                    if (versionSuffix.Contains("-"))
                    {
                        var start = versionSuffix.IndexOf("-");
                        if (start < versionSuffix.Length)
                        {
                            versionSuffix = versionSuffix.Substring(start + 1);
                            if (Regex.IsMatch(versionSuffix, @"^[0-9a-zA-Z\-]+$"))
                            {
                                newVersion.VersionSuffix = versionSuffix;
                            }
                        }
                    }
                }

                return newVersion;
            }
            else
            {
                var newFailedProject = new FailedProject()
                {
                    Name = project.Name,
                    FailedAssemblyVersion = string.IsNullOrWhiteSpace(version),
                    FailedAssemblyFileVersion = string.IsNullOrWhiteSpace(fileVersion),
                };

                FailedProjects.Add(newFailedProject);
            }

            return null;
        }

        /// <summary>
        /// Process the new style CSProj file
        /// </summary>
        /// <param name="project"></param>
        /// <returns></returns>
        private ProjectVersion ProcessNewStyleProject(Project project)
        {
            var version = string.Empty;
            var fileVersion = string.Empty;
            var packageVersion = string.Empty;
            var versionSuffix = string.Empty;

            ThreadHelper.ThrowIfNotOnUIThread();

            foreach (Property aProp in project.Properties)
            {
                if (aProp.Name.ToLower().Equals("assemblyversion"))
                {
                    version = aProp.Value as String;
                }
                else if (aProp.Name.ToLower().Equals("fileversion"))
                {
                    fileVersion = aProp.Value as String;
                }
                else if (aProp.Name.ToLower().Equals("version"))
                {
                    packageVersion = aProp.Value as String;
                    if (packageVersion.Contains("-") && string.IsNullOrEmpty(versionSuffix))
                    {
                        var start = packageVersion.IndexOf("-");
                        if (start < packageVersion.Length) start += 1;
                        versionSuffix = packageVersion.Substring(start);
                    }
                }
                else if (aProp.Name.ToLower().Equals("versionsuffix"))
                {
                    versionSuffix = aProp.Value as String;
                }
            }

            //is this using the new style csproj

            if (version != String.Empty)
            {
                //MessageBox.Show(String.Format("{0} \n {1}", proj.Name, version));
                var newVersion = new ProjectVersion();
                newVersion.Name = project.Name;
                newVersion.Path = project.FileName;
                newVersion.RealProject = project;
                newVersion.IsNewStyleProject = true;
                newVersion.ProjectType = "SDK";
                newVersion.VersionSuffix = versionSuffix;
                try
                {
                    newVersion.AssemblyVersion = new Version(version);
                }
                catch
                {
                    newVersion.AssemblyVersion = new Version("1.0");
                }

                if (fileVersion != String.Empty)
                {
                    try
                    {
                        newVersion.FileVersion = new Version(fileVersion);
                    }
                    catch
                    {
                        newVersion.FileVersion = newVersion.AssemblyVersion;
                    }
                }

                return newVersion;

            }

            return null;
        }

        public void Dispose()
        {
            _mainSolution = null;
        }
    }
}
