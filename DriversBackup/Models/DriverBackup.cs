using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using Microsoft.Win32;

namespace DriversBackup.Models
{
    [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
    public class DriverBackup
    {
        public string WindowsRoot;
        public string SystemRoot;

        /// <summary>
        /// Constructor
        /// </summary>
        public DriverBackup()
        {
            /*
             * URI to the System32 folder.
             */
            WindowsRoot = Environment.GetEnvironmentVariable("SystemRoot") + "\\";
            SystemRoot = WindowsRoot + "system32\\";
        }

        /// <summary>
        /// Returns a list of drivers registered on a system.
        /// </summary>
        /// <returns>ArrayList containing driver information</returns>
        public List<DriverInformation> ListDrivers(bool showMicrosoft)
        {
            List<DriverInformation> driverList = new List<DriverInformation>();

            /*
             * Open registry and get a list of device types.
             */
            var regDeviceGuiDs = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Class\\");
            var deviceGuiDs = regDeviceGuiDs?.GetSubKeyNames();

            /*
             * Iterate through devices.
             */
            foreach (var deviceGuid in deviceGuiDs)
            {
                /*
                 * Get drivers assigned to each device type.
                 */
                var regDevice = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Class\\" + deviceGuid);
                var regDeviceSubkeys = regDevice?.GetSubKeyNames();

                /*
                 * For each driver, get the information on it (provider, type etc).
                 */
                foreach (var regDriverNumber in regDeviceSubkeys)
                {
                    string tmpProvider = "", tmpDesc = "";
                    try
                    {
                        var regDriver = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Class\\" + deviceGuid + "\\" + regDriverNumber);

                        try
                        {
                            /*
                             * Add information to our ArrayList.
                             */
                            tmpDesc = regDriver.GetValue("DriverDesc").ToString();
                            tmpProvider = regDriver.GetValue("ProviderName").ToString();
                        }
                        catch
                        {
                            //TODO Handle exception
                        }

                        /*
                         * If any of the information checks out as rubbish, discard this driver.
                         * Hash, but fair.
                         */
                        if (tmpProvider.Length > 0 && tmpDesc.Length > 0)
                        {
                            if (tmpProvider != "Microsoft")
                            {
                                driverList.Add(
                                    new DriverInformation(tmpProvider, tmpDesc, deviceGuid, regDriverNumber)
                                );
                            }
                            else
                            {
                                if (showMicrosoft)
                                {
                                    driverList.Add(
                                        new DriverInformation(tmpProvider, tmpDesc, deviceGuid, regDriverNumber)
                                    );
                                }
                            }

                        }
                        regDriver.Close();
                    }
                    catch
                    {
                        // TODO Handle exception
                    }

                    regDevice.Close();
                }
            }

            regDeviceGuiDs.Close();

            return driverList;
        }

        /// <summary>
        /// Creates a thread of backupDriverExec. No more, no less.
        /// </summary>
        /// <param name="classGuid"></param>
        /// <param name="driverId"></param>
        /// <param name="backupLocation"></param>
        public void BackupDriver(string classGuid, string driverId, string backupLocation)
        {
            string[] driverInfo = { classGuid, driverId, backupLocation };
            var backupThread = new Thread(BackupDriverExec);

            backupThread.Start(driverInfo);

        }

        /// <summary>
        /// Backs up the given device driver.
        /// </summary>
        private void BackupDriverExec(object driverInfoObj)
        {
            /*
             * Driver Info.
             */
            var driverInfoArray = (string[])driverInfoObj;
            var classGuid = driverInfoArray[0];
            var driverId = driverInfoArray[1];
            var backupLocation = driverInfoArray[2];
            string infFile, infFilePath;
            string driverDesc;

            var regDriverType = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Class\\" + classGuid);
            var driverType = regDriverType?.GetValue("Class").ToString();

            var driverInfo = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Class\\" + classGuid + "\\" + driverId);
            driverDesc = driverInfo?.GetValue("DriverDesc").ToString();
            infFile = driverInfo?.GetValue("InfPath").ToString();
            infFilePath = WindowsRoot + "inf\\" + infFile;

            /*
             * Create backup directory.
             */
            if (driverType.Length > 0)
            {
                Directory.CreateDirectory(backupLocation + driverType);
            }
            Directory.CreateDirectory(backupLocation + driverType + "\\" + driverDesc);



            /*
             * Copy over inf file.
             */
            try
            {
                File.Copy(infFilePath, backupLocation + driverType + "\\" + driverDesc + "\\" + infFile);
            }
            catch (IOException)
            {
            }

            /*
             * Backup driver files.
             */
            var driverIniFile = new IniFiles();
            var driverFiles = driverIniFile.GetKeys(infFilePath, "SourceDisksFiles");

            foreach (var driverFile in driverFiles)
            {
                try
                {
                    /*
                     * El-Cheapo driver companies put weird things like %SYSFILE%
                     * in their inf files, no idea what this does. So ignore it.
                     */
                    if (driverFile.Split('.').Length > 1)
                    {
                        /*
                         * Copy driver files from the right place.
                         */
                        if (driverFile.Split('.')[1] == "hlp")
                        {
                            File.Copy(WindowsRoot + "Help\\" + driverFile, backupLocation + driverType + "\\" + driverDesc + "\\" + driverFile);
                        }
                        else if (driverFile.Split('.')[1] == "sys")
                        {
                            File.Copy(SystemRoot + "drivers\\" + driverFile, backupLocation + driverType + "\\" + driverDesc + "\\" + driverFile);
                        }
                        else
                        {
                            File.Copy(SystemRoot + driverFile, backupLocation + driverType + "\\" + driverDesc + "\\" + driverFile);
                        }
                    }
                }
                catch (IOException)
                {
                }
            }

            /*
             * Close registry.
             */
            regDriverType?.Close();
            driverInfo?.Close();
        }
    }
}
