using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Models;

namespace ProtoJsonInput;

public class Function1
{
    private readonly ILogger _logger;

    public Function1(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<Function1>();
    }

    [Function("Function1")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        var model = await RequestHandler.ParseBody<Model>(req);
        if (model is null)
        {
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        var result = new Model
        {
            Data = $"{model.Id}. {model.Data}",
            Id = Random.Shared.Next()
        };

        return await RequestHandler.GenerateResponse(req, result);
    }
}