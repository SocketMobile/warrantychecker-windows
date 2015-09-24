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
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using RestSharp;

namespace WarrantyChecker
{
    public partial class FrmWarrantyChecker : Form
    {
        private const string StrDeveloperId = "21EC2020-3AEA-1069-A2DD-08002B30309D";
        private const string StrApplicationId = "com.socketmobile.test";
        private const string RemoteBase = "https://api.socketmobile.com/";
        private const string DbEndpoint = "v1/scanners/{1}{0}";
        private const string SandboxEndpoint = "v1/sandbox/scanners/{1}{0}";
        private const string EndPoint = SandboxEndpoint; // set to SandboxEndpoint or DbEndpoint
        private string _baseUrl;
        private string _endpoint;

        public static Warranty LastWarranty;
        
        private static string GetOsVersion()
        {
            OperatingSystem osInfo = Environment.OSVersion;
            return (osInfo.Platform == PlatformID.Win32NT)
                ? osInfo.Version.Major + "." + osInfo.Version.Minor
                : "Unknown";
        }
        private void DoWarrantyUpdateThread()
        {
            _baseUrl = RemoteBase;
            _endpoint = _baseUrl + EndPoint;
            UpdateStatusText("Starting REST GET call (Check Warranty)....");
            if (DoCheckWarranty())
            {
                UpdateStatusText("Starting REST PUT call (Update Warranty)....!");
                DoPostWarrantyUpdate();
                UpdateStatusText("Update Warranty done!");
            }

        }
        private static string Credentials()
        {
            string auth = string.Format(
                            "{0}:{1}",
                            StrDeveloperId,
                            StrApplicationId
                            );

            string enc = Convert.ToBase64String(Encoding.ASCII.GetBytes(auth));
            return string.Format("{0} {1}", "Basic", enc);
        }

