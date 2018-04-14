
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage.Table;
using System.Threading.Tasks;
using KrakowDemo.Classes;
using System.Linq;

namespace KrakowDemo
{
    public static class HttpRetrieveOrder
    {
        [FunctionName(nameof(HttpRetrieveOrder))]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get",  Route = null)]HttpRequest req,
            [Table("Orders", Connection = "AzureStorageConnectionString")]CloudTable ordersTable, TraceWriter log)
        {
            string fileName = req.Query["fileName"];
            if (string.IsNullOrWhiteSpace(fileName))
                return new BadRequestResult();
            TableQuery<Order> query = new TableQuery<Order>().Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, fileName));
            TableQuerySegment<Order> tableQueryResult = await ordersTable.ExecuteQuerySegmentedAsync(query, null);
            var resultList = tableQueryResult.Results;

            if (resultList.Any())
            {
                var firstElement = resultList.First();
                return new JsonResult(new
                {
                    firstElement.CustomerName,
                    firstElement.Filename,
                    firstElement. SizeY,
                    firstElement.SizeX
                });
            }

            return new NotFoundResult();
        }
    }
}
