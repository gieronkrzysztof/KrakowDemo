
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using KrakowDemo.Classes;
using KrakowDemo.DTOs;
using System;

namespace KrakowDemo
{
    public static class CreateOrder
    {
        [FunctionName(nameof(CreateOrder))]
        public static IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")]HttpRequest req, 
            TraceWriter log, [Table("Orders", Connection = "AzureStorageConnectionString")] ICollector<Order> outputTable)
        {
            try
            {
                string requestBody = new StreamReader(req.Body).ReadToEnd();
                var data = JsonConvert.DeserializeObject<OrderDTO>(requestBody);
                outputTable.Add(new Order
                {
                    CustomerName = data.CustomerName,
                    Email = data.Email,
                    Filename = data.Filename,
                    SizeX = data.SizeX,
                    SizeY = data.SizeY,
                    RowKey = Guid.NewGuid().ToString(),
                    PartitionKey = "Orders"
                });
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }

            return new OkResult();
            //return name != null
            //    ? (ActionResult)new OkObjectResult($"Hello, {name}")
            //    : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
        }
    }
}
