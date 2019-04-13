using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSoft.VersionChanger.Data
{
    public class FailedProject
    {
        public string Name { get; set; }

        public bool FailedAssemblyVersion { get; set; }

        public bool FailedAssemblyFileVersion { get; set; }

        public string ErrorMessage
        {
            get
            {
                if (FailedAssemblyVersion == true && FailedAssemblyFileVersion == true)
                {
                    return "Unable to load the AssemblyVersion and AssemblyFileVersion from the AssemblyInfo.cs file";
                }
                else if (FailedAssemblyVersion == true && FailedAssemblyFileVersion == false)
                {
                    return "Unable to load the AssemblyVersion from the AssemblyInfo.cs file";
                }
                else
                {
                    return "Unable to load the AssemblyFileVersion from the AssemblyInfo.cs file";
                }
            }
        }
    }
}