        private Boolean DoCheckWarranty()
        {
            var strBdAddress = _device.BdAddress;
            var strUrl = string.Format(_endpoint, "", strBdAddress);
            RestClient restClient = new RestClient(strUrl);

            RestRequest request = new RestRequest(Method.GET);

            request.AddHeader("Authorization", Credentials());
            request.AddParameter("FirmwareVersion", _device.Version);
            request.AddParameter("hostPlatform", "WinNT-" + Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE"));
            request.AddParameter("osVersion", GetOsVersion());
            request.AddParameter("ScanApiType", "ScanAPI SDK");
            request.AddParameter("ScanApiVersion", _strScanApiVersion);
            RestResponse<WarrantyRequest> response2 =
                (RestResponse<WarrantyRequest>)restClient.Execute<WarrantyRequest>(request);
            if (response2.StatusCode == System.Net.HttpStatusCode.OK && response2.Data != null && response2.Data.Warranty != null)
            {
                MessageBox.Show(
                    String.Format(
                        "Your Scanner is {0}eligible for an extra year of Warranty {2} currently your end date is {1:M/d/yyyy}",
                                        response2.Data.Warranty.ExtensionEligible ? "" : "NOT ",
                                        response2.Data.Warranty.EndDate,
                                        response2.Data.Warranty.Description));
                LastWarranty = response2.Data.Warranty;
                // Show the product info here.
                if (response2.Data.Product != null)
                {
                    ProductInfo product = response2.Data.Product;
                    MessageBox.Show("Your Scanner is as follows: " + product.SKUNumber + " " + product.ProductNumber + ", " + product.PartNumber + "-" + product.SerialNumber + ":" + product.Description + " UPC=" + product.UPC);
                }
                return true;
            }
            else // Must be error 4xx or 5xx (or 000)
            {
                MessageBox.Show(GetErrorMessage(response2.StatusCode, response2.StatusDescription ?? response2.ErrorMessage));
                return false;
            }

        }
        private void DoPostWarrantyUpdate()
        {

            var strBdAddress = _device.BdAddress;
            var strUrl = string.Format(_endpoint, "/registrations", strBdAddress);
            var restClient = new RestClient(strUrl);

            var request = new RestRequest(Method.POST);
            request.AddHeader("Authorization", Credentials());
            // End user information, normally would be filled in by EndUser from a form
            request.AddParameter("userName", "Alfred Socket");
            request.AddParameter("userEmail", "alfred@socketmobile.com");
            request.AddParameter("userCompany", "Socket Mobile, Inc.");
            request.AddParameter("userAddress", "39700 Eureka Drive");
            request.AddParameter("userCity", "Newark");
            request.AddParameter("userState", "California");
            request.AddParameter("userZipcode", "94560");
            request.AddParameter("userCountry", "US"); // Two letter ISO3166-1 country codes
            request.AddParameter("userIndustry", "Computer Technology");
            request.AddParameter("isPurchaser", false);
            request.AddParameter("whrPurchased", "Socket Store");
            request.AddParameter("useSoftscan", true);


            RestResponse<WarrantyRequest> response2 = (RestResponse<WarrantyRequest>)restClient.Execute<WarrantyRequest>(request);
            if (response2.StatusCode == System.Net.HttpStatusCode.OK && response2.Data != null && response2.Data.Warranty != null)
            {
                DateTime endDate = response2.Data.Warranty.EndDate;
                DateTime previousDate = LastWarranty != null ? LastWarranty.EndDate : endDate;
                MessageBox.Show(previousDate < endDate
                    ? String.Format("Congratulations, your warranty has been extended to {0:M/d/yyyy}", endDate)
                    : String.Format("Your scanner has been registered."));
            }
            else  // Must be error 4xx or 5xx
            {
                MessageBox.Show(GetErrorMessage(response2.StatusCode, response2.StatusDescription));
            }

        }
        private static string GetErrorMessage(System.Net.HttpStatusCode httpStatusCode, string statusDescription)
        {
            switch (httpStatusCode)
            {
                case (System.Net.HttpStatusCode.BadRequest):
                    return "The request is invalid, contact support.";
                case (System.Net.HttpStatusCode.Unauthorized):
                    return "Request is not authorized.";
                case (System.Net.HttpStatusCode.NotFound):
                    return "We are unable to register your scanner at this time. Please contact Socket Mobile support to register your scanner.";
                case (System.Net.HttpStatusCode.Conflict):
                    return
                        "This scanner has already been registered. Please contact Socket Mobile support if you'd like to update your registration details.";
                default:
                    return string.Format("Unknown error code: {0} - {1}", httpStatusCode, statusDescription);
            }
        }

        //{"is_registered":"true","warranty":{"Description":"1 Year Limited Warranty (includes 90 days buffer)","EndDate":"2015-11-27", "ExtensionEligible":false}}
        //  If it isEligible it has _not_ been CVI registered, but isEligible is false does _not_ mean that it _has_ been CVI registered, 
        //{"Message":"This scanner is already registered to a user"}
        //CVI - Status = 400 "{"Message":"The request is invalid.","ModelState":{"RegistrationData.UserCountry":["The field UserCountry must be a string or array type with a maximum length of '2'."]}}"
        //      Status = 401
        //DVI - Status = 404 "{"Message":"Oops! We are unable to register your scanner at this time. Please contact Socket Mobile support to register your scanner."}
        //CVI - Status = 409 "{"Message":"This scanner has already been registered. Please contact Socket Mobile support if you'd like to update your registration details."}"
        public class WarrantyRequest
        {
            public string Message { get; set; } // only sent for errors
            public ModelState ModelState { get; set; }
            public Warranty Warranty { get; set; }
            public ProductInfo Product { get; set; }
        }
        public class Warranty
        {
            public string Description { get; set; }
            public DateTime EndDate { get; set; }
            public bool ExtensionEligible { get; set; } // eligible if not already extended, not a refurb or repair, or on SocketCare
        }
        public class ProductInfo
        {
            public string SerialNumber { get; set; }
            public string MacAddress { get; set; }
            public string PartNumber { get; set; }
            public string ProductNumber { get; set; }
            public string SKUNumber { get; set; }
            public string Description { get; set; }
            public string UPC { get; set; }
        }
        public class ModelState
        {
            public List<string> RegistrationData_UserCountry { get; set; }
            public List<string> RegistrationData_UserEmail { get; set; }
            public List<string> RegistrationData_UserCity { get; set; }
        }

    }
}