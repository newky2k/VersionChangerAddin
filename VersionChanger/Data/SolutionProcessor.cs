using DSoft.VersionChanger.Extensions;
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
using System.Xml;

namespace DSoft.VersionChanger.Data
{
    /// <summary>
    /// Processes the solution
    /// </summary>
    public class SolutionProcessor : IDisposable
    {
        #region Fields
        private List<FailedProject> _failedProjects;
        private Solution _mainSolution;
        #endregion

        #region Properties

        public event EventHandler<int> OnLoadedProjects = delegate { };

        public event EventHandler<Tuple<int, string>> OnStartingProject = delegate { };

        public bool DetectedUnloadedProjects { get; set; }

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

        #endregion

        #region Constructors

        public SolutionProcessor(Solution MainSolution)
        {
            _mainSolution = MainSolution;
        }


        #endregion

        #region Public Methods


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

            OnLoadedProjects(this, projs.Count);

           

            try
            {
                var loopPosition = 0;

                foreach (Project proj in projs)
                {
                    loopPosition++;

                    OnStartingProject.Invoke(this, new Tuple<int, string>(loopPosition, proj.Name));

                    if (!string.IsNullOrEmpty(proj.FileName) 
                        && proj.ProjectItems != null)
                    {
                        
                        bool hasCocoa = false;
                        bool hasAndroid = false;
                        bool isSdk = false;
                        bool hasUwp = false;

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

                            var iOSTypes = new List<string> { "{FEACFBD2-3405-455C-9665-78FE426C6842}", "{EE2C853D-36AF-4FDB-B1AD-8E90477E2198}" };
                            var androidTypes = new List<string> { "{EFBA0AD7-5A72-4C68-AF49-83D382785DCF}", "{10368E6C-D01B-4462-8E8B-01FC667A7035}" };
                            var macTypes = new List<string> { "{A3F8F2AB-B479-4A4A-A458-A89E7DC349F1}", "{EE2C853D-36AF-4FDB-B1AD-8E90477E2198}" };
                            var uwpTypes = new List<string> { "{A5A43C5B-DE2A-4C0C-9213-0A381AF9435A}" };

                            if (iOSTypes.Contains(projectTypeGuids.First()) || macTypes.Contains(projectTypeGuids.First()))
                            {
                                hasCocoa = true;      
                            }
                            else if (androidTypes.Contains(projectTypeGuids.First()))
                            {
                                hasAndroid = true;
                            }
                            else if (uwpTypes.Contains(projectTypeGuids.First()))
                            {
                                hasUwp = true;
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

                            isSdk = true;
                        }

                        var newVersion = LoadVersionNumber(proj, projectItem, hasCocoa, hasAndroid, hasUwp, isSdk);

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

        /// <summary>
        /// Update an SDK style csproj
        /// </summary>
        /// <param name="realProject"></param>
        /// <param name="newVersion"></param>
        /// <param name="fileVersion"></param>
        /// <param name="versionSuffix"></param>
        public void UpdateSdkProject(Project realProject, AssemblyVersionOptions versionOptions, Version newVersion, Version fileVersion = null, string versionSuffix = null)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            foreach (Property aProp in realProject.Properties)
            {
                var lowerCase = aProp.Name.ToLower();

                if (lowerCase.Equals("assemblyversion") && versionOptions.UpdateAssemblyVersion == true)
                {
                    aProp.Value = versionOptions.GetVersionString(newVersion);
                }
                else if (lowerCase.Equals("versionprefix") && versionOptions.UpdateAssemblyVersionPrefix == true)
                {
                    aProp.Value = versionOptions.GetVersionString(newVersion);
                }
                else if (lowerCase.Equals("fileversion") && versionOptions.UpdateFileVersion == true)
                {
                    var fVersion = (fileVersion == null) ? newVersion : fileVersion;

                    aProp.Value = versionOptions.GetVersionString(fVersion);
                }
                else if (lowerCase.Equals("version") && versionOptions.UpdateVersion == true)
                {
                    var str = versionOptions.CalculateVersion(newVersion, versionSuffix);

                    aProp.Value = str;

                }
                else if (lowerCase.Equals("versionsuffix") && !string.IsNullOrWhiteSpace(versionSuffix))
                {
                    aProp.Value = versionSuffix;
                }
                else if (lowerCase.Equals("packageversion", StringComparison.OrdinalIgnoreCase) && versionOptions.UpdatePackageVersion == true)
                {
                    var str = versionOptions.CalculateVersion(newVersion, versionSuffix);

                    aProp.Value = str;

                }
                else if (lowerCase.Equals("assemblyinformationalversion") || aProp.Name.ToLower().Equals("informationalversion") && versionOptions.UpdateInformationalVersion == true)
                {
                    var str = versionOptions.CalculateVersion(newVersion, versionSuffix);

                    aProp.Value = str;

                }
            }

            realProject.Save();


            //Update some properties via the xml, such as InformationalVersion
            var txt = File.ReadAllLines(realProject.FileName);
            var searchableText = string.Join("", txt);

            var infoVersion = "InformationalVersion";
            var packVer = "PackageVersion";
            var mauiDisplayVersion = "ApplicationDisplayVersion";
            var mauiAppVersionStr = "ApplicationVersion";
            var verPrefix = "VersionPrefix";

            var outPutLines = new List<string>();

            //update informational version and package version
            if (searchableText.Contains(infoVersion) || searchableText.Contains(packVer) || searchableText.Contains(mauiDisplayVersion) || searchableText.Contains(mauiAppVersionStr) || searchableText.Contains(verPrefix))
            {
                foreach (var aLine in txt)
                {
                    if (aLine.Contains($"<{infoVersion}>") && versionOptions.UpdateInformationalVersion == true)
                    {
                        var newLine = aLine;

                        var pos = newLine.IndexOf($"<{infoVersion}>");
                        var closer = newLine.IndexOf($"</{infoVersion}>");

                        if (pos != -1 && closer != -1)
                        {
                            newLine = newLine.Substring(0, pos + (infoVersion.Length + 2));

                            newLine += versionOptions.CalculateVersion(newVersion, versionSuffix);

                            newLine += $"</{infoVersion}>";

                            outPutLines.Add(newLine);
                        }
                    }
                    else if (aLine.Contains($"<{packVer}>") && versionOptions.UpdatePackageVersion == true)
                    {
                        var newLine = aLine;

                        var pos = newLine.IndexOf($"<{packVer}>");
                        var closer = newLine.IndexOf($"</{packVer}>");

                        if (pos != -1 && closer != -1)
                        {
                            newLine = newLine.Substring(0, pos + (packVer.Length + 2));

                            newLine += versionOptions.CalculateVersion(newVersion, versionSuffix);

                            newLine += $"</{packVer}>";

                            outPutLines.Add(newLine);
                        }
                    }
                    else if (aLine.Contains($"<{mauiDisplayVersion}>") && versionOptions.UpdateAppDisplayVersion == true)
                    {
                        var newLine = aLine;

                        var pos = newLine.IndexOf($"<{mauiDisplayVersion}>");
                        var closer = newLine.IndexOf($"</{mauiDisplayVersion}>");

                        if (pos != -1 && closer != -1)
                        {
                            newLine = newLine.Substring(0, pos + (mauiDisplayVersion.Length + 2));

                            newLine += versionOptions.CalculateVersion(newVersion);

                            newLine += $"</{mauiDisplayVersion}>";

                            outPutLines.Add(newLine);
                        }
                    }
                    else if (aLine.Contains($"<{mauiAppVersionStr}>") && versionOptions.UpdateAppDisplayVersion == true)
                    {
                        var newLine = aLine;

                        var pos = newLine.IndexOf($"<{mauiAppVersionStr}>");
                        var closer = newLine.IndexOf($"</{mauiAppVersionStr}>");

                        if (pos != -1 && closer != -1)
                        {
                            newLine = newLine.Substring(0, pos + (mauiAppVersionStr.Length + 2));

                            newLine += newVersion.Major.ToString();

                            newLine += $"</{mauiAppVersionStr}>";

                            outPutLines.Add(newLine);
                        }
                    }
                    else if (aLine.Contains($"<{verPrefix}>") && versionOptions.UpdateAssemblyVersionPrefix == true)
                    {
                        var newLine = aLine;

                        var pos = newLine.IndexOf($"<{verPrefix}>");
                        var closer = newLine.IndexOf($"</{verPrefix}>");

                        if (pos != -1 && closer != -1)
                        {
                            newLine = newLine.Substring(0, pos + (verPrefix.Length + 2));

                            newLine += versionOptions.CalculateVersion(newVersion);

                            newLine += $"</{verPrefix}>";

                            outPutLines.Add(newLine);
                        }
                    }
                    else
                    {
                        outPutLines.Add(aLine);
                    }
                }  
            }

            if (outPutLines.Count > 0) //only write if changes occured
                File.WriteAllLines(realProject.FileName, outPutLines);
        }

        /// <summary>
        /// Update an old framework style csproj
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="newAssemblyVersion">The new assembly version.</param>
        /// <param name="newFileVersion">The new file version.</param>
        public void UpdateFrameworkProject(ProjectItem item, AssemblyVersionOptions versionOptions, Version newAssemblyVersion, Version newFileVersion = null, string versionSuffix = null)
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
            string assemblyText = "assembly:";

            //if the file version is null, as seperate version have not been set
            if (newFileVersion == null)
            {
                newFileVersion = newAssemblyVersion;
            }

            var endLine = endPpint.Line;

            var lastLine = false;

            while (true)
            {
                var aLine = objEditPt.GetText(objEditPt.LineLength);

                //Debug.WriteLine($"Line: {objEditPt.Line} - {aLine}");

                if (!aLine.StartsWith("//")
                        && !aLine.StartsWith("'"))
                {

                    if (aLine.ToLower().Contains(assemblyText))
                    {
                        if (aLine.Contains(searchText) && versionOptions.UpdateAssemblyVersion == true)
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

                            var newVersionValue = versionOptions.CalculateVersion(newAssemblyVersion, includeZeroRevision: true);

                            var newLine = string.Format("{0}{1}{2}", firstBit, newVersionValue, end);

                            objEditPt.ReplaceText(objEditPt.LineLength, newLine, (int)vsEPReplaceTextOptions.vsEPReplaceTextKeepMarkers);

                        }

                        if (aLine.Contains(searchText2) && versionOptions.UpdateFileVersion == true)
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


                            var newFileVersionValue = versionOptions.CalculateVersion(newFileVersion, includeZeroRevision: true);

                            var newLine = string.Format("{0}{1}{2}", firstBit, newFileVersionValue.ToString(), end);

                           objEditPt.ReplaceText(objEditPt.LineLength, newLine, (int)vsEPReplaceTextOptions.vsEPReplaceTextKeepMarkers);


                        }

                        if (aLine.Contains(searchText3) && versionOptions.UpdateInformationalVersion == true)
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

                            var newFileVersionValue = versionOptions.CalculateVersion(newFileVersion, versionSuffix, true);

                            var newLine = string.Format("{0}{1}{2}", firstBit, newFileVersionValue.ToString(), end);


                            objEditPt.ReplaceText(objEditPt.LineLength, newLine, (int)vsEPReplaceTextOptions.vsEPReplaceTextKeepMarkers);

                            //var aLine2 = objEditPt.GetText(objEditPt.LineLength);

                        }
                    }

                }

