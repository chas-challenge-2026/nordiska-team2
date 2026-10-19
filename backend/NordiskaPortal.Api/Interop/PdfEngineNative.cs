using System.Runtime.InteropServices;

namespace NordiskaPortal.Api.Interop
{
    public enum PdfReportType
    {
        TaxReport = 0,
        BankStatement = 1
    }

    public static class PdfEngineNative
    {
        [DllImport("pdf_engine", CallingConvention = CallingConvention.Cdecl)]
        public static extern int pdf_engine_generate_and_sign(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string jsonFilePath,
            PdfReportType reportType,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string outDir,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string? pfxPath,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string? password,
            IntPtr progressCb,
            IntPtr progressCtx
        );
    }
}
