using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace Abs.CommonCore.Platform.Certificates;

public static class CertificateImporter
{
    public static void ImportLocalRabbitCertificates(
        ILogger logger,
        string pathEnvironmentVariable = CertificateConstants.LocalCertPath,
        string passwordEnvironmentVariable = CertificateConstants.LocalCertPassword)
    {
        ImportCertificateFromEnvironmentVariables(logger, pathEnvironmentVariable, passwordEnvironmentVariable);
    }

    public static void ImportRemoteRabbitCertificates(
        ILogger logger,
        string pathEnvironmentVariable = CertificateConstants.RemoteCertPath,
        string passwordEnvironmentVariable = CertificateConstants.RemoteCertPassword)
    {
        ImportCertificateFromEnvironmentVariables(logger, pathEnvironmentVariable, passwordEnvironmentVariable);
    }

    public static void ImportCertificateFromEnvironmentVariables(
        ILogger logger,
        string pathEnvironmentVariable,
        string passwordEnvironmentVariable)
    {
        var path = Environment.GetEnvironmentVariable(pathEnvironmentVariable);
        if (string.IsNullOrEmpty(path))
        {
            throw new Exception($"Certificate path environment variable ({pathEnvironmentVariable}) not found");
        }

        var password = Environment.GetEnvironmentVariable(passwordEnvironmentVariable);
        ImportCertificate(path, password, logger);
    }

    public static void ImportCertificate(string path, string? password, ILogger logger)
    {
        using var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
        store.Open(OpenFlags.ReadWrite);
        var certificate = string.IsNullOrWhiteSpace(password)
            ? new X509Certificate2(path)
            : new X509Certificate2(path, password);
        store.Add(certificate);
        store.Close();
        logger.LogInformation($"Imported certificate from {path}");
    }
}
