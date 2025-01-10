using ACDI.DataContext;
using CBS.Models;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using CBS.Models.Shared;
using CBS.Services.Remittance;
using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Dapper;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Data;
using Microsoft.Extensions.Configuration;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Spreadsheet;

namespace CBS.Controllers
{
    public class RemittanceController : Controller
    {
        private readonly ILogger<RemittanceController> _logger;
        private readonly AppDBContext _context;
        private readonly IRemittanceServices _remittanceServices;
        private readonly string _connectionString;
        private readonly IConfiguration _configuration;

        private readonly string _uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads");


        public RemittanceController(
            ILogger<RemittanceController> logger,
            AppDBContext context,
            IRemittanceServices remittanceServices,
            IConfiguration configuration)
        {
            _logger = logger;
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _remittanceServices = remittanceServices;
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DbConnection");

            if (!Directory.Exists(_uploadFolder))
            {
                Directory.CreateDirectory(_uploadFolder);
            }
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            const int PageSize = 10;

            // Await the asynchronous method to get all data
            var allData = await _remittanceServices.GetRemittanceUploadReportsAsync();

            // Skip and take for pagination
            var paginatedData = allData
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            // Create the view model for pagination
            var viewModel = new PaginatedList<RemittanceUploadReportsModel>(
                paginatedData,
                allData.Count(),
                page,
                PageSize
            );

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> PayArrearsTransaction()
        {
            var result = new Result
            {
                IsSuccessful = false,
                Message = "An error occurred while uploading the file."
            };
            _logger.LogInformation($"Start process:" + DateTime.Now);
            var jsonParyArrears = await GetRemittanceDataAsync();
            _logger.LogInformation($"Row Count:" + jsonParyArrears.Count());

            int batchSize = 10000;
            // Split into batches
            var list = jsonParyArrears.Select((item, index) => new { item, index })
                       .GroupBy(x => x.index / batchSize)
                       .Select(g => g.Select(x => x.item).ToList())
                       .ToList();

            foreach(var data in list)
            {
                _logger.LogInformation($"Start process arrears:" + DateTime.Now);
                var json = JsonConvert.SerializeObject(data);
                await PayArrearsTransaction(json);
                _logger.LogInformation($"End process arrears:" + DateTime.Now);
            }

            _logger.LogInformation($"End process:" + DateTime.Now);

            return Json(result);
        }

        [HttpPost]
        public async Task<List<GetComputedArrearsDataModel>> GetRemittanceDataAsync()
        {
            var parameters = new DynamicParameters();
            try
            {
                parameters.Add("@IsSuccessful", 1);
                parameters.Add("@Message", "Test");
                using var connection = new SqlConnection(_connectionString);
                var result = await connection.QueryAsync<GetComputedArrearsDataModel>(
                   "dbo.GetMonthlySavingInterests",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 0
                );
                return result.ToList();
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex.Message);
                return new List<GetComputedArrearsDataModel>();
            }
        }

        public async Task PayArrearsTransaction(string jsonData)
        {
            var parameters = new DynamicParameters();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                parameters.Add("@json", jsonData);
                parameters.Add("@IsSuccessful", 1);
                parameters.Add("@Message", "Test");
                await connection.ExecuteAsync(
                    "dbo.SavingInterestTransactions",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 0
                );
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex.Message);
            }

        }

        public async Task PayArrearsTransactionTest(string jsonData)
        {
            var parameters = new DynamicParameters();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                parameters.Add("@json", jsonData);
                parameters.Add("@IsSuccessful", 1);
                parameters.Add("@Message", "Test");
                await connection.ExecuteAsync(
                    "dbo.SavingInterestTransactions",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 0
                );
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex.Message);
            }

        }


        [HttpPost]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            var result = new Result
            {
                IsSuccessful = false,
                Message = "An error occurred while uploading the file."
            };

            if (file == null || file.Length == 0)
            {
                result.Message = "Please attach a file to upload.";
                return Json(result);
            }

            var filePath = Path.Combine(_uploadFolder, file.FileName);
            try
            {
                _logger.LogInformation($"Start process:" + DateTime.Now);

                if (System.IO.File.Exists(filePath))
                {
                    result.Message = "File already uploaded.";
                    return Json(result);
                }

                await using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                var list = MapExcelFile(filePath);
                var transaction =  await _remittanceServices.ExecuteStoredProcWithProcessedItemsAsync(list);

                _logger.LogInformation($"End processed:" + DateTime.Now);

                if (!transaction.IsSuccessful)
                    System.IO.File.Delete(filePath);

                result.IsSuccessful = transaction.IsSuccessful;
                result.Message = transaction.Message;
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                System.IO.File.Delete(filePath);
                result.Message = $"Error: {ex.Message}";
                return Json(result);
            }
        }

        public List<PensionerRemittanceUploadModel> MapExcelFile(string filePath)
        {
            var memPensionerSet = _context.CONTROLN_MEMBERSHIP_INFO
                .AsNoTracking()
                .Select(x => x.CONTROLN)
                .Distinct()
                .ToHashSet();

            var fileInfo = new FileInfo(filePath);
            using var package = new ExcelPackage(fileInfo);
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var worksheet = package.Workbook.Worksheets[0];
            var rowCount = worksheet.Dimension.Rows;
            var remittanceUploadModels = new List<PensionerRemittanceUploadModel>(rowCount - 1);

            Parallel.For(2, rowCount + 1, rowIndex =>
            {
                lock (remittanceUploadModels)
                {
                    var controlNr = worksheet.Cells[rowIndex, 1].Text;
                    var model = new PensionerRemittanceUploadModel
                    {
                        CONTROLN = controlNr,
                        DEDCODE = worksheet.Cells[rowIndex, 6].Text,
                        DEDAMOUNT = decimal.Parse(worksheet.Cells[rowIndex, 8].Text),
                        IsPosted = memPensionerSet.Contains(controlNr)
                    };
                    remittanceUploadModels.Add(model);
                }
            });
            _logger.LogInformation($"Log total List:" + remittanceUploadModels.Count());
            return remittanceUploadModels;
        }

        [HttpGet]
        public async Task<IActionResult> GetSavingsLedgerData(int page = 1,int pageSize = 10, string? Scno = "", string? Br = null)
        {
            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();

            parameters.Add("@Page", page);
            parameters.Add("@PageSize", pageSize);
            parameters.Add("@Scno", Scno);
            parameters.Add("@Br", Br);

            //if(filter?.Br != null)
            //    parameters.Add("@Br", filter?.Br);

            var result = await connection.QueryAsync<ReportSavingsLedgerDataModel>(
               "dbo.GetSDLedger",
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 0
            );

            return Ok(new
            {
                data = result,
                totalCount = result.FirstOrDefault()?.TotalRowCount ?? 0
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetMemberLoanLedgerData(int page = 1, int pageSize = 10, string? Scno = "", string? Br = null, string? Pn = null)
        {
            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();

            parameters.Add("@Page", page);
            parameters.Add("@PageSize", pageSize);
            parameters.Add("@Scno", Scno);
            parameters.Add("@Br", Br);
            parameters.Add("@Pn", Pn);

            //if (filter?.Br != null)
            //    parameters.Add("@Br", filter?.Br);

            var result = await connection.QueryAsync<ReportLoanLedgerDataModel>(
               "[dbo].[GetMemberLoanLedger]",
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 0
            );

            return Ok(new
            {
                data = result,
                totalCount = result.FirstOrDefault()?.TotalRowCount ?? 0
            });
        }
    }
}
