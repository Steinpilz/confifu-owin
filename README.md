# Confifu.Owin

## Introduction

This repository contains simple **Owin Configuration** store abstractions & implementation based on `Confifu.Abstractions.IAppConfig` (checkout confifu [here](https://github.com/Steinpilz/confifu)). Depends on `Microsoft.Owin`'s `IAppBuilder`. Could be used by multiple modules to store their own `IAppBuilder` customization in a single place. 

The library is distributed as a nuget package `Confifu.Owin` on nuget's official package feed.

### Confifu.Owin

To get started using it in a library: 

```csharp

public static IAppConfig RegisterMyAwesomeModule(this IAppConfig appConfig)
{
  appConfig.UseOwin(c => {
    c.AddConfiguration(appBuilder => {
      // here goes IAppBiulder customization
      appBuilder.Use(new MyMiddleware());
    });
  });
}

```

To apply stored configuration in your App:

```csharp

public class Startup
{
  public void Configuration(IAppBuilder appBuilder)
  {
    // setup & run your Confifu App
    var app = new MyApp();
    app.Setup().Run();
    
    // here all IAppBuilder customizations from used modules are loaded
    var owinConfiguration = app.AppConfig.GetOwinConfiguration();
    owinConfiguration(appBuilder);
  }
}

```


## FAQ

## Questions & Issues

Use built-in github [issue tracker](https://github.com/Steinpilz/confifu-owin/issues)

## Maintainers
@ivanbenko

## Contribution

* Setup development environment:

1. Clone the repo
2. ```.paket\paket restore``` 
3. ```build target=build```
