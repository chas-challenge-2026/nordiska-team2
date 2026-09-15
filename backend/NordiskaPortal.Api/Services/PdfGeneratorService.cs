//using System.Text.Json;
//using NordiskaPortal.Api.Interop;

//namespace NordiskaPortal.Api.Services
//{
//    public class PdfGeneratorService
//    {
//        private readonly IConfiguration _configuration;

//        public PdfGeneratorService(IConfiguration configuration)
//        {
//            _configuration = configuration;
//        }

//        public int GenerateTaxReports(object reportOrReports, string outDir, string? pfxPath, string? password)
//        {
//            string tempDir = _configuration["PdfTemp:Directory"] ?? "json-temp";
//            Directory.CreateDirectory(tempDir);
//            Directory.CreateDirectory(outDir);

//            string tempJsonPath = Path.Combine(tempDir, $"{Guid.NewGuid()}.json");

//            // The native engine always expects a root-level JSON ARRAY of reports.
//            var reportsArray = reportOrReports is System.Collections.IEnumerable and not string
//                ? reportOrReports
//                : new[] { reportOrReports };

//            File.WriteAllText(tempJsonPath, JsonSerializer.Serialize(reportsArray));

//            try
//            {
//                int result = PdfEngineNative.pdf_engine_generate_and_sign(
//                    tempJsonPath,
//                    PdfReportType.TaxReport,
//                    outDir,
//                    pfxPath,
//                    password,
//                    IntPtr.Zero,
//                    IntPtr.Zero
//                );

//                return result; 
//            }
//            finally
//            {
//                File.Delete(tempJsonPath);
//            }
//        }
//    }

//}

using System.Text.Json;
using NordiskaPortal.Api.Interop;

namespace NordiskaPortal.Api.Services
{
    public class PdfGeneratorService
    {
        private readonly IConfiguration _configuration;

        public PdfGeneratorService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Generates a single report's PDF and returns its bytes directly.
        /// Uses an isolated temp folder per call so concurrent generations
        /// never collide or pick up the wrong file.
        /// </summary>
        public byte[]? GenerateSingleReportPdf(object report, string? pfxPath, string? password)
        {
            string tempBaseDir = _configuration["PdfTemp:Directory"] ?? "json-temp";
            string callId = Guid.NewGuid().ToString("N");

            string tempJsonPath = Path.Combine(tempBaseDir, $"{callId}.json");
            string tempOutDir = Path.Combine(tempBaseDir, callId);

            Directory.CreateDirectory(tempBaseDir);
            Directory.CreateDirectory(tempOutDir);

            File.WriteAllText(tempJsonPath, JsonSerializer.Serialize(new[] { report }));

            try
            {
                int result = PdfEngineNative.pdf_engine_generate_and_sign(
                    tempJsonPath,
                    PdfReportType.TaxReport,
                    tempOutDir,
                    pfxPath,
                    password,
                    IntPtr.Zero,
                    IntPtr.Zero
                );

                if (result <= 0)
                    return null;

                var pdfFile = new DirectoryInfo(tempOutDir).GetFiles("*.pdf").FirstOrDefault();
                if (pdfFile == null)
                    return null;

                return File.ReadAllBytes(pdfFile.FullName);
            }
            finally
            {
                File.Delete(tempJsonPath);
                Directory.Delete(tempOutDir, recursive: true);
            }
        }
    }
}