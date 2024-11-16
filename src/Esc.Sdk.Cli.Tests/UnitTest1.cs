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
                ProjectName = "Sandbox",
                EnvironmentName = "test",
                PulumiAccessToken = "",
                UseCache = true
            };

            var escConfig = new EscConfig(options);

           // escConfig.Set("Foo_Bar_2", "asdf123", true);
            var success = escConfig.TryLoad(out var config);
            //var success = escConfig.TryLoad(out var config, out var exception);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
}