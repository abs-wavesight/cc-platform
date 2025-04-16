using Abs.CommonCore.Installer.Actions.Models;

namespace Abs.CommonCore.Installer.Services;
public class DockerService
{
    public static List<DockerContainerInfoModel> ParceDockerPsCommand(List<string> cmdResult)
    {
        var result = new List<DockerContainerInfoModel>();
        var i = 0;
        while (!cmdResult[i].Contains("CONTAINER ID"))
        {
            i++;
        }

        var headers = new[] {
            "CONTAINER ID",
            "IMAGE",
            "COMMAND",
            "CREATED",
            "STATUS",
            "PORTS",
            "NAMES",
        };

        var startIndicies = headers.Select(h => cmdResult[i].IndexOf(h)).ToArray();

        for (var j = i+1; j < cmdResult.Count; j++)
        {
            var line = cmdResult[j];
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var parts = new string[headers.Length];
            for (var k = 0; k < headers.Length; k++)
            {
                var startIndex = startIndicies[k];
                var endIndex = k == headers.Length - 1 ? line.Length : startIndicies[k + 1];
                parts[k] = line.Substring(startIndex, endIndex - startIndex).Trim();
            }
                        
            var image = parts[1].Split(":");

            var container = new DockerContainerInfoModel
            {
                ContainerId = parts[0],
                ImageName = image[0],
                ImageTag = image[1],
                Command = parts[2],
                Created = parts[3],
                Status = parts[4],
                Ports = parts[5],
                Names = parts[6]
            };
            result.Add(container);
        }

        return result;
    }
}
