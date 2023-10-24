using System.Net;
using System.Net.Http.Headers;
using Google.Protobuf;
using Microsoft.Azure.Functions.Worker.Http;

namespace ProtoJsonInput;

public class RequestHandler
{
    private const string AcceptHeader = "Accept";
    private const string ContentTypeHeader = "Content-Type";
    private const string ProtobufMediaType = "application/protobuf";
    private const string JsonMediaType = "application/json";
    public static async Task<T?> ParseBody<T>(HttpRequestData request) where T : class, IMessage, new()
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
            var t = new T();
            t.MergeFrom(request.Body);
            return t;
        }

        if (type.MediaType.Equals(JsonMediaType, StringComparison.OrdinalIgnoreCase))
        {
            return await request.ReadFromJsonAsync<T>();
        }

        return null;
    }

    public static async Task<HttpResponseData> GenerateResponse<T>(HttpRequestData req, T model,
        HttpStatusCode responseCode = HttpStatusCode.OK) where T : IMessage
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

            var protobufData = model.ToByteArray();
            await response.WriteBytesAsync(protobufData);
            return response;
        }

        await response.WriteAsJsonAsync(model);
        return response;
    }
}