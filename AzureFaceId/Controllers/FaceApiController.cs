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



// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace AzureFaceId
{
    [Route("api/[controller]")]
    public class FaceApiController : Controller
    {

        const string _subscriptionKey = "";
        static FaceServiceClient faceServiceClient = new FaceServiceClient(_subscriptionKey, "https://southeastasia.api.cognitive.microsoft.com/face/v1.0");
        const string personGroupId = "MindtreeGroupId";
        const string personGroupName = "Mindtree";

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
            var response = "";

            CloudTable PersonInfo = await UploadPersonDataController.CreateTableAsync("PersonInfo");

            using (Stream s = Request.Body)
            {
                var faces = await faceServiceClient.DetectAsync(s);
                var faceIds = faces.Select(face => face.FaceId).ToArray();

                var results = await faceServiceClient.IdentifyAsync(personGroupId, faceIds);
                foreach (var identifyResult in results)
                {
                    Console.WriteLine("Result of face: {0}", identifyResult.FaceId);
                    if (identifyResult.Candidates.Length == 0)
                    {
                        Console.WriteLine("No one identified");
                    }
                    else
                    {
                        // Get top 1 among all candidates returned
                        var candidateId = identifyResult.Candidates[0].PersonId;
                        var person = await faceServiceClient.GetPersonAsync(personGroupId, candidateId);

                        var personInfo = await GetTableEntities<PersonInfo>(PersonInfo, person.PersonId.ToString());

                        response = $"{personInfo.First().PersonName},{personInfo.First().PersonCompany}"; 
                        //Console.WriteLine("Identified as {0}", person.Name);
                        
                       
                    }
                }
            }


            return response;

            //Stream datastream = await req.Content.ReadAsStreamAsync();

            //datastream.Position = 0;
           

        }


        public static async Task<List<T>> GetTableEntities<T>(CloudTable table, string partitionKey) where T : ITableEntity, new()
        {
            Console.WriteLine($"Fetching {typeof(T)}....");

            TableQuery<T> partitionScanQuery =
            new TableQuery<T>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey));

            TableContinuationToken token = null;


            long tickCount = Environment.TickCount;
            TableQuerySegment<T> segment;
            List<T> tableItems = new List<T>();


            do
            {
                segment = await table.ExecuteQuerySegmentedAsync<T>(partitionScanQuery, token);
                tableItems.AddRange(segment.ToList());
                token = segment.ContinuationToken;

            } while (token != null);

            long currentTick = Environment.TickCount;
            double elapsedTime = Math.Round((currentTick - tickCount) / 1000.0, 3);
            Console.WriteLine($"Fetch Completed for {typeof(T)}.");
            Console.Write("\rTotal Time Elapsed={0} secs.  Table Row Count:{1} ", Math.Round(elapsedTime, 3), tableItems.Count());

            return tableItems;

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
}

