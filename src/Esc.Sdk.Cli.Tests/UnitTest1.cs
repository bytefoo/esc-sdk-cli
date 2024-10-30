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
            var escPath = "D:\\bytefoo\\esc-sdk-cli\\esc_win64.exe";

            var options = new EscOptions
            {
                EscPath = escPath,
                OrgName = "jetlinx",
                ProjectName = "AppSettings",
                EnvironmentName = "appsettings-test",
                //PulumiAccessToken = "",
            };

            var escConfig = new EscConfig(options);

            var success = escConfig.TryLoad(out var config);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
}