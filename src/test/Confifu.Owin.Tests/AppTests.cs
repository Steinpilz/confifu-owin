using Confifu.Abstractions;
using Confifu.Owin.Config;
using NSubstitute;
using Owin;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Confifu.Owin.Tests
{
    public class AppFeatures
    {
        [Fact]
        public void it_stores_owin_configuration()
        {
            var app = new TestApp();
            var middleware1 = new object();
            var middleware2 = new object();
            app.AppConfig.UseOwin(c => {
                c.AddConfiguration(appBuilder =>
                {
                    appBuilder.Use(middleware1);
                    appBuilder.Use(middleware2);
                });
            });

            app.Setup().Run();

            var owinConfiguration = app.AppConfig.GetOwinConfiguration();

            var appBuilderMock = Substitute.For<IAppBuilder>();
            owinConfiguration(appBuilderMock);

            appBuilderMock.Received(1).Use(middleware1);
            appBuilderMock.Received(1).Use(middleware2);
        }

        [Fact]
        public void it_ovewrites_configuration()
        {
            var app = new TestApp();
            var middleware1 = new object();
            var middleware2 = new object();
            app.AppConfig.UseOwin(c => {
                c.AddConfiguration(appBuilder =>
                {
                    appBuilder.Use(middleware1);
                });
                c.SetConfiguration(appBuilder => {
                    appBuilder.Use(middleware2);
                });
            });

            app.Setup().Run();

            var owinConfiguration = app.AppConfig.GetOwinConfiguration();

            var appBuilderMock = Substitute.For<IAppBuilder>();
            owinConfiguration(appBuilderMock);

            appBuilderMock.Received(0).Use(middleware1);
            appBuilderMock.Received(1).Use(middleware2);
        }


        [Fact]
        public void it_always_return_valid_owin_configuration()
        {
            var app = new TestApp();

            app.Setup().Run();
            var owinConfiguration = app.AppConfig.GetOwinConfiguration();
            owinConfiguration.ShouldNotBeNull();
        }
        
        class TestApp : AppSetup
        {
            public TestApp() : base(new EmptyConfigVariables())
            { }
        }
    }
}
