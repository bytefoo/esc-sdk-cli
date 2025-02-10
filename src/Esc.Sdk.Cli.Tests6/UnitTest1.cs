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
    [InlineData("env get", "testOrg", "testProject", "testEnv", "foo.bar", "string", false, false, "env get testOrg/testProject/testEnv foo.bar --value string")]
    [InlineData("env get", "testOrg", "testProject", "testEnv", "foo.bar", "json", false, false, "env get testOrg/testProject/testEnv foo.bar --value json")]
    [InlineData("env get", "testOrg", "testProject", "testEnv", "foo.bar", "string", true, false, "env get testOrg/testProject/testEnv foo.bar --value string --show-secrets")]
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
        var escPath = "C:\\Users\\foo\\.pulumi\\bin\\esc.exe";
        
        const string orgName = "orgName";
        var projectName = "Infrastructure";
        const string envName = "test2";

        var Name = "someproj";
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
        var path = "AppSettings_Infrastructure_Application1";
        escConfig.Set(new List<(string path, string value, bool isSecret)>
        {
            ($"{path}_SomeValue1", "someValue1", false),
            ($"{path}_SomePassword", "someValue2", false),
        });
    }
}