                //check to see if the last line has already been processed
                if (objEditPt.Line.Equals(endLine) && lastLine == true)
                    break;//break the loop

                objEditPt.LineDown();
                objEditPt.StartOfLine();

                //if the we're on the last line, allow one further loop to process the line
                if (objEditPt.Line.Equals(endLine))
                    lastLine = true;

            }

            item.Save();
            aDoc.Close();
            
            
        }

        public void Dispose()
        {
            _mainSolution = null;
        }

        #endregion

        #region Private Methods

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
                    //int count = proj.ProjectItems.Count;

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
                //int count = Proj.ProjectItems.Count;

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

        private string[] GetProjectTypeGuids(EnvDTE.Project proj)
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

        private object GetService(object serviceProvider, Type type)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            return GetService(serviceProvider, type.GUID);
        }

        private object GetService(object serviceProviderObject, System.Guid guid)
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

        private ProjectVersion LoadVersionNumber(EnvDTE.Project project, ProjectItem projectItem, bool hasCocoa, bool hasAndroid, bool hasUWP, bool isSdk)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            ProjectVersion result;


            result = (isSdk) ? ProcessNewStyleProject(project) : ProcessOldStyleProject(project, projectItem);

            if (result != null)
            {
                if (hasCocoa)
                {
                    result.IsCocoa = true;
                    result.ProjectType = "Xamarin";


                    var infoPlist = FindProjectItem(project.ProjectItems, "info.plist");
                    result.SecondaryProjectItem = infoPlist;

                    if (infoPlist != null && infoPlist.Document != null)
                        infoPlist.Document.Close();
                }
                else if (hasAndroid)
                {
                    result.IsAndroid = true;
                    result.ProjectType = "Xamarin";

                    var aManifest = FindProjectItem(project.ProjectItems, "androidmanifest.xml");
                    result.SecondaryProjectItem = aManifest;

                    if (aManifest != null && aManifest.Document != null)
                        aManifest.Document.Close();
                }
                else if (hasUWP)
                {
                    result.IsUWP = true;
                    result.ProjectType = "UWP";

                    var packageManifest = FindProjectItem(project.ProjectItems, "package.appxmanifest");
                    result.SecondaryProjectItem = packageManifest;

                    if (packageManifest != null && packageManifest.Document != null)
                        packageManifest.Document.Close();


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
            var assemblyVersion = string.Empty;
            var fileVersion = string.Empty;
            var packageVersion = string.Empty;
            var versionSuffix = string.Empty;
            var version = string.Empty;
            var versionPrefix = string.Empty;
            var informationVersion = string.Empty;
            var mauiAppDisplayVersion = string.Empty;
            var mauiAppVersion = string.Empty;

            ThreadHelper.ThrowIfNotOnUIThread();

            foreach (Property aProp in project.Properties)
            {

                if (aProp.Name.ToLower().Equals("assemblyversion"))
                {
                    assemblyVersion = aProp.Value as string;
                }
                else if (aProp.Name.ToLower().Equals("fileversion"))
                {
                    fileVersion = aProp.Value as string;
                }
                else if (aProp.Name.ToLower().Equals("version"))
                {
                    version = aProp.Value as string;
                }
                else if (aProp.Name.ToLower().Equals("versionprefix"))
                {
                    versionPrefix = aProp.Value as string;
                }
                else if (aProp.Name.ToLower().Equals("packageversion"))
                {
                    packageVersion = aProp.Value as string;
                    if (packageVersion.Contains("-") && string.IsNullOrEmpty(versionSuffix))
                    {
                        var start = packageVersion.IndexOf("-");
                        if (start < packageVersion.Length) start += 1;
                        versionSuffix = packageVersion.Substring(start);
                    }
                }
                else if (aProp.Name.ToLower().Equals("versionsuffix"))
                {
                    versionSuffix = aProp.Value as string;
                }
            }


            //Update some properties via the xml, such as InformationalVersion
            var txt = File.ReadAllLines(project.FileName);
            var searchableText = string.Join("", txt);

            var infoVersion = "InformationalVersion";
            var verPrefix = "VersionPrefix";
            var mauiDisplayVersion = "ApplicationDisplayVersion";
            var mauiAppVersionStr = "ApplicationVersion";

            //update informational version and package version
            if (searchableText.Contains(infoVersion) || searchableText.Contains(verPrefix) || searchableText.Contains(mauiDisplayVersion) || searchableText.Contains(mauiAppVersionStr))
            {
                foreach (var aLine in txt)
                {
                    if (aLine.Contains($"<{infoVersion}>"))
                    {
                        informationVersion = aLine.ValueForNode(infoVersion);
                    }
                    else if (aLine.Contains($"<{verPrefix}>"))
                    {
                        versionPrefix = aLine.ValueForNode(verPrefix);
                    }
                    else if (aLine.Contains($"<{mauiDisplayVersion}>"))
                    {
                        mauiAppDisplayVersion = aLine.ValueForNode(mauiDisplayVersion);
                    }
                    else if (aLine.Contains($"<{mauiAppVersionStr}>"))
                    {
                        mauiAppVersion = aLine.ValueForNode(mauiAppVersionStr);
                    }

                }
            }

            //is this using the new style csproj
            var newVersion = new ProjectVersion();
            newVersion.Name = project.Name;
            newVersion.Path = project.FileName;
            newVersion.RealProject = project;
            newVersion.IsNewStyleProject = true;
            newVersion.ProjectType = "SDK";
            newVersion.VersionSuffix = versionSuffix;
            newVersion.InformationalVersion = informationVersion;

            if (assemblyVersion != string.Empty)
            {               
                try
                {
                    newVersion.AssemblyVersion = new Version(assemblyVersion);
                }
                catch
                {
                   
                }

            }

            if (fileVersion != string.Empty)
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

            if (version != string.Empty)
            {
                try
                {
                    newVersion.Version = new Version(version);
                }
                catch
                {
                    
                }
            }

            if (packageVersion != string.Empty)
            {
                try
                {
                    newVersion.PackageVersion = new Version(packageVersion);
                }
                catch
                {

                }
            }

            if (versionPrefix != string.Empty)
            {
                try
                {
                    newVersion.VersionPrefix = new Version(versionPrefix);
                }
                catch
                {

                }
            }

            if (mauiAppDisplayVersion != string.Empty)
            {
                try
                {
                    newVersion.MauiDisplayVersion = new Version(mauiAppDisplayVersion);
                }
                catch
                {

                }
            }

            if (mauiAppVersion != string.Empty)
            {
                try
                {
                    newVersion.MauiAppVersion = new Version(mauiAppVersion);
                }
                catch
                {

                }
            }

            return newVersion;
        }

        #endregion

 
    }
}
