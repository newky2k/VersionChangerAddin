using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DSoft.VersionChanger.Data
{
    public class ManualAssemblyResolver : IDisposable
    {
        #region Attributes

        /// <summary>
        /// list of the known assemblies by this resolver
        /// </summary>
        private readonly List<Assembly> _assemblies;

        #endregion

        #region Properties

        /// <summary>
        /// function to be called when an unknown assembly is requested that is not yet kown
        /// </summary>
        public Func<ResolveEventArgs, Assembly> OnUnknowAssemblyRequested { get; set; }

        #endregion

        #region Constructor

        public ManualAssemblyResolver(params Assembly[] assemblies)
        {
            _assemblies = new List<Assembly>();

            if (assemblies != null)
                _assemblies.AddRange(assemblies);

            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
        }

        #endregion

        #region Implement IDisposeable

        public void Dispose()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= OnAssemblyResolve;
        }

        #endregion

        #region Private

        /// <summary>
        /// will be called when an unknown assembly should be resolved
        /// </summary>
        /// <param name="sender">sender of the event</param>
        /// <param name="args">event that has been sent</param>
        /// <returns>the assembly that is needed or null</returns>
        private Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            foreach (Assembly assembly in _assemblies)
                if (args.Name == assembly.FullName)
                    return assembly;

            if (OnUnknowAssemblyRequested != null)
            {
                Assembly assembly = OnUnknowAssemblyRequested(args);

                if (assembly != null)
                    _assemblies.Add(assembly);

                return assembly;
            }

            return null;
        }

        #endregion
    }
}
