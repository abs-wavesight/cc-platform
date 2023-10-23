using System.Net;
using System.Net.Http.Headers;
using Microsoft.Azure.Functions.Worker.Http;
using ProtoBuf;

namespace ProtoJsonInput;

public class RequestHandler
{
    private const string AcceptHeader = "Accept";
    private const string ContentTypeHeader = "Content-Type";
    private const string ProtobufMediaType = "application/protobuf";
    private const string JsonMediaType = "application/json";
    public static async Task<T?> ParseBody<T>(HttpRequestData request) where T : class
    {
        if (!request.Headers.TryGetValues(ContentTypeHeader, out var values))
        {
            return null;
        }

        var header = values.FirstOrDefault();
        if (header is null
            || !MediaTypeHeaderValue.TryParse(header, out var type)
            || type?.MediaType is null)
        {
            return null;
        }

        if (type.MediaType.Equals(ProtobufMediaType, StringComparison.OrdinalIgnoreCase))
        {
            var body = request.Body;
            return Serializer.Deserialize<T>(body);
        }

        if (type.MediaType.Equals(JsonMediaType, StringComparison.OrdinalIgnoreCase))
        {
            return await request.ReadFromJsonAsync<T>();
        }

        return null;
    }

    public static async Task<HttpResponseData> GenerateResponse<T>(HttpRequestData req, T model,
        HttpStatusCode responseCode = HttpStatusCode.OK)
    {
        if (!req.Headers.TryGetValues(AcceptHeader, out var values))
        {
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        var acceptHeader = values.ToArray();
        var response = req.CreateResponse(responseCode);
        if (acceptHeader.Any(type => type.Equals(ProtobufMediaType, StringComparison.OrdinalIgnoreCase)))
        {
            
            response.Headers.Add(ContentTypeHeader, ProtobufMediaType);

            byte[] protobufData;
            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, model);
                protobufData = stream.ToArray();
            }

            await response.WriteBytesAsync(protobufData);
            return response;
        }

        await response.WriteAsJsonAsync(model);
        return response;
    }
}