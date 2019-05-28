namespace Confifu.Owin.Tests
{
    using System;
    using System.Collections.Generic;
    using Confifu.Abstractions;
    using Confifu.Owin.Config;
    using global::Owin;
    using NSubstitute;
    using Shouldly;
    using Xunit;

    public class AppFeatures
    {
        class TestApp : AppSetup
        {
            public TestApp() : base(new EmptyConfigVariables())
            {
            }
        }

        [Fact]
        public void it_always_return_valid_owin_configuration()
        {
            var app = new TestApp();

            app.Setup().Run();
            var owinConfiguration = app.AppConfig.GetOwinConfiguration();
            owinConfiguration.ShouldNotBeNull();
        }


        [Fact]
        public void it_detects_circular_dependency_in_stages_order()
        {
            var app = new TestApp();

            var calls = new List<string>();

            app.AppConfig.UseOwin(owin =>
            {
                owin.AddConfiguration("authorize", appBuilder => { calls.Add("authorize"); });
                owin.AddConfiguration("auth", appBuilder => { calls.Add("auth"); });
                owin.AddConfiguration("log", appBuilder => { calls.Add("log"); });

                owin.OrderStages("auth", "authorize", "log", "default", "auth");
            });

            Should.Throw<InvalidOperationException>(() => app.AppConfig.GetOwinConfiguration());
        }

        [Fact]
        public void it_does_not_smoke_when_stage_configuration_is_not_defined()
        {
            var app = new TestApp();

            var calls = new List<string>();

            app.AppConfig.UseOwin(owin =>
            {
                owin.AddConfiguration("authorize", appBuilder => { calls.Add("authorize"); });
                owin.AddConfiguration("log", appBuilder => { calls.Add("log"); });

                owin.OrderStages("auth", "authorize", "log");
            });

            var owinConfiguration = app.AppConfig.GetOwinConfiguration();
        }

        [Fact]
        public void it_ovewrites_configuration()
        {
            var app = new TestApp();
            var middleware1 = new object();
            var middleware2 = new object();
            app.AppConfig.UseOwin(c =>
            {
                c.AddConfiguration(appBuilder => { appBuilder.Use(middleware1); });
                c.SetConfiguration(appBuilder => { appBuilder.Use(middleware2); });
            });

            app.Setup().Run();

            var owinConfiguration = app.AppConfig.GetOwinConfiguration();

            var appBuilderMock = Substitute.For<IAppBuilder>();
            owinConfiguration(appBuilderMock);

            appBuilderMock.Received(0).Use(middleware1);
            appBuilderMock.Received(1).Use(middleware2);
        }

        [Fact]
        public void it_runs_staged_configuration_in_proper_order()
        {
            var app = new TestApp();

            var calls = new List<string>();

            app.AppConfig.UseOwin(owin =>
            {
                owin.AddConfiguration("authorize", appBuilder => { calls.Add("authorize"); });
                owin.AddConfiguration("auth", appBuilder => { calls.Add("auth"); });
                owin.AddConfiguration("log", appBuilder => { calls.Add("log"); });

                owin.OrderStages("auth", "authorize", "log");
            });

            var owinConfiguration = app.AppConfig.GetOwinConfiguration();

            var appBuilderMock = Substitute.For<IAppBuilder>();
            owinConfiguration(appBuilderMock);

            calls.ShouldBe(new[] {"auth", "authorize", "log"});
        }

        [Fact]
        public void it_stores_owin_configuration()
        {
            var app = new TestApp();
            var middleware1 = new object();
            var middleware2 = new object();
            app.AppConfig.UseOwin(c =>
            {
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
    }
}