using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.IO;
using System.Threading;
using Microsoft.ProjectOxford.Face;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage;



// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace AzureFaceId
{
    [Route("api/[controller]")]
    public class UploadPersonDataController : Controller
    {

        const string _subscriptionKey = "";
        static FaceServiceClient faceServiceClient = new FaceServiceClient(_subscriptionKey, "https://southeastasia.api.cognitive.microsoft.com/face/v1.0");
        const string personGroupId = "anildwaid1";
        const string personGroupName = "anildwa1";

        // GET: api/values
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
        [HttpPost]
        public async Task<string> Post(HttpRequestMessage req)
        {

            Console.WriteLine("Uploading Image...");
            try
            {

                string response = "";

                var createPersonResult = await faceServiceClient.CreatePersonAsync(Request.Query["personGroupId"], Request.Query["personName"]);

                response = $"Person Created: {createPersonResult.PersonId.ToString()}\n";


                Console.WriteLine($"Person Created: {createPersonResult.PersonId.ToString()}\n");



                using (Stream s = Request.Body)
                {
                    var addPersonFaceResult = await faceServiceClient.AddPersonFaceAsync(Request.Query["personGroupId"], createPersonResult.PersonId, s);
                    response = response + $"Face Added: {addPersonFaceResult.PersistedFaceId.ToString()}\n";

                    Console.WriteLine($"Face Added: {addPersonFaceResult.PersistedFaceId.ToString()}\n");

                    CloudTable PersonInfo = await CreateTableAsync("PersonInfo");
                    TableBatchOperation batchOperation = new TableBatchOperation();
                    var personEntity = new PersonInfo()
                    { PersonName = Request.Query["personName"], PersonCompany = Request.Query["personCompany"] };

                    var tableEntity = personEntity as TableEntity;
                    tableEntity.PartitionKey = createPersonResult.PersonId.ToString();
                    tableEntity.RowKey = addPersonFaceResult.PersistedFaceId.ToString();


                    batchOperation.Insert(personEntity);
                    IList<TableResult> results = await PersonInfo.ExecuteBatchAsync(batchOperation);
                    if (results.Count > 0)
                    {
                        response = response + $"Table Updated\n";
                        Console.WriteLine($"Table Updated\n");

                    }

                    await faceServiceClient.TrainPersonGroupAsync(personGroupId);
                    Console.WriteLine($"Training Started\n");

                }


                return response;
            }
            catch (Exception ex)
            {

                return ex.ToString();
            }
            

        }

        public static async Task<CloudTable> CreateTableAsync(string tableName)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse("");
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            CloudTable table = tableClient.GetTableReference(tableName);
            try
            {
                if (await table.CreateIfNotExistsAsync())
                {
                    //Console.WriteLine("Created Table named: {0}", tableName);
                }
                else
                {
                    //Console.WriteLine("Table {0} already exists", tableName);
                }
            }
            catch (StorageException ex)
            {
                Console.WriteLine(ex.ToString());
                Console.ReadLine();
                throw;
            }
            //Console.WriteLine();
            return table;
        }






        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }



    public class PersonInfo : TableEntity
    {

        public string PersonName { get; set; }
        public string PersonCompany { get; set; }


        


    }
}
