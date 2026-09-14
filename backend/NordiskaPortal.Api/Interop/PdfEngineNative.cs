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
            string jsonFilePath,
            PdfReportType reportType,
            string outDir,
            string? pfxPath,
            string? password,
            IntPtr progressCb,
            IntPtr progressCtx
        );
    }
}
