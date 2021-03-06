﻿// Copyright 2004-2011 Castle Project - http://www.castleproject.org/
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Castle.MonoRail.Framework.Tests.Handlers
{
	using System;
	using System.IO;
	using System.Web;
	using Castle.MonoRail.Framework.Configuration;
	using Castle.MonoRail.Framework.Providers;
	using Castle.MonoRail.Framework.Routing;
	using Castle.MonoRail.Framework.Services;
	using Container;
	using Descriptors;
	using NUnit.Framework;
	using Rhino.Mocks;

	[TestFixture]
	public class MRHandlerFactoryTestCase
	{
		private readonly MockRepository mockRepository = new MockRepository();
		private MonoRailHttpHandlerFactory handlerFactory;
		private IServiceProviderLocator serviceProviderLocatorMock;
		private IMonoRailContainer container;
		private IControllerFactory controllerFactoryMock;
		private IAsyncController controllerMock;
		private IControllerDescriptorProvider controllerDescriptorProviderMock;
		private IControllerContextFactory controllerContextFactoryMock;

		[SetUp]
		public void Init()
		{
			container = mockRepository.DynamicMock<IMonoRailContainer>();
			serviceProviderLocatorMock = mockRepository.DynamicMock<IServiceProviderLocator>();
			controllerFactoryMock = mockRepository.DynamicMock<IControllerFactory>();
			controllerMock = mockRepository.DynamicMock<IAsyncController>();
			controllerDescriptorProviderMock = mockRepository.DynamicMock<IControllerDescriptorProvider>();
			controllerContextFactoryMock = mockRepository.DynamicMock<IControllerContextFactory>();

			SetupResult.For(container.UrlTokenizer).Return(new DefaultUrlTokenizer());
			SetupResult.For(container.UrlBuilder).Return(new DefaultUrlBuilder());
			SetupResult.For(container.EngineContextFactory).Return(new DefaultEngineContextFactory());
			SetupResult.For(container.ControllerFactory).Return(controllerFactoryMock);
			SetupResult.For(container.ControllerContextFactory).Return(controllerContextFactoryMock);
			SetupResult.For(container.ControllerDescriptorProvider).Return(controllerDescriptorProviderMock);
			SetupResult.For(container.StaticResourceRegistry).Return(new DefaultStaticResourceRegistry());

			handlerFactory = new MonoRailHttpHandlerFactory(serviceProviderLocatorMock);
			handlerFactory.ResetState();
			handlerFactory.Configuration = new MonoRailConfiguration();
			handlerFactory.Container = container;
		}

		[Test]
		public void Request_CreatesSessionfulHandler()
		{
			var writer = new StringWriter();
			
			var res = new HttpResponse(writer);
			var req = new HttpRequest(Path.Combine(
			                                  	AppDomain.CurrentDomain.BaseDirectory, "Handlers/Files/simplerequest.txt"),
			                                  "http://localhost:1333/home/something", "");
			var routeMatch = new RouteMatch();
			var httpCtx = new HttpContext(req, res);
			httpCtx.Items[RouteMatch.RouteMatchKey] = routeMatch;

			using(mockRepository.Record())
			{
				var controllerDesc = new ControllerMetaDescriptor
				{
					ControllerDescriptor = new ControllerDescriptor(typeof(Controller), "home", "", false)
				};

				Expect.Call(controllerFactoryMock.CreateController("", "home")).IgnoreArguments().Return(controllerMock);
				Expect.Call(controllerDescriptorProviderMock.BuildDescriptor(controllerMock)).Return(controllerDesc);
				var controllerContext = new ControllerContext
				{
					ControllerDescriptor = new ControllerMetaDescriptor()
				};
				Expect.Call(controllerContextFactoryMock.Create("", "home", "something", controllerDesc, routeMatch)).
					Return(controllerContext);
			}

			using(mockRepository.Playback())
			{
				var handler = handlerFactory.GetHandler(httpCtx, "GET", "", "");

				Assert.IsNotNull(handler);
				Assert.IsInstanceOf(typeof(MonoRailHttpHandler), handler);
			}
		}

		[Test]
		public void Request_CreatesSessionfulHandler_Async()
		{
			var writer = new StringWriter();

			var res = new HttpResponse(writer);
			var req = new HttpRequest(Path.Combine(
			                                  AppDomain.CurrentDomain.BaseDirectory, "Handlers/Files/simplerequest.txt"),
			                                  "http://localhost:1333/home/something", "");
			var routeMatch = new RouteMatch();
			var httpCtx = new HttpContext(req, res);
			httpCtx.Items[RouteMatch.RouteMatchKey] = routeMatch;

			using(mockRepository.Record())
			{
				var controllerDesc = new ControllerMetaDescriptor
				{
					ControllerDescriptor = new ControllerDescriptor(typeof(Controller), "home", "", false)
				};

				Expect.Call(controllerFactoryMock.CreateController("", "home")).IgnoreArguments().Return(controllerMock);
				Expect.Call(controllerDescriptorProviderMock.BuildDescriptor(controllerMock)).Return(controllerDesc);
				var controllerContext = new ControllerContext
				{
					ControllerDescriptor = new ControllerMetaDescriptor(), 
					Action = "something"
				};
				controllerContext.ControllerDescriptor.Actions["something"] = new AsyncActionPair(null, null, null);
				Expect.Call(controllerContextFactoryMock.Create("", "home", "something", controllerDesc, routeMatch)).
					Return(controllerContext);
			}

			using(mockRepository.Playback())
			{
				var handler = handlerFactory.GetHandler(httpCtx, "GET", "", "");

				Assert.IsNotNull(handler);
				Assert.IsInstanceOf(typeof(AsyncMonoRailHttpHandler), handler);
			}
        }

		[Test]
		public void Request_CreatesSessionlessHandler()
		{
			var writer = new StringWriter();

			var res = new HttpResponse(writer);
			var req = new HttpRequest(Path.Combine(
			                                  	AppDomain.CurrentDomain.BaseDirectory, "Handlers/Files/simplerequest.txt"),
			                                  "http://localhost:1333/home/something", "");
			var routeMatch = new RouteMatch();
			var httpCtx = new HttpContext(req, res);
			httpCtx.Items[RouteMatch.RouteMatchKey] = routeMatch;

			using(mockRepository.Record())
			{
				var controllerDesc = new ControllerMetaDescriptor
				{
					ControllerDescriptor = new ControllerDescriptor(typeof(Controller), "home", "", true)
				};

				Expect.Call(controllerFactoryMock.CreateController("", "home")).IgnoreArguments().Return(controllerMock);
				Expect.Call(controllerDescriptorProviderMock.BuildDescriptor(controllerMock)).Return(controllerDesc);
                var controllerContext = new ControllerContext
                {
                	ControllerDescriptor = new ControllerMetaDescriptor()
                };
				Expect.Call(controllerContextFactoryMock.Create("", "home", "something", controllerDesc, routeMatch)).
                    Return(controllerContext);
			}

			using(mockRepository.Playback())
			{
				var handler = handlerFactory.GetHandler(httpCtx, "GET", "", "");

				Assert.IsNotNull(handler);
				Assert.IsInstanceOf(typeof(SessionlessMonoRailHttpHandler), handler);
			}
		}

        [Test]
        public void Request_CreatesSessionlessHandler_Async()
        {
            var writer = new StringWriter();

            var res = new HttpResponse(writer);
            var req = new HttpRequest(Path.Combine(
                                                AppDomain.CurrentDomain.BaseDirectory, "Handlers/Files/simplerequest.txt"),
                                              "http://localhost:1333/home/something", "");
            var routeMatch = new RouteMatch();
            var httpCtx = new HttpContext(req, res);
            httpCtx.Items[RouteMatch.RouteMatchKey] = routeMatch;

            using (mockRepository.Record())
            {
                var controllerDesc = new ControllerMetaDescriptor
                {
                	ControllerDescriptor = new ControllerDescriptor(typeof(Controller), "home", "", true)
                };

            	Expect.Call(controllerFactoryMock.CreateController("", "home")).IgnoreArguments().Return(controllerMock);
                Expect.Call(controllerDescriptorProviderMock.BuildDescriptor(controllerMock)).Return(controllerDesc);
                var controllerContext = new ControllerContext
                {
                	Action = "something", 
					ControllerDescriptor = new ControllerMetaDescriptor()
                };
            	controllerContext.ControllerDescriptor.Actions["something"] = new AsyncActionPair(null,null,null);
                Expect.Call(controllerContextFactoryMock.Create("", "home", "something", controllerDesc, routeMatch)).
                    Return(controllerContext);
            }

            using (mockRepository.Playback())
            {
                var handler = handlerFactory.GetHandler(httpCtx, "GET", "", "");

                Assert.IsNotNull(handler);
                Assert.IsInstanceOf(typeof(AsyncSessionlessMonoRailHttpHandler), handler);
            }
        }
	}
}