namespace Abs.CommonCore.Installer.Actions
{
    public abstract class ActionBase
    {
        protected void MergeParameters(Dictionary<string, string> source, Dictionary<string, string> parameters)
        {
            foreach (var parameter in parameters)
            {
                source[parameter.Key] = parameter.Value;
            }
        }

        protected string ReplaceConfigParameters(string text, Dictionary<string, string> parameters)
        {
            foreach (var parameter in parameters)
            {
                text = text.Replace(parameter.Key, parameter.Value, StringComparison.OrdinalIgnoreCase);
            }

            return text;
        }
    }
}
