using System.Diagnostics;
using Task = System.Threading.Tasks.Task;

namespace Esc.Sdk.Cli.Tests;

public class UnitTest1
{
    [Theory]
    [InlineData("env init", "testOrg", "testProject", "testEnv", "", "", false,false, "env init testOrg/testProject/testEnv")]
    [InlineData("env rm", "testOrg", "testProject", "testEnv", "", "", false,true, "env rm testOrg/testProject/testEnv --yes")]
    [InlineData("env rm", "testOrg", "testProject", "testEnv", "foo.bar.123", "", false,true, "env rm testOrg/testProject/testEnv foo.bar.123 --yes")]
    [InlineData("env set", "testOrg", "testProject", "testEnv", "foo.bar", "123", false, false, "env set testOrg/testProject/testEnv foo.bar 123")]
    [InlineData("env set", "testOrg", "testProject", "testEnv", "foo.bar", "123", true, false, "env set testOrg/testProject/testEnv foo.bar 123 --secret")]
    public void BuildArguments_ShouldReturnCorrectArgumentsString(string command, string orgName, string projectName, string environmentName, string path, string value, bool isSecret, bool skipConfirmation, string expected)
    {
        // Arrange
        var escConfig = new EscConfig();

        // Act
        var result = escConfig.BuildCommand(command, orgName, projectName, environmentName, path, value, isSecret, skipConfirmation);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Test()
    {
        var escPath = "C:\\Users\\dbeattie\\.pulumi\\bin\\esc.exe";
        
        const string orgName = "jetlinx";
        var projectName = "Infrastructure";
        const string envName = "test";

        var Name = "biz-solutions";
        var escEnvName = $"{envName}_{Name}";
        var escConfig = new EscConfig(new EscOptions
        {
            EscPath = escPath,
            OrgName = orgName,
            ProjectName = projectName,
            EnvironmentName = escEnvName,
            PulumiAccessToken = Environment.GetEnvironmentVariable("PULUMI_ACCESS_TOKEN"),
            UseCache = false
        });
        escConfig.RemoveEnvironment();
        escConfig.Init();
        var path = "AppSettings_Infrastructure_ClientPortal";
        escConfig.Set(new List<(string path, string value, bool isSecret)>
        {
            ($"{path}_SomeValue1", "someValue1", false),
            ($"{path}_SomeValue2", "someValue2", false),
        });

        Name = "document-api";
        escEnvName = $"{envName}_{Name}";
        escConfig = new EscConfig(new EscOptions
        {
            EscPath = escPath,
            OrgName = orgName,
            ProjectName = projectName,
            EnvironmentName = escEnvName,
            PulumiAccessToken = Environment.GetEnvironmentVariable("PULUMI_ACCESS_TOKEN"),
            UseCache = false
        });
        escConfig.RemoveEnvironment();
        escConfig.Init();
        path = "AppSettings_Infrastructure_DocumentApi";
        escConfig.Set(new List<(string path, string value, bool isSecret)>
        {
            ($"{path}_SomeValue1", "someValue1", false),
            ($"{path}_SomeValue2", "someValue2", false),
        });

        escConfig = new EscConfig(new EscOptions
        {
            EscPath = escPath,
            OrgName = orgName,
            ProjectName = projectName,
            EnvironmentName = envName,
            PulumiAccessToken = Environment.GetEnvironmentVariable("PULUMI_ACCESS_TOKEN"),
            UseCache = false
        });
        escConfig.RemoveEnvironment();
        escConfig.Init();

        var projects = escConfig.List(projectName)
            .Where(s => s.Contains($"{envName}_"))
            .ToList();

        var index = 0;
        foreach (var project in projects)
        {
            //orgName/projectName/environmentName
            var split = project.Split('/');
            var importName = split.Last();

            escConfig.Set($"imports[{index}]", $"{projectName}/{importName}");
            index++;
        }
    }
}
