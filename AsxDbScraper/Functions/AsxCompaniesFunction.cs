using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using AsxDbScraper.Services;
using Microsoft.Extensions.Logging;

namespace AsxDbScraper.Functions;

public class AsxCompaniesFunction
{
    private readonly IAsxCompanyService _companyService;
    private readonly ILogger<AsxCompaniesFunction> _logger;

    public AsxCompaniesFunction(
        IAsxCompanyService companyService,
        ILogger<AsxCompaniesFunction> logger)
    {
        _companyService = companyService;
        _logger = logger;
    }

    [Function("UpdateCompaniesList")]
    public async Task<HttpResponseData> UpdateCompaniesListAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        try
        {
            _logger.LogInformation("Triggering ASX companies list update");
            await _companyService.UpdateCompaniesListAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new { message = "Companies list updated successfully" });
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating companies list");
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(new { message = "Internal server error" });
            return response;
        }
    }

    [Function("GetAllCompanies")]
    public async Task<HttpResponseData> GetAllCompaniesAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
    {
        try
        {
            var companies = await _companyService.GetAllCompaniesAsync();
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(companies);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching companies list");
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(new { message = "Internal server error" });
            return response;
        }
    }

    [Function("GetCompanyByCode")]
    public async Task<HttpResponseData> GetCompanyByCodeAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "companies/{code}")] HttpRequestData req,
        string code)
    {
        try
        {
            var company = await _companyService.GetCompanyByCodeAsync(code);
            if (company == null)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteAsJsonAsync(new { message = $"Company with code {code} not found" });
                return notFoundResponse;
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(company);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching company with code {Code}", code);
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(new { message = "Internal server error" });
            return response;
        }
    }
}