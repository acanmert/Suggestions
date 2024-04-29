using Microsoft.AspNetCore.Mvc;
using Suggestions.Models;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
namespace Suggestions.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private static readonly HttpClient _client = new HttpClient();

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet]
        public IActionResult DataUpload()
        {

            return View(GetFile());
        }
        [HttpPost]
        public async Task<IActionResult> DataUpload(IFormFile formFile)
        {
            //...ekleme i?lemleri   
            if (formFile != null && formFile.FileName.EndsWith(".csv"))
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\dataset", formFile.FileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await formFile.CopyToAsync(stream);
                }
            }
            return View(GetFile());
        }
        public List<string> GetFile()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\dataset");
            var dataSet = Directory.GetFiles(filePath);

            List<string> dataFileNames = new List<string>();
            foreach (var data in dataSet)
            {
                dataFileNames.Add(Path.GetFileName(data));
            }
            return dataFileNames;
        }

        public static async Task<List<string>> get_recommendations(string fileName, List<string> benzerlikName, string productName, string getProductName, string p_type)
        {
            string benzerlik = string.Join(",", benzerlikName);
            string secim = fileName;
            string selectedFeatures = benzerlik;
            string title = getProductName;
            string pName = productName;
            string pType = p_type;

            //string secim = "book_data.csv";
            //string[] selectedFeatures = { "Name", "Genre" };
            //string title = "ciglik";
            //string pName = "Name";
            //string pType = "hayir"; string secim = "book_data.csv";


            // Flask API'sine GET iste?i g�nder
            string apiUrl = $"http://127.0.0.1:5000/recommendations?secim={secim}&selected_features={string.Join(",", selectedFeatures)}&title={title}&p_name={pName}&p_type={pType}";
            HttpResponseMessage response = await _client.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                // API'den d�nen JSON verisini oku
                string jsonResponse = await response.Content.ReadAsStringAsync();

                // JSON verisini diziye d�n�?t�r
                var recommendations = Newtonsoft.Json.JsonConvert.DeserializeObject<string[]>(jsonResponse).ToList();

                // �nerileri d�ng� i�inde de?il, d�ng� bitti?inde toplu olarak d�nd�r
                return recommendations;
            }
            else
            {
                List<string> error_List = new List<string>();

                error_List.Add("-1");

                return error_List;
            }

        }
        public string Python(string fileName, List<string> benzerlikName, string productName, string getProductName, string p_type)
        {
            string output;
            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = "python"; // Python yorumlay?c?s?n?n yolu.
            start.Arguments = "oneri.py"; // Python scriptinizin yolu.
            start.UseShellExecute = false;
            start.RedirectStandardInput = true;
            start.RedirectStandardOutput = true;
            start.RedirectStandardError = true;
            start.StandardOutputEncoding = Encoding.UTF8;
            start.StandardErrorEncoding = Encoding.UTF8;
            using (Process process = Process.Start(start))
            {
                using (StreamWriter writer = process.StandardInput)
                {
                    string benzerlik = string.Join(",", benzerlikName);
                    writer.WriteLine(fileName.ToString()); // �rne?in, veri seti se�imi i�in.
                    writer.WriteLine(benzerlik.ToString());
                    writer.WriteLine(getProductName.ToString());
                    writer.WriteLine(productName.ToString());
                    writer.WriteLine(p_type.ToString());
                }

                output = process.StandardOutput.ReadToEnd();


            }
            return output;
        }


        public IActionResult Suggestions(string fileName)
        {
            FileUploadViewModel file = new FileUploadViewModel();

            file.FieldList = Header(fileName);
            file.FileNames = GetFile();
            file.ThisFileName = fileName;
            return View(file);
        }
        public async Task<IActionResult> ProcessSuggestions(string fileName, List<string> benzerlikName, string productName, string getProductName, string p_type)
        {
            var recommendations = await get_recommendations(fileName, benzerlikName, productName, getProductName, p_type);
            // var recommendationList = recommendations.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();

            return View(recommendations);
        }

        public List<string> Header(string fileName)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\dataset", fileName);
            using (var reader = new StreamReader(path))
            {
                // ?lk sat?r? oku
                string ilkSatir = reader.ReadLine();
                List<string> liste = ilkSatir.Split(',').ToList();
                return liste;
            }
        }


    

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

    }
}
