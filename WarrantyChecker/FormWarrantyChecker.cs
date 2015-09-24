// Copyright 2015 Socket Mobile, Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
using System;
using System.Windows.Forms;
using System.Threading;
using ScanAPI;

namespace WarrantyChecker
{
    public partial class FrmWarrantyChecker : ScanApiHelper.ScanApiHelper.ScanApiHelperNotification
    {
        private const int ScanapiTimerPeriod = 100;		// milliseconds
        private readonly ScanApiHelper.ScanApiHelper _scanApiHelper;
        private String _strScanApiVersion = "Unknown";
        private const String StrOkBoxTitle = "Warranty Checker";
        ScanApiHelper.DeviceInfo _device;

        // for the status label
        public delegate void StandardTextOutputDelegate(string strStatus);

        public FrmWarrantyChecker()
        {
            InitializeComponent();
            lblStatus.Text = @"Initializing...";
            _scanApiHelper = new ScanApiHelper.ScanApiHelper();
            _scanApiHelper.SetNotification(this);
            Load += WarrantyChecker_Load;
        }
        private void WarrantyChecker_Load(object sender, EventArgs e)
        {
            // Start ScanAPI Helper
            _scanApiHelper.Open();
            timerScanners.Interval = ScanapiTimerPeriod;
            timerScanners.Start();
        }

        // if ScanAPI is fully initialized then we can
        // receive ScanObject from ScanAPI.
        private void timerScanners_Tick_1(object sender, EventArgs e)
        {
            _scanApiHelper.DoScanAPIReceive();
        }

        // OnDeviceArrival will pass this as the callback for the PostGetBtAddress call
        public void OnGetBdAddress(long result, ISktScanObject scanObj)
        {
            if (SktScanErrors.SKTSUCCESS(result))
            {
                byte[] bd = scanObj.Property.Array.Value;
                // Format the BD address the way we need it for the URLs
                _device.BdAddress = string.Format("{0:X2}{1:X2}{2:X2}{3:X2}{4:X2}{5:X2}", bd[0], bd[1], bd[2], bd[3], bd[4], bd[5]);
                UpdateStatusText("New Scanner: " + _device.Name + " - " + _device.BdAddress);
                _scanApiHelper.PostGetFirmware(_device, OnGetFirmwareVersion);
            }
            else
            {
                UpdateStatusText(String.Format("Unable to get Bluetooth address - error = %d"));
                MessageBox.Show(@"We are sorry, but your scanner is not supported for the Warranty Extension program");
            }
        }

        public void OnGetFirmwareVersion(long result, ISktScanObject scanObj)
        {
            if (SktScanErrors.SKTSUCCESS(result))
            {
                _device.Version =
                     Convert.ToString(scanObj.Property.Version.dwMajor, 16) + "." +
                     Convert.ToString(scanObj.Property.Version.dwMiddle, 16) + "." +
                     Convert.ToString(scanObj.Property.Version.dwMinor, 16) + " " +
                     scanObj.Property.Version.dwBuild + " " +
                     Convert.ToString(scanObj.Property.Version.wYear, 16) + "/" +
                     Convert.ToString(scanObj.Property.Version.wMonth, 16) + "/" +
                     Convert.ToString(scanObj.Property.Version.wDay, 16) + " " +
                     Convert.ToString(scanObj.Property.Version.wHour, 16) + ":" +
                     Convert.ToString(scanObj.Property.Version.wMinute, 16);
            }
            // done with ScanAPI, so go ahead and close it now...
            _scanApiHelper.Close();
            // Even if we couldn't get the firmware, we can go ahead and try...
            // start a thread to begin the dci/cvi process...
            var thread = new Thread(DoWarrantyUpdateThread) {Name = "RestThread"};
            thread.Start();
        }

        // ScanAPI Helper provides a series of Callbacks
        // indicating some asynchronous events has occured
        #region ScanApiHelperNotification Members

        // a scanner has connected to the host
        public void OnDeviceArrival(long result, ScanApiHelper.DeviceInfo newDevice)
        {
            if (SktScanErrors.SKTSUCCESS(result))
            {
                UpdateStatusText("New Scanner: " + newDevice.Name);
                _device = newDevice;
                _scanApiHelper.PostGetBtAddress(newDevice, OnGetBdAddress);
            }
            else
            {
                string strMsg = String.Format("Unable to open scanner, error = {0}.", result);
                MessageBox.Show(strMsg, StrOkBoxTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }


        }

        // a scanner has disconnected from the host
        public void OnDeviceRemoval(ScanApiHelper.DeviceInfo deviceRemoved)
        {
            UpdateStatusText("Scanner Removed: " + deviceRemoved.Name);
        }

        // a ScanAPI error occurs.
        public void OnError(long result, string errMsg)
        {
            string strErrorMsg = "ScanAPI Error: " + Convert.ToString(result) + " [" + (errMsg ?? "") + "]";
            MessageBox.Show(strErrorMsg, StrOkBoxTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            UpdateStatusText("Restart - " + strErrorMsg);
        }
        // some decoded data have been received
        public void OnDecodedData(ScanApiHelper.DeviceInfo device, ISktScanDecodedData decodedData)
        {
        }

        // ScanAPI is now initialized and fully functional
        // (ScanAPI has some internal testing that might take
        // few seconds to complete)
        public void OnScanApiInitializeComplete(long result)
        {
            UpdateStatusText(SktScanErrors.SKTSUCCESS(result)
                ? "SktScanAPI opened! Waiting for scanner..."
                : "SktScanOpen failed!");
            if (SktScanErrors.SKTSUCCESS(result))
                _scanApiHelper.PostGetScanAPIVersion(OnScanApiVersion);

        }

        public void UpdateStatusText(string strStatus)
        {
            if (InvokeRequired)
                Invoke(new StandardTextOutputDelegate(UpdateStatusText), new object[] { strStatus });
            else
                lblStatus.Text = strStatus;
        }
        // ScanAPI has now terminate, it is safe to
        // close the application now
        public void OnScanApiTerminated()
        {
            UpdateStatusText("ScanAPI Shutdown...");
        }

        // the ScanAPI Helper encounters an error during
        // the retrieval of a ScanObject
        public void OnErrorRetrievingScanObject(long result)
        {
            MessageBox.Show("Unable to retrieve a ScanAPI ScanObject: " + Convert.ToString(result),
                "Warranty Check", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        public void OnScanApiVersion(long result, ISktScanObject scanObj)
        {
            if (SktScanErrors.SKTSUCCESS(result))
            {
                _strScanApiVersion =
                     Convert.ToString(scanObj.Property.Version.dwMajor, 16) + "." +
                     Convert.ToString(scanObj.Property.Version.dwMiddle, 16) + "." +
                     Convert.ToString(scanObj.Property.Version.dwMinor, 16) + " " +
                     scanObj.Property.Version.dwBuild + " " +
                     Convert.ToString(scanObj.Property.Version.wYear, 16) + "/" +
                     Convert.ToString(scanObj.Property.Version.wMonth, 16) + "/" +
                     Convert.ToString(scanObj.Property.Version.wDay, 16) + " " +
                     Convert.ToString(scanObj.Property.Version.wHour, 16) + ":" +
                     Convert.ToString(scanObj.Property.Version.wMinute, 16);
            }
        }

        #endregion
    }
}
