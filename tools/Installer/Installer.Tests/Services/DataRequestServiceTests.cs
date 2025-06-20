﻿using Abs.CommonCore.Installer.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Installer.Tests.Services;

public class DataRequestServiceTests
{
    [Fact]
    public async Task DataDownloaded()
    {
        var dataRequest = new DataRequestService(NullLoggerFactory.Instance, true);
        var result = await dataRequest.RequestByteArrayAsync("http://Not.a.valid.url.path");
        Assert.Equal(Stream.Null, result);
    }

    [Fact]
    public async Task VerifyOnly_FileNotDownloaded()
    {
        var dataRequest = new DataRequestService(NullLoggerFactory.Instance, false);
        var result = await dataRequest.RequestByteArrayAsync("https://www.abswavesight.com/");
        Assert.True(result.Length > 0);
    }
}
