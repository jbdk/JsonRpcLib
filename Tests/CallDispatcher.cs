using System;
using System.IO;
using System.Text;
using FluentAssertions;
using FluentAssertions.Json;
using JsonRpcLib;
using JsonRpcLib.Server;
using Moq;
using Newtonsoft.Json.Linq;
using Tests.Fakes;
using Xunit;

namespace Tests
{
    public class UnitTest1
    {
        [Fact]
        public void CanCallInstanceHandler_GivenTypesArgs()
        {
            string reply = null;
            Func<IClient, string, bool> process = (client, data) => false;

            var handler = new TestHandlerFake();
            var server = new JsonRpcServer();
            var clientMock = new Mock<JsonRpcServer.ClientConnection>(1, "localhost", new MemoryStream(), process, Encoding.UTF8);
            clientMock.Setup(x => x.Write(It.IsAny<string>())).Callback<string>(s => reply = s);

            server.RegisterHandlers(handler);
            server.ExecuteHandler(clientMock.Object, 1, "FirstTest", new object[] { 1, "string", false, null });

            reply.Should().NotBeNull();
            var replyJson = JToken.Parse(reply);
            var a = JToken.Parse("{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":23}");
            replyJson.Should().BeEquivalentTo(a);
        }

        [Fact]
        public void CanCallInstanceHandler_GivenIntStringBoolNull()
        {
            string reply = null;
            Func<IClient, string, bool> process = (client, data) => false;

            var handler = new TestHandlerFake();
            var server = new JsonRpcServer();
            var clientMock = new Mock<JsonRpcServer.ClientConnection>(1, "localhost", new MemoryStream(), process, Encoding.UTF8);
            clientMock.Setup(x => x.Write(It.IsAny<string>())).Callback<string>(s => reply = s);

            var reqOring = new Request() { Id = 1, Method = "FirstTest", Params = new object[] { 1, "string", false, null } };
            var json = Serializer.Serialize(reqOring);
            var req = Serializer.Deserialize<Request>(json);

            server.RegisterHandlers(handler);
            server.ExecuteHandler(clientMock.Object, req.Id, req.Method, req.Params);

            reply.Should().NotBeNull();
            var replyJson = JToken.Parse(reply);
            var a = JToken.Parse("{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":23}");
            replyJson.Should().BeEquivalentTo(a);
        }

        [Fact]
        public void CanCallInstanceHandler_WithIntToByteConversion()
        {
            string reply = null;
            Func<IClient, string, bool> process = (client, data) => false;

            var handler = new TestHandlerFake();
            var server = new JsonRpcServer();
            var clientMock = new Mock<JsonRpcServer.ClientConnection>(1, "localhost", new MemoryStream(), process, Encoding.UTF8);
            clientMock.Setup(x => x.Write(It.IsAny<string>())).Callback<string>(s => reply = s);

            var reqOring = new Request() { Id = 81, Method = "ByteTest", Params = new object[] { 99 } };
            var json = Serializer.Serialize(reqOring);
            var req = Serializer.Deserialize<Request>(json);

            server.RegisterHandlers(handler);
            server.ExecuteHandler(clientMock.Object, req.Id, req.Method, req.Params);

            reply.Should().NotBeNull();
            var replyJson = JToken.Parse(reply);
            var a = JToken.Parse("{\"jsonrpc\":\"2.0\",\"id\":81}");
            replyJson.Should().BeEquivalentTo(a);
        }

        [Fact]
        public void CanCallInstanceHandler_WithDefaultArgsValues()
        {
            string reply = null;
            Func<IClient, string, bool> process = (client, data) => false;

            var handler = new TestHandlerFake();
            var server = new JsonRpcServer();
            var clientMock = new Mock<JsonRpcServer.ClientConnection>(1, "localhost", new MemoryStream(), process, Encoding.UTF8);
            clientMock.Setup(x => x.Write(It.IsAny<string>())).Callback<string>(s => reply = s);

            server.RegisterHandlers(handler);
            server.ExecuteHandler(clientMock.Object, 31, "DefaultArgs", new object[] { 123 });

            reply.Should().NotBeNull();
            var replyJson = JToken.Parse(reply);
            var a = JToken.Parse("{\"jsonrpc\":\"2.0\",\"id\":31}");
            replyJson.Should().BeEquivalentTo(a);
        }

        [Fact]
        public void CanCallInstanceHandler_WithUIntArg_GivenInt()
        {
            string reply = null;
            Func<IClient, string, bool> process = (client, data) => false;

            var handler = new TestHandlerFake();
            var server = new JsonRpcServer();
            var clientMock = new Mock<JsonRpcServer.ClientConnection>(1, "localhost", new MemoryStream(), process, Encoding.UTF8);
            clientMock.Setup(x => x.Write(It.IsAny<string>())).Callback<string>(s => reply = s);

            var reqOring = new Request() { Id = 81, Method = "UIntTest", Params = new object[] { uint.MaxValue } };
            var json = Serializer.Serialize(reqOring);
            var req = Serializer.Deserialize<Request>(json);

            server.RegisterHandlers(handler);
            server.ExecuteHandler(clientMock.Object, req.Id, req.Method, req.Params);

            reply.Should().NotBeNull();
            var replyJson = JToken.Parse(reply);
            var a = JToken.Parse("{\"jsonrpc\":\"2.0\",\"id\":81,\"result\":4294967295}");
            replyJson.Should().BeEquivalentTo(a);
        }

        [Fact]
        public void CallInstance_GivenDateTime()
        {
            string reply = null;
            Func<IClient, string, bool> process = (client, data) => false;

            var handler = new TestHandlerFake();
            var server = new JsonRpcServer();
            var clientMock = new Mock<JsonRpcServer.ClientConnection>(1, "localhost", new MemoryStream(), process, Encoding.UTF8);
            clientMock.Setup(x => x.Write(It.IsAny<string>())).Callback<string>(s => reply = s);

            var now = DateTime.Now;
            var reqOring = new Request() { Id = 81, Method = "DateTimeTest", Params = new object[] { now } };
            var json = Serializer.Serialize(reqOring);
            var req = Serializer.Deserialize<Request>(json);

            server.RegisterHandlers(handler);
            server.ExecuteHandler(clientMock.Object, req.Id, req.Method, req.Params);

            reply.Should().NotBeNull();
            var replyJson = JToken.Parse(reply);
            var a = JToken.Parse("{\"jsonrpc\":\"2.0\",\"id\":81}");
            replyJson.Should().BeEquivalentTo(a);
        }

        [Fact]
        public void CallInstance_GivenTimeSpan()
        {
            string reply = null;
            Func<IClient, string, bool> process = (client, data) => false;

            var handler = new TestHandlerFake();
            var server = new JsonRpcServer();
            var clientMock = new Mock<JsonRpcServer.ClientConnection>(1, "localhost", new MemoryStream(), process, Encoding.UTF8);
            clientMock.Setup(x => x.Write(It.IsAny<string>())).Callback<string>(s => reply = s);

            var reqOring = new Request() { Id = 81, Method = "TimeSpanTest", Params = new object[] { TimeSpan.FromHours(4) } };
            var json = Serializer.Serialize(reqOring);
            var req = Serializer.Deserialize<Request>(json);

            server.RegisterHandlers(handler);
            server.ExecuteHandler(clientMock.Object, req.Id, req.Method, req.Params);

            reply.Should().NotBeNull();
            var replyJson = JToken.Parse(reply);
            var a = JToken.Parse("{\"jsonrpc\":\"2.0\",\"id\":81}");
            replyJson.Should().BeEquivalentTo(a);
        }

        [Fact]
        public void CanCallInstanceHandler_GivenStringArray()
        {
            string reply = null;
            Func<IClient, string, bool> process = (client, data) => false;

            var handler = new TestHandlerFake();
            var server = new JsonRpcServer();
            var clientMock = new Mock<JsonRpcServer.ClientConnection>(1, "localhost", new MemoryStream(), process, Encoding.UTF8);
            clientMock.Setup(x => x.Write(It.IsAny<string>())).Callback<string>(s => reply = s);

            var reqOring = new Request() { Id = 3, Method = "StringArrayTest", Params = new object[] { new string[] { "one", "two", "tree", "four" } } };
            var json = Serializer.Serialize(reqOring);
            var req = Serializer.Deserialize<Request>(json);

            server.RegisterHandlers(handler);
            server.ExecuteHandler(clientMock.Object, req.Id, req.Method, req.Params);

            reply.Should().NotBeNull();
            var replyJson = JToken.Parse(reply);
            var a = JToken.Parse("{\"jsonrpc\":\"2.0\",\"id\":3,\"result\":4}");
            replyJson.Should().BeEquivalentTo(a);
        }
    }
}
