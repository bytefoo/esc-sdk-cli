using Esc.Sdk.Cli;
using System.Security.Cryptography;
using Task = System.Threading.Tasks.Task;

namespace DynamicsGP.Sdk.Tests;

public class UnitTest1
{
    [Fact]
    public async Task Test()
    {
        try
        {
            var escPath = "D:\\bytefoo\\esc-sdk-cli\\esc.exe";
            var options = new EscOptions
            {
                EscPath = escPath,
                OrgName = "OrgName",
                ProjectName = "AppSettings",
                EnvironmentName = "test",
                PulumiAccessToken ="PulumiAccessToken",
            };

            var escConfig = new EscConfig(options);

            //escConfig.Set("Foo_Bar", "asdf123");
            //var success = escConfig.TryLoad(out var config);
            var success = escConfig.TryLoad(out var config, out var exception);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
}