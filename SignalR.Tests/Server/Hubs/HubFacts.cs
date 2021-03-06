﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SignalR.Client.Hubs;
using SignalR.Hosting.Memory;
using SignalR.Hubs;
using SignalR.Tests.Infrastructure;
using Xunit;

namespace SignalR.Tests
{
    public class HubFacts : IDisposable
    {
        [Fact]
        public void ReadingState()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            var hub = connection.CreateProxy("demo");

            hub["name"] = "test";

            connection.Start(host).Wait();

            var result = hub.Invoke<string>("ReadStateValue").Result;

            Assert.Equal("test", result);

            connection.Stop();
        }

        [Fact]
        public void SettingState()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            var hub = connection.CreateProxy("demo");
            connection.Start(host).Wait();

            var result = hub.Invoke<string>("SetStateValue", "test").Result;

            Assert.Equal("test", result);
            Assert.Equal("test", hub["Company"]);

            connection.Stop();
        }

        [Fact]
        public void GetValueFromServer()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            var hub = connection.CreateProxy("demo");

            connection.Start(host).Wait();

            var result = hub.Invoke<int>("GetValue").Result;

            Assert.Equal(10, result);
            connection.Stop();
        }

        [Fact]
        public void TaskWithException()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            var hub = connection.CreateProxy("demo");

            connection.Start(host).Wait();

            var ex = Assert.Throws<AggregateException>(() => hub.Invoke("TaskWithException").Wait());

            Assert.IsType<InvalidOperationException>(ex.GetBaseException());
            Assert.Contains("System.Exception", ex.GetBaseException().Message);
            connection.Stop();
        }

        [Fact]
        public void GenericTaskWithException()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            var hub = connection.CreateProxy("demo");

            connection.Start(host).Wait();

            var ex = Assert.Throws<AggregateException>(() => hub.Invoke("GenericTaskWithException").Wait());

            Assert.IsType<InvalidOperationException>(ex.GetBaseException());
            Assert.Contains("System.Exception", ex.GetBaseException().Message);
            connection.Stop();
        }

        [Fact]
        public void Overloads()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            var hub = connection.CreateProxy("demo");

            connection.Start(host).Wait();

            hub.Invoke("Overload").Wait();
            int n = hub.Invoke<int>("Overload", 1).Result;

            Assert.Equal(1, n);
            connection.Stop();
        }

        [Fact]
        public void UnsupportedOverloads()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            var hub = connection.CreateProxy("demo");

            connection.Start(host).Wait();

            var ex = Assert.Throws<InvalidOperationException>(() => hub.Invoke("UnsupportedOverload", 13177).Wait());

            Assert.Equal("'UnsupportedOverload' method could not be resolved.", ex.GetBaseException().Message);
            connection.Stop();
        }

        [Fact]
        public void ChangeHubUrl()
        {
            var host = new MemoryHost();
            host.MapHubs("/foo");
            var connection = new Client.Hubs.HubConnection("http://site/foo", useDefaultUrl: false);

            var hub = connection.CreateProxy("demo");

            var wh = new ManualResetEvent(false);

            hub.On("signal", id =>
            {
                Assert.NotNull(id);
                wh.Set();
            });

            connection.Start(host).Wait();

            hub.Invoke("DynamicTask").Wait();

            Assert.True(wh.WaitOne(TimeSpan.FromSeconds(5)));
            connection.Stop();
        }

        [Fact]
        public void GuidTest()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://site/");

            var hub = connection.CreateProxy("demo");

            var wh = new ManualResetEvent(false);

            hub.On<Guid>("TestGuid", id =>
            {
                Assert.NotNull(id);
                wh.Set();
            });

            connection.Start(host).Wait();

            hub.Invoke("TestGuid").Wait();

            Assert.True(wh.WaitOne(TimeSpan.FromSeconds(5)));
            connection.Stop();
        }

        [Fact]
        public void HubHasConnectionEvents()
        {
            var type = typeof(Hub);
            // Hub has the disconnect method
            Assert.True(type.GetMethod("Disconnect") != null);

            // Hub has the connect method
            Assert.True(type.GetMethod("Connect") != null);

            // Hub has the reconnect method
            Assert.True(type.GetMethod("Reconnect") != null);
        }

        [Fact]
        public void ComplexPersonState()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://site/");

            var hub = connection.CreateProxy("demo");

            var wh = new ManualResetEvent(false);

            connection.Start(host).Wait();

            var person = new SignalR.Samples.Hubs.DemoHub.DemoHub.Person
            {
                Address = new SignalR.Samples.Hubs.DemoHub.DemoHub.Address
                {
                    Street = "Redmond",
                    Zip = "98052"
                },
                Age = 25,
                Name = "David"
            };

            var person1 = hub.Invoke<SignalR.Samples.Hubs.DemoHub.DemoHub.Person>("ComplexType", person).Result;
            var person2 = hub.GetValue<SignalR.Samples.Hubs.DemoHub.DemoHub.Person>("person");
            JObject obj = ((dynamic)hub).person;
            var person3 = obj.ToObject<SignalR.Samples.Hubs.DemoHub.DemoHub.Person>();

            Assert.NotNull(person1);
            Assert.NotNull(person2);
            Assert.NotNull(person3);
            Assert.Equal("David", person1.Name);
            Assert.Equal("David", person2.Name);
            Assert.Equal("David", person3.Name);
            Assert.Equal(25, person1.Age);
            Assert.Equal(25, person2.Age);
            Assert.Equal(25, person3.Age);
            Assert.Equal("Redmond", person1.Address.Street);
            Assert.Equal("Redmond", person2.Address.Street);
            Assert.Equal("Redmond", person3.Address.Street);
            Assert.Equal("98052", person1.Address.Zip);
            Assert.Equal("98052", person2.Address.Zip);
            Assert.Equal("98052", person3.Address.Zip);

            connection.Stop();
        }

        [Fact]
        public void DynamicInvokeTest()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://site/");
            string callback = @"!!!|\CallMeBack,,,!!!";

            var hub = connection.CreateProxy("demo");

            var wh = new ManualResetEvent(false);

            hub.On(callback, () => wh.Set());

            connection.Start(host).Wait();

            hub.Invoke("DynamicInvoke", callback).Wait();

            Assert.True(wh.WaitOne(TimeSpan.FromSeconds(5)));
            connection.Stop();
        }

        [Fact]
        public void CreateProxyAfterConnectionStartsThrows()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://site/");

            try
            {
                connection.Start(host).Wait();
                Assert.Throws<InvalidOperationException>(() => connection.CreateProxy("demo"));
            }
            finally
            {
                connection.Stop();
            }
        }

        [Fact(Skip = "Groups need fixing")]
        public void AddingToMultipleGroups()
        {
            var host = new MemoryHost();
            host.MapHubs();
            int max = 100;

            var countDown = new CountDownRange<int>(Enumerable.Range(0, max));
            var connection = new Client.Hubs.HubConnection("http://foo");
            var proxy = connection.CreateProxy("MultGroupHub");

            proxy.On<User>("onRoomJoin", user =>
            {
                Assert.True(countDown.Mark(user.Index));
            });

            connection.Start(host).Wait();

            for (int i = 0; i < max; i++)
            {
                proxy.Invoke("login", new User { Index = i, Name = "tester", Room = "test" + i }).Wait();
            }

            Assert.True(countDown.Wait(TimeSpan.FromSeconds(10)), "Didn't receive " + max + " messages. Got " + (max - countDown.Count) + " missed " + String.Join(",", countDown.Left.Select(i => i.ToString())));

            connection.Stop();
        }

        [Fact]
        public void CustomQueryStringRaw()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/", "a=b");

            var hub = connection.CreateProxy("CustomQueryHub");

            connection.Start(host).Wait();

            var result = hub.Invoke<string>("GetQueryString", "a").Result;

            Assert.Equal("b", result);

            connection.Stop();
        }

        [Fact]
        public void CustomQueryString()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var qs = new Dictionary<string, string>();
            qs["a"] = "b";
            var connection = new Client.Hubs.HubConnection("http://foo/", qs);

            var hub = connection.CreateProxy("CustomQueryHub");

            connection.Start(host).Wait();

            var result = hub.Invoke<string>("GetQueryString", "a").Result;

            Assert.Equal("b", result);

            connection.Stop();
        }

        [Fact]
        public void UsingHubAfterManualCreationThrows()
        {
            var hub = new SomeHub();
            Assert.Throws<InvalidOperationException>(() => hub.AllFoo());
            Assert.Throws<InvalidOperationException>(() => hub.OneFoo());
        }

        public class SomeHub : Hub
        {
            public void AllFoo()
            {
                Clients.foo();
            }

            public void OneFoo()
            {
                Caller.foo();
            }
        }

        public class CustomQueryHub : Hub
        {
            public string GetQueryString(string key)
            {
                return Context.QueryString[key];
            }
        }

        public class MultGroupHub : Hub
        {
            public Task Login(User user)
            {
                return Task.Factory.StartNew(
                    () =>
                    {
                        Groups.Remove(Context.ConnectionId, "foo").Wait();
                        Groups.Add(Context.ConnectionId, "foo").Wait();

                        Groups.Remove(Context.ConnectionId, user.Name).Wait();
                        Groups.Add(Context.ConnectionId, user.Name).Wait();
                        Clients[user.Name].onRoomJoin(user).Wait();
                    });
            }
        }

        public class User
        {
            public int Index { get; set; }
            public string Name { get; set; }
            public string Room { get; set; }
        }

        public void Dispose()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}
