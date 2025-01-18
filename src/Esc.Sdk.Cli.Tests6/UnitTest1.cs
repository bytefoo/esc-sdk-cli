using Task = System.Threading.Tasks.Task;

namespace Esc.Sdk.Cli.Tests;

public class UnitTest1
{
    [Fact]
    public async Task Test()
    {
        try
        {
            var escPath = "C:\\Users\\dbeattie\\.pulumi\\bin\\esc.exe";
            var options = new EscOptions
            {
                EscPath = escPath,
                OrgName = "jetlinx",
                ProjectName = "test",
                EnvironmentName = "test",
                PulumiAccessToken = Environment.GetEnvironmentVariable("PULUMI_ACCESS_TOKEN"),
                UseCache = true
            };

            var escConfig = new EscConfig(options);

            //escConfig.Set("Foo_Bar_2", "asdf123", true);
            //escConfig.Set("test.Foo_Bar_2", "asdf123", true);
            //var success = escConfig.TryLoad(out var config);
            //var success = escConfig.TryLoad(out var config, out var exception);

            escConfig.Remove("settings.infra.apicloud");
            escConfig.Set(new List<(string Path, string Value, bool IsSecret)>
            {
                ("settings.infra.apicloud.one", "1", false),
                ("settings.infra.apicloud.two", "2", false),
                ("settings.infra.apicloud.three", "3", false),
            });
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
}