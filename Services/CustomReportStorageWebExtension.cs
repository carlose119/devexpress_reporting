using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DevExpress.XtraReports.Web.Extensions;
using DevExpress.XtraReports.UI;
using Microsoft.AspNetCore.Hosting;
using reporting.PredefinedReports;

namespace reporting.Services
{
    public class CustomReportStorageWebExtension : DevExpress.XtraReports.Web.Extensions.ReportStorageWebExtension
    {
        protected ReportDbContext DbContext { get; set; }
        readonly string ReportDirectory;
        const string FileExtension = ".repx";
        public CustomReportStorageWebExtension(IWebHostEnvironment env, ReportDbContext dbContext) {
            ReportDirectory = Path.Combine(env.ContentRootPath, "Reports");
            if (!Directory.Exists(ReportDirectory)) {
                Directory.CreateDirectory(ReportDirectory);
            }

            this.DbContext = dbContext;
        }

        private bool IsWithinReportsFolder(string url, string folder) {
            var rootDirectory = new DirectoryInfo(folder);
            var fileInfo = new FileInfo(Path.Combine(folder, url));
            return fileInfo.Directory.FullName.ToLower().StartsWith(rootDirectory.FullName.ToLower());
        }

        public override bool CanSetData(string url) {
            // Determines whether or not it is possible to store a report by a given URL. 
            // For instance, make the CanSetData method return false for reports that should be read-only in your storage. 
            // This method is called only for valid URLs (i.e., if the IsValidUrl method returned true) before the SetData method is called.

            return true;
        }

        public override bool IsValidUrl(string url) {
            // Determines whether or not the URL passed to the current Report Storage is valid. 
            // For instance, implement your own logic to prohibit URLs that contain white spaces or some other special characters. 
            // This method is called before the CanSetData and GetData methods.

            return Path.GetFileName(url) == url;
        }

        public override byte[] GetData(string url) {
            // Returns report layout data stored in a Report Storage using the specified URL. 
            // This method is called only for valid URLs after the IsValidUrl method is called.
            //try {
            //    if (Directory.EnumerateFiles(ReportDirectory).Select(Path.GetFileNameWithoutExtension).Contains(url))
            //    {
            //        return File.ReadAllBytes(Path.Combine(ReportDirectory, url + FileExtension));
            //    }
            //    if (ReportsFactory.Reports.ContainsKey(url))
            //    {
            //        using (MemoryStream ms = new MemoryStream()) {
            //            ReportsFactory.Reports[url]().SaveLayoutToXml(ms);
            //            return ms.ToArray();
            //        }
            //    }
            //} catch (Exception ex) {
            //    throw new DevExpress.XtraReports.Web.ClientControls.FaultException("Could not get report data.", ex);
            //}
            //throw new DevExpress.XtraReports.Web.ClientControls.FaultException(string.Format("Could not find report '{0}'.", url));

            // Uses a specified URL to return report layout data stored within a report storage medium.
            // This method is called if the **IsValidUrl** method returns **true**.
            // You can use the **GetData** method to process report parameters sent from the client
            // if the parameters are included in the report URL's query string.
            var reportData = DbContext.Reports.FirstOrDefault(x => x.Name == url);
            if (reportData != null)
                return reportData.LayoutData;

            if (ReportsFactory.Reports.ContainsKey(url))
            {
                using var ms = new MemoryStream();
                using XtraReport report = ReportsFactory.Reports[url]();
                report.SaveLayoutToXml(ms);
                return ms.ToArray();
            }
            throw new DevExpress.XtraReports.Web.ClientControls.FaultException(string.Format("Could not find report '{0}'.", url));
        }

        public override Dictionary<string, string> GetUrls() {
            // Returns a dictionary of the existing report URLs and display names. 
            // This method is called when running the Report Designer, 
            // before the Open Report and Save Report dialogs are shown and after a new report is saved to a storage.

            //return Directory.GetFiles(ReportDirectory, "*" + FileExtension)
            //                         .Select(Path.GetFileNameWithoutExtension)
            //                         .Union(ReportsFactory.Reports.Select(x => x.Key))
            //                         .ToDictionary<string, string>(x => x);

            // Returns a dictionary that contains the report names (URLs) and display names. 
            // The Report Designer uses this method to populate the Open Report and Save Report dialogs.
            return DbContext.Reports
                .ToList()
                .Select(x => x.Name)
                .Union(ReportsFactory.Reports.Select(x => x.Key))
                .ToDictionary<string, string>(x => x);
        }

        public override void SetData(XtraReport report, string url) {
            // Stores the specified report to a Report Storage using the specified URL. 
            // This method is called only after the IsValidUrl and CanSetData methods are called.
            //if(!IsWithinReportsFolder(url, ReportDirectory))
            //    throw new DevExpress.XtraReports.Web.ClientControls.FaultException("Invalid report name.");
            //report.SaveLayoutToXml(Path.Combine(ReportDirectory, url + FileExtension));

            // Saves the specified report to the report storage with the specified name
            // (saves existing reports only). 
            using var stream = new MemoryStream(); report.SaveLayoutToXml(stream);
            var reportData = DbContext.Reports.FirstOrDefault(x => x.Name == url);
            if (reportData == null)
            {
                DbContext.Reports.Add(new ReportItem { Name = url, DisplayName = url, LayoutData = stream.ToArray() });
            }
            else
            {
                reportData.LayoutData = stream.ToArray();
            }
            DbContext.SaveChanges();
        }

        public override string SetNewData(XtraReport report, string defaultUrl) {
            // Stores the specified report using a new URL. 
            // The IsValidUrl and CanSetData methods are never called before this method. 
            // You can validate and correct the specified URL directly in the SetNewData method implementation 
            // and return the resulting URL used to save a report in your storage.
            SetData(report, defaultUrl);
            return defaultUrl;
        }
    }
}
