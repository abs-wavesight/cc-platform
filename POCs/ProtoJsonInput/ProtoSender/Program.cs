using System.Net.Http.Headers;
using System.Text;
using Google.Protobuf;
using Models;
using Newtonsoft.Json.Schema.Generation;
using Newtonsoft.Json.Serialization;

var model = new Model
{
    Id = Random.Shared.Next(),
    Data = "Some random Data"
};

// To generate Json schema
//var generator = new JSchemaGenerator { ContractResolver = new CamelCasePropertyNamesContractResolver() };
//var schema = generator.Generate(typeof(Model));

byte[] protobufData = model.ToByteArray();
var protobufContent = new ByteArrayContent(protobufData);
protobufContent.Headers.Add("Content-Type", "application/protobuf");
await PostRequest(protobufContent, "application/protobuf");

var formatter = new JsonFormatter(JsonFormatter.Settings.Default);
var json = formatter.Format(model);
var jsonContent = new StringContent(json, Encoding.UTF8, "application/json");
await PostRequest(jsonContent, "application/json");

Console.ReadLine();
static async Task PostRequest(HttpContent content, string type)
{
    using var httpClient = new HttpClient();
    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(type));
    string apiUrl = "http://localhost:7004/api/Function1";
    var response = await httpClient.PostAsync(apiUrl, content);

    if (response.IsSuccessStatusCode)
    {
        Console.WriteLine("Request was successful.");

        if (type == "application/protobuf")
        {
            var model = new Model();
            model.MergeFrom(response.Content.ReadAsStream());
            Console.WriteLine($"{model.Id}. {model.Data}");
        }
        else
        {
            Console.WriteLine(await response.Content.ReadAsStringAsync());
        }
    }
    else
    {
        Console.WriteLine("Request failed with status code: " + response.StatusCode);
    }
}
