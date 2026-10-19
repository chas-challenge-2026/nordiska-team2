using System.Runtime.InteropServices;

namespace NordiskaPortal.Api.Interop
{
    public enum PdfReportType
    {
        TaxReport = 0,
        BankStatement = 1
    }

    /// <summary>
    /// Result and error codes returned by the native pdf_engine C library.
    /// Values 0 and -1 through -9 match pdf_engine.h.
    /// </summary>
    public enum PdfEngineResult
    {
        Success = 0,
        ErrorInvalidArgs = -1,
        ErrorNoReportObject = -2,
        ErrorJsonReadFailed = -3,
        ErrorOutOfMemory = -4,
        ErrorUnknownReportType = -5,
        ErrorGenerationFailed = -6,
        ErrorSigningFailed = -7,
        ErrorMissingField = -8,
        ErrorMalformedReport = -9
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PdfEngineReportResult
    {
        public IntPtr ReportId;
        public int Status;
        public IntPtr Detail;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void PdfEngineProgressCb(
        ref PdfEngineReportResult result,
        IntPtr userCtx
    );

    public static class PdfEngineNative
    {
        [DllImport("pdf_engine", CallingConvention = CallingConvention.Cdecl)]
        public static extern int pdf_engine_generate_and_sign(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string jsonFilePath,
            PdfReportType reportType,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string outDir,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string? pfxPath,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string? password,
            PdfEngineProgressCb? progressCb,
            IntPtr progressCtx
        );

        [DllImport("pdf_engine", CallingConvention = CallingConvention.Cdecl, EntryPoint = "pdf_engine_generate_and_sign")]
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

