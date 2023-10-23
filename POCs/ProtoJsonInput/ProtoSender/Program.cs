using System.Net.Http.Headers;
using ProtoBuf;
using System.Text;
using System.Text.Json;
using Models;

var model = new Model
{
    Id = Random.Shared.Next(),
    Data = "Some random Data"
};

byte[] protobufData;
using (var stream = new MemoryStream())
{
    Serializer.Serialize(stream, model);
    protobufData = stream.ToArray();
}
var protobufContent = new ByteArrayContent(protobufData);
protobufContent.Headers.Add("Content-Type", "application/protobuf");
await PostRequest(protobufContent, "application/protobuf");

var json = JsonSerializer.Serialize(model);
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
            var model = Serializer.Deserialize<Model>(response.Content.ReadAsStream());
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