using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using NordiskaPortal.Api.DTOs;
using NordiskaPortal.Api.Interop;
using NordiskaPortal.Api.Services;
using Xunit;

namespace NordiskaPortal.Api.Tests
{
    public class PdfEngineNativeTests : IDisposable
    {
        private readonly string _tempDirectory;

        public PdfEngineNativeTests()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), "PdfEngineTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDirectory);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_tempDirectory))
                    Directory.Delete(_tempDirectory, recursive: true);
            }
            catch
            {
                // Ignore cleanup failures
            }
        }

        private static TaxReportDto CreateValidTaxReport()
        {
            return new TaxReportDto(
                Metadata: new TaxReportMetadataDto(
                    ReportId: "TAX-2024-TEST",
                    ReportType: "AnnualTaxReport",
                    Year: 2024,
                    PeriodStart: "2024-01-01",
                    PeriodEnd: "2024-12-31",
                    GenerationDate: "2025-01-15"
                ),
                Customer: new TaxReportCustomerDto(
                    CustomerId: "CUST-001",
                    PersonalId: "19850101-1234",
                    FullName: "Erik Svensson",
                    Address: "Storgatan 1, Stockholm"
                ),
                Account: new TaxReportAccountDto(
                    AccountNumber: "ACC-12345",
                    AccountType: "Savings",
                    InterestRatePct: 2.5m
                ),
                Summary: new TaxReportSummaryDto(
                    StartingBalanceSek: 10000m,
                    EndingBalanceSek: 15000m,
                    TotalDepositsSek: 5000m,
                    TotalWithdrawalsSek: 0m,
                    TotalInterestEarnedSek: 250m,
                    TotalTaxWithheldSek: 75m
                ),
                Transactions: new List<TaxReportTransactionDto>()
            );
        }

        [Fact]
        public void PdfEngineNative_ReturnsErrorInvalidArgs_WhenPathIsEmpty()
        {
            int result = PdfEngineNative.pdf_engine_generate_and_sign(
                "",
                PdfReportType.TaxReport,
                _tempDirectory,
                null,
                null,
                null,
                IntPtr.Zero
            );

            Assert.Equal((int)PdfEngineResult.ErrorInvalidArgs, result);
        }

        [Fact]
        public void PdfEngineNative_ReturnsErrorJsonReadFailed_WhenFileNotFound()
        {
            string nonExistentJson = Path.Combine(_tempDirectory, "non_existent.json");

            int result = PdfEngineNative.pdf_engine_generate_and_sign(
                nonExistentJson,
                PdfReportType.TaxReport,
                _tempDirectory,
                null,
                null,
                null,
                IntPtr.Zero
            );

            Assert.Equal((int)PdfEngineResult.ErrorJsonReadFailed, result);
        }

        [Fact]
        public void PdfEngineNative_ReturnsErrorNoReportObject_WhenJsonArrayIsEmpty()
        {
            string emptyArrayJson = Path.Combine(_tempDirectory, "empty.json");
            File.WriteAllText(emptyArrayJson, "[]");

            int result = PdfEngineNative.pdf_engine_generate_and_sign(
                emptyArrayJson,
                PdfReportType.TaxReport,
                _tempDirectory,
                null,
                null,
                null,
                IntPtr.Zero
            );

            Assert.Equal((int)PdfEngineResult.ErrorNoReportObject, result);
        }

        [Fact]
        public void PdfEngineNative_ReturnsErrorUnknownReportType_WhenReportTypeIsInvalid()
        {
            string jsonPath = Path.Combine(_tempDirectory, "report.json");
            File.WriteAllText(jsonPath, JsonSerializer.Serialize(new[] { CreateValidTaxReport() }));

            int result = PdfEngineNative.pdf_engine_generate_and_sign(
                jsonPath,
                (PdfReportType)999,
                _tempDirectory,
                null,
                null,
                null,
                IntPtr.Zero
            );

            Assert.Equal((int)PdfEngineResult.ErrorUnknownReportType, result);
        }

        [Fact]
        public void PdfEngineNative_ReturnsErrorSigningFailed_WhenPfxFileDoesNotExist()
        {
            string jsonPath = Path.Combine(_tempDirectory, "report_signing.json");
            File.WriteAllText(jsonPath, JsonSerializer.Serialize(new[] { CreateValidTaxReport() }));

            int result = PdfEngineNative.pdf_engine_generate_and_sign(
                jsonPath,
                PdfReportType.TaxReport,
                _tempDirectory,
                pfxPath: "non_existent_certificate.pfx",
                password: "pw",
                progressCb: null,
                progressCtx: IntPtr.Zero
            );

            Assert.Equal((int)PdfEngineResult.ErrorSigningFailed, result);
        }

        [Fact]
        public void PdfEngineNative_GeneratesSignedPdf_WhenValidPfxSupplied()
        {
            string jsonPath = Path.Combine(_tempDirectory, "report_signed.json");
            string outDir = Path.Combine(_tempDirectory, "signed_output");
            Directory.CreateDirectory(outDir);
            File.WriteAllText(jsonPath, JsonSerializer.Serialize(new[] { CreateValidTaxReport() }));

            string? pfxPath = new[]
            {
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../NordiskaPortal.Api/certificates/nordiska-signing.pfx")),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../NordiskaPortal.Api/certificates/nordiska-signing.pfx")),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "certificates/nordiska-signing.pfx")),
                Path.GetFullPath("certificates/nordiska-signing.pfx")
            }.FirstOrDefault(File.Exists);

            Assert.True(pfxPath != null && File.Exists(pfxPath), "Certificate not found.");

            int result = PdfEngineNative.pdf_engine_generate_and_sign(
                jsonPath,
                PdfReportType.TaxReport,
                outDir,
                pfxPath: pfxPath,
                password: "secret123",
                progressCb: null,
                progressCtx: IntPtr.Zero
            );

            Assert.Equal(1, result);
            var generatedPdfs = Directory.GetFiles(outDir, "*.pdf");
            Assert.Single(generatedPdfs);

            // Verify the generated PDF contains digital signature marker bytes
            byte[] pdfBytes = File.ReadAllBytes(generatedPdfs[0]);
            Assert.True(pdfBytes.Length > 0);
            string pdfContent = System.Text.Encoding.ASCII.GetString(pdfBytes);
            Assert.Contains("/ByteRange", pdfContent);
        }


        [Fact]
        public void PdfEngineNative_CapturesProgressCallback_WhenFieldsAreMissing()
        {
            string jsonPath = Path.Combine(_tempDirectory, "invalid_fields.json");
            File.WriteAllText(jsonPath, "[{ \"wrong_field\": 123 }]");

            PdfEngineResult callbackStatus = PdfEngineResult.Success;
            string? callbackDetail = null;

            PdfEngineProgressCb cb = (ref PdfEngineReportResult reportResult, IntPtr _) =>
            {
                if (reportResult.Status != 0)
                {
                    callbackStatus = (PdfEngineResult)reportResult.Status;
                    callbackDetail = Marshal.PtrToStringUTF8(reportResult.Detail);
                }
            };

            int count = PdfEngineNative.pdf_engine_generate_and_sign(
                jsonPath,
                PdfReportType.TaxReport,
                _tempDirectory,
                null,
                null,
                cb,
                IntPtr.Zero
            );

            Assert.Equal(0, count);
            Assert.Equal(PdfEngineResult.ErrorMissingField, callbackStatus);
            Assert.False(string.IsNullOrEmpty(callbackDetail));
        }

        [Fact]
        public void PdfEngineNative_GeneratesPdf_WhenReportDataIsValid()
        {
            string jsonPath = Path.Combine(_tempDirectory, "valid.json");
            string outDir = Path.Combine(_tempDirectory, "valid_out");
            Directory.CreateDirectory(outDir);

            File.WriteAllText(jsonPath, JsonSerializer.Serialize(new[] { CreateValidTaxReport() }));

            int count = PdfEngineNative.pdf_engine_generate_and_sign(
                jsonPath,
                PdfReportType.TaxReport,
                outDir,
                null,
                null,
                null,
                IntPtr.Zero
            );

            Assert.Equal(1, count);
            Assert.True(Directory.GetFiles(outDir, "*.pdf").Length > 0);
        }

        [Fact]
        public void PdfGeneratorService_GenerateSingleReport_CapturesMissingFieldError()
        {
            var inMemoryConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["PdfTemp:Directory"] = _tempDirectory
                })
                .Build();

            var service = new PdfGeneratorService(inMemoryConfig);

            var result = service.GenerateSingleReport(
                new { Invalid = "data" },
                PdfReportType.TaxReport,
                null,
                null
            );

            Assert.False(result.Success);
            Assert.Null(result.PdfBytes);
            Assert.Equal(PdfEngineResult.ErrorMissingField, result.ErrorCode);
            Assert.NotNull(result.ErrorMessage);
            Assert.Equal(PdfEngineResult.ErrorMissingField, service.LastErrorCode);
        }

        [Fact]
        public void PdfGeneratorService_GenerateSingleReport_CapturesSigningFailed()
        {
            var inMemoryConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["PdfTemp:Directory"] = _tempDirectory
                })
                .Build();

            var service = new PdfGeneratorService(inMemoryConfig);

            var result = service.GenerateSingleReport(
                CreateValidTaxReport(),
                PdfReportType.TaxReport,
                pfxPath: "does_not_exist.pfx",
                password: "pw"
            );

            Assert.False(result.Success);
            Assert.Null(result.PdfBytes);
            Assert.Equal(PdfEngineResult.ErrorSigningFailed, result.ErrorCode);
            Assert.Equal(PdfEngineResult.ErrorSigningFailed, service.LastErrorCode);
        }

        [Fact]
        public void PdfGeneratorService_GenerateSingleReport_Succeeds_ForValidReport()
        {
            var inMemoryConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["PdfTemp:Directory"] = _tempDirectory
                })
                .Build();

            var service = new PdfGeneratorService(inMemoryConfig);

            var result = service.GenerateSingleReport(
                CreateValidTaxReport(),
                PdfReportType.TaxReport,
                null,
                null
            );

            Assert.True(result.Success);
            Assert.NotNull(result.PdfBytes);
            Assert.True(result.PdfBytes!.Length > 0);
            Assert.Equal(PdfEngineResult.Success, result.ErrorCode);
            Assert.Equal(PdfEngineResult.Success, service.LastErrorCode);
        }

        [Fact]
        public void PdfGeneratorService_GenerateSingleReportPdf_ReturnsNull_AndSavesErrorCode_OnFailure()
        {
            var inMemoryConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["PdfTemp:Directory"] = _tempDirectory
                })
                .Build();

            var service = new PdfGeneratorService(inMemoryConfig);

            byte[]? pdfBytes = service.GenerateSingleReportPdf(
                new { Invalid = "data" },
                PdfReportType.TaxReport,
                null,
                null
            );

            Assert.Null(pdfBytes);
            Assert.Equal(PdfEngineResult.ErrorMissingField, service.LastErrorCode);
            Assert.NotNull(service.LastErrorMessage);
        }
    }
}
