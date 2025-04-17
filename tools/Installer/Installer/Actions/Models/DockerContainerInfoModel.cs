namespace Abs.CommonCore.Installer.Actions.Models;
public class DockerContainerInfoModel
{
    public string ContainerId { get; set; }
    public string ImageName { get; set; }
    public string ImageTag { get; set; }
    public string Command { get; set; }
    public string Created { get; set; }
    public string Status { get; set; }
    public string Ports { get; set; }
    public string Names { get; set; }
}
