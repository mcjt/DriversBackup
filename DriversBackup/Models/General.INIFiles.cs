using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace DriversBackup.Models
{
    public class IniFiles
    {
        [DllImport("KERNEL32.DLL",   EntryPoint = "GetPrivateProfileStringW",
            SetLastError=true,
            CharSet=CharSet.Unicode, ExactSpelling=true,
            CallingConvention=CallingConvention.StdCall)]

        private static extern int GetPrivateProfileString(
            string lpAppName,
            string lpKeyName,
            string lpDefault,
            string lpReturnString,
            int nSize,
            string lpFilename);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="iniFile"></param>
        /// <param name="category"></param>
        /// <returns></returns>
        public List<string> GetKeys(string iniFile, string category)
        {
            string returnString = new string(' ', 32768);
            GetPrivateProfileString(category, null, null, returnString, 32768, iniFile);

            var result = new List<string>(returnString.Split('\0'));
            result.RemoveRange(result.Count-2,2);
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="iniFile"></param>
        /// <param name="category"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public string GetIniFileString(string iniFile, string category, string key, string defaultValue)
        {
            string returnString = new string(' ', 1024);
            GetPrivateProfileString(category, key, defaultValue, returnString, 1024, iniFile);

            return returnString.Split('\0')[0];
        }
    }
}
