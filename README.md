# extensions-configuration-envkey

based on: https://github.com/Azure/AppConfiguration-DotnetProvider

```
using ByteFoo.Extensions.Configuration.EnvKey;

c.AddEnvKeyConfiguration(options =>
{
    options
        .Connect("EnvKey")
        .Cache(false)
        .Path(envKeyPath)
        .MapKeys(s => new ValueTask<string>(s.Replace("_", ":")))
        .MapValues(s => new ValueTask<string>(s.Replace("foo", "bar")))
        .Select("SomePrefix:AppSettings*")
        .TrimKeyPrefix("SomePrefix:");
});
```