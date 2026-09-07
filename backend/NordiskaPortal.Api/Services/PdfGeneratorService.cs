using NordiskaPortal.Api.Interop;

namespace NordiskaPortal.Api.Services
{
    public class PdfGeneratorService
    {
        public bool GenerateTaxReport(string jsonData, string outPath, string? pfxPath, string? password)
        {
            int result = PdfEngineNative.pdf_engine_generate_and_sign(
                jsonData,
                PdfReportType.TaxReport,
                outPath,
                pfxPath,
                password
            );

            return result == 0;
        }
    }
}