# Warranty Extension API Sample

This C# program demonstrates the usage of both the ScanAPI and REST API's to get 
warranty status and request an extension for a connected CHS Scanner.

It is built with VS2010 (or higher) due to the usage of the NuGet RESTSharp 
package.  This package should be automatically downloaded from NuGet main 
repository at nuget.org, but if not, it can be found at: 
https://github.com/restsharp/RestSharp

Additionally, it utilizes the ScanApiHelper file from the ScanAPI SDK NuGet to 
faciltate interaction with ScanAPI.

## Prerequisite
The ScanAPI SDK NuGet is required for this sample. This is a private NuGet that
needs to be downloaded from Socket Mobile.

This is intended as a sample only, as all error checking and thread management 
is not included.

## Description
The registration of the scanner requires the scanner Bluetooth address. That's
how the scanner is identified in Socket warranty registration database.

ScanApiHelper is actually a data member of the WarrantyChecker Form object.
It gets allocated in the Form constructor function. This is also a good place to
set the notification reference to this Form which derives from 
ScanApiHelperNotification.

The ScanApiHelperNotification provides an interface of the possible 
notifications coming from ScanAPI.

ScanApiHelper is then opens in the Form Load handler. A timer is also set that 
is used to "consume" any asynchronous events comming from ScanAPI. The handler 
of this timer just called the function ScanApiHelper DoScanAPIReceive.

The rest of the code is driven by the notifications received from ScanAPI.

The initial state is to wait for a scanner to connect. Once a scanner connects,
this app receive the OnDeviceArrival notification. In this notification the 
request of reading the scanner Bluetooth address is made by calling the 
ScanApiHelper PostGetBtAddress. This function returns immediately, and the 
response will be received in the callback passed as argument.

Once the Get Bluetooth address has completed, the callback OnGetBdAddress is 
called. In this callback the request to get the firmware version of the scanner
is made.
The Bluetooth address of the scanner is saved in the data member of the Form.

The get scanner firmware version is retrieved in the OnGetFirmwareVersion 
callback, and again this version is saved in the Form data member.

Once this last callback is received, then the ScanApiHelper is close as it is no
longer needed.
A thread to check the warranty starts at this point.

The DoWarrantyUpdateThread thread is invoked and proceed by checking the 
Warranty of this specific scanner. If this check is true, then it proceed to the
Warranty extension request.

Both of these request are REST requests. The details can be found in the code
behind the Rest.cs Form.


 


 








