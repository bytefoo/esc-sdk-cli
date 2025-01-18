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

            //var secrets = new List<Secret>
            //{
            //    new Secret("foo.bar", "1"),
            //    new Secret("foo.baz", "1"),
            //    new Secret("foo.que", "1"),
            //};
            //escConfig.Set(secrets);

            escConfig.Remove("settings.infra.apicloud");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
}