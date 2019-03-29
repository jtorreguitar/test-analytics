using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using Google.Apis.AnalyticsReporting.v4;
using Google.Apis.AnalyticsReporting.v4.Data;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;

using test_analytics.Models;
using Microsoft.Extensions.Configuration;

namespace test_analytics.Controllers
{
    public class HomeController : Controller
    {
        private readonly string _viewId;

        public string ViewId => _viewId;

        public HomeController(IConfiguration configuration)
        {
            this._viewId = configuration["ViewId"];
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet]
        public IActionResult Test()
        {
            TestViewModel viewModel = new TestViewModel
            {
                StartDate = "2019-01-01",
                EndDate = "2019-03-29",
            };
            return View(viewModel);
        }

        [HttpPost]
        public IActionResult Test(TestViewModel viewModel)
        {
            var filepath = "analytics-creds.json";  // path to the json file for the Service account
            GoogleCredential credentials;
            using (var stream = new FileStream(filepath, FileMode.Open, FileAccess.Read))
            {
                string[] scopes = { AnalyticsReportingService.Scope.AnalyticsReadonly };
                var googleCredential = GoogleCredential.FromStream(stream);
                credentials = googleCredential.CreateScoped(scopes);
            }

            var reportingService = new AnalyticsReportingService(
            new BaseClientService.Initializer
            {
                HttpClientInitializer = credentials
            });

            var dateRange = new DateRange
            {
                StartDate = viewModel.StartDate,
                EndDate = viewModel.EndDate,
            };
            var sessions = new Metric
            {
                Expression = "ga:users",
                Alias = "Sessions",
            };
            var date = new Dimension { Name = "ga:date" };

            var reportRequest = new ReportRequest
            {
                DateRanges = new List<DateRange> { dateRange },
                Dimensions = new List<Dimension> { date },
                Metrics = new List<Metric> { sessions },
                ViewId = ViewId // your view id
            };

            var getReportsRequest = new GetReportsRequest
            {
                ReportRequests = new List<ReportRequest> { reportRequest }
            };
            var batchRequest = reportingService.Reports.BatchGet(getReportsRequest);
            var response = batchRequest.Execute();
            var responseText = new List<string>();
            foreach (var x in response.Reports.First().Data.Rows)
            {
                responseText.Add(string.Join(", ", x.Dimensions) + "   " + string.Join(", ", x.Metrics.First().Values));
            }

            return RedirectToAction("Results", new { results = responseText });
        }

        [HttpGet]
        public IActionResult Results(List<string> results)
        {
            return View(results);
        }
    }
}
