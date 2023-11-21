using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSoft.VersionChanger.Controls
{
    public static class SettingsControl
    {
        private static SettingsManager mSettingsManager;
        private static WritableSettingsStore mSettingsStore;
        private static bool mIsLoaded = false;

        public static bool IsLoaded
        {
            get
            {
                return mIsLoaded;
            }
        }
        public static SettingsManager SettingsManager
        {
            get
            {
                if (mSettingsManager == null)
                    throw new NullReferenceException("SettingsManager has not been set");

                return mSettingsManager;
            }
            set
            {
                mSettingsManager = value;

                Init();
            }
        }

        private static WritableSettingsStore SettingsStore
        {
           get
            {
                if (mSettingsStore == null)
                {
                    mSettingsStore = SettingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
                }

                return mSettingsStore;
            }
        }

        public static void Init()
        {
            if (!SettingsStore.CollectionExists("Version Options"))
            {
                SettingsStore.CreateCollection("Version Options");
            }

            mIsLoaded = true;
        }
        public static bool GetBooleanValue(string key, bool defaultValue = false)
        {
            var returnVal = defaultValue;

            try
            {
                returnVal = SettingsStore.GetBoolean("Version Options", key);
            }
            catch
            {
                SetBooleanValue(returnVal, key);

            }


            return returnVal;
        }

        public static MemoryStream GetStreamValue(string key)
        {
            MemoryStream returnVal = null;

            try
            {
                returnVal = SettingsStore.GetMemoryStream("Version Options", key);
            }
            catch
            {
                

            }


            return returnVal;
        }

        public static void SetBooleanValue(bool value, string key)
        {         
            SettingsStore.SetBoolean("Version Options", key, value);
        }

        public static void SetStreamValue(MemoryStream value, string key)
        {
            SettingsStore.SetMemoryStream("Version Options", key, value);
        }


    }
}
