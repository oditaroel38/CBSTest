using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using CBS.Models;
using CBS.Data.Entities;
using ACDI.DataContext;
using CBS.Common;
using Microsoft.EntityFrameworkCore.Internal;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Math;
using Newtonsoft.Json;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Drawing;
using CBS.Models.Shared;
using Microsoft.Extensions.FileSystemGlobbing;
using System.Dynamic;
using Dapper;
using System.Data;
using System.Diagnostics;
using DocumentFormat.OpenXml.Wordprocessing;

namespace CBS.Services.Remittance
{
    public interface IRemittanceServices
    {
        Task<List<RemittanceUploadReportsModel>> GetRemittanceUploadReportsAsync();
        Task<ITransactionResult> ExecuteStoredProcWithProcessedItemsAsync(List<PensionerRemittanceUploadModel> dataList);
        
    }

    public class RemittanceServices : IRemittanceServices
    {
        private readonly AppDBContext _context;
        private readonly ILogger<RemittanceServices> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(10); // Semaphore for async locking

        public RemittanceServices(
            AppDBContext context,
            ILogger<RemittanceServices> logger,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DbConnection");
        }

        public async Task<List<RemittanceUploadReportsModel>> GetRemittanceUploadReportsAsync()
        {
            try
            {
                return await _context.Set<RemittanceUploadReportsModel>()
                    .FromSqlInterpolated($"EXEC RemittanceUploadReport")
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex.Message);
                return new List<RemittanceUploadReportsModel>();
            }
        }

        public async Task<ITransactionResult> ExecuteStoredProcWithProcessedItemsAsync(List<PensionerRemittanceUploadModel> dataList)
        {
            try
            {
                int batchSize = 10000;
                // Split into batches
                var postedBatches = BatchData(dataList.Where(x => x.IsPosted).ToList(), batchSize);
                var unpostedBatches = BatchData(dataList.Where(x => !x.IsPosted).ToList(), batchSize);
                // Process posted and Unposted batches in parallel
                var postedTasks = postedBatches.Select(batch => Task.Run(() => ProcessBatchAsync(batch, true)));
                var unpostedTasks = unpostedBatches.Select(batch => Task.Run(() => ProcessBatchAsync(batch, false)));

                await Task.WhenAll(postedTasks.Concat(unpostedTasks));
                return new SuccessfulTransaction("Successfully uploaded and processed the file.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new FailedTransaction("Failed to upload and process the file.");
            }
        }

        private List<List<T>> BatchData<T>(List<T> data, int batchSize)
        {
            return data.Select((item, index) => new { item, index })
                       .GroupBy(x => x.index / batchSize)
                       .Select(g => g.Select(x => x.item).ToList())
                       .ToList();
        }

        //Pensioner Remittance
        private async Task ProcessBatchAsync(List<PensionerRemittanceUploadModel> batch, bool isPosted)
        {
            var jsonData = JsonConvert.SerializeObject(batch);
            try
            {
                await _semaphore.WaitAsync(); // Limit the degree of parallelism

                if (isPosted)
                {
                    var jsonData2 = await GetRemittanceDataAsync(jsonData);

                    if (jsonData2 == "[]")
                        throw new Exception("Failed to upload and process the file.");

                    var jsonData3 = await ProcessRemittanceJsonListAsync(jsonData2);
                    if (jsonData3 == "[]")
                        throw new Exception("Failed to upload and process the file.");

                    await ProcessRemittanceTransaction(jsonData3);
                    await HistoryRemittanceTransaction(jsonData3);
                }
                else
                {
                    await UnpostedProcessRemittanceTransaction(jsonData);
                }
            }
            finally
            {
                _semaphore.Release(); // Always release semaphore
            }
        }

        //ARMY Remittance
        public async Task<string> GetRemittanceDataAsync(string jsonData)
        {
            var parameters = new DynamicParameters();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                parameters.Add("@json", jsonData);
                var result = await connection.QueryAsync<RemittanceCheckingRawDataModel>(
                    "dbo.RemittanceCheckingRawData",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 0
                );
                return JsonConvert.SerializeObject(result);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex.Message);
                return JsonConvert.SerializeObject(new List<RemittanceCheckingRawDataModel>());
            }

        }
        public async Task<string> ProcessRemittanceJsonListAsync(string jsonData)
        {
            var parameters = new DynamicParameters();
            try
            {
                _logger.LogError($"Start dbo.RemittanceComputingRawData:");
                using var connection = new SqlConnection(_connectionString);
                parameters.Add("@json", jsonData);
                var result = await connection.QueryAsync<RemittanceComputingRawDataModel>(
                    "dbo.RemittanceComputingRawData",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 0
                );
                _logger.LogError($"End dbo.RemittanceComputingRawData:");
                return JsonConvert.SerializeObject(result);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex.Message);
                return JsonConvert.SerializeObject(new List<RemittanceComputingRawDataModel>());
            }
        }

        public async Task ProcessRemittanceTransaction(string jsonData)
        {
            var parameters = new DynamicParameters();
            try
            {
                _logger.LogError($"Start dbo.RemittanceTransaction:");
                using var connection = new SqlConnection(_connectionString);
                parameters.Add("@json", jsonData);
                await connection.ExecuteAsync(
                    "dbo.RemittanceTransaction",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 0
                );
                _logger.LogError($"End dbo.RemittanceTransaction:");
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex.Message);
            }

        }

        public async Task UnpostedProcessRemittanceTransaction(string jsonData)
        {
            var parameters = new DynamicParameters();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                parameters.Add("@json", jsonData);
                await connection.ExecuteAsync(
                    "dbo.UnpostedRemittanceTransaction",
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

        public async Task HistoryRemittanceTransaction( string jsonData)
        {
            var parameters = new DynamicParameters();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                parameters.Add("@json", jsonData);
                await connection.ExecuteAsync(
                    "dbo.RemittanceHistoryData",
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
    }
}
