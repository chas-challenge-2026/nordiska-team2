using System.Runtime.InteropServices;

namespace NordiskaPortal.Api.Interop
{
    public enum PdfReportType
    {
        TaxReport = 0
    }

    public static class PdfEngineNative
    {
        private const string LibraryName = "pdf_engine";

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int pdf_engine_generate_and_sign(
            string jsonData,
            PdfReportType reportType,
            string outPath,
            string? pfxPath,
            string? password
        );
    }
}
