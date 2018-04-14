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
    public class InvokeInstance
    {
        [Fact]
        public void Call_GivenTypesArgs()
        {
            Response<object> reply = null;
            Func<IClient, string, bool> process = (client, data) => false;

            var handler = new TestHandlerFake();
            var server = new JsonRpcServer();
            var clientMock = new Mock<JsonRpcServer.ClientConnection>(1, "localhost", new MemoryStream(), process, Encoding.UTF8);
            clientMock.Setup(x => x.WriteAsJson(It.IsAny<object>())).Callback<object>(o => reply = (Response<object>)o);

            server.Bind(handler);
            server.ExecuteHandler(clientMock.Object, 1, "FirstTest", new object[] { 1, "string", false, null });

            reply.Should().NotBeNull();
            reply.Id.Should().Be(1);
            reply.Result.Should().Be(23);
        }

        [Fact]
        public void Call_GivenIntStringBoolNull()
        {
            Response<object> reply = null;
            Func<IClient, string, bool> process = (client, data) => false;

            var handler = new TestHandlerFake();
            var server = new JsonRpcServer();
            var clientMock = new Mock<JsonRpcServer.ClientConnection>(1, "localhost", new MemoryStream(), process, Encoding.UTF8);
            clientMock.Setup(x => x.WriteAsJson(It.IsAny<object>())).Callback<object>(o => reply = (Response<object>)o);

            var reqOring = new Request() { Id = 1, Method = "FirstTest", Params = new object[] { 1, "string", false, null } };
            var json = Serializer.Serialize(reqOring);
            var req = Serializer.Deserialize<Request>(json);

            server.Bind(handler);
            server.ExecuteHandler(clientMock.Object, req.Id, req.Method, req.Params);

            reply.Should().NotBeNull();
            reply.Id.Should().Be(1);
            reply.Result.Should().Be(23);
        }

        [Fact]
        public void Call_WithIntToByteConversion()
        {
            Response reply = null;
            Func<IClient, string, bool> process = (client, data) => false;

            var handler = new TestHandlerFake();
            var server = new JsonRpcServer();
            var clientMock = new Mock<JsonRpcServer.ClientConnection>(1, "localhost", new MemoryStream(), process, Encoding.UTF8);
            clientMock.Setup(x => x.WriteAsJson(It.IsAny<object>())).Callback<object>(o => reply = (Response)o);

            var reqOring = new Request() { Id = 81, Method = "ByteTest", Params = new object[] { 99 } };
            var json = Serializer.Serialize(reqOring);
            var req = Serializer.Deserialize<Request>(json);

            server.Bind(handler);
            server.ExecuteHandler(clientMock.Object, req.Id, req.Method, req.Params);

            reply.Should().NotBeNull();
            reply.Id.Should().Be(81);
            reply.Error.Should().BeNull();
        }

        [Fact]
        public void Call_WithDefaultArgsValues()
        {
            Response reply = null;
            Func<IClient, string, bool> process = (client, data) => false;

            var handler = new TestHandlerFake();
            var server = new JsonRpcServer();
            var clientMock = new Mock<JsonRpcServer.ClientConnection>(1, "localhost", new MemoryStream(), process, Encoding.UTF8);
            clientMock.Setup(x => x.WriteAsJson(It.IsAny<object>())).Callback<object>(o => reply = (Response)o);

            server.Bind(handler);
            server.ExecuteHandler(clientMock.Object, 31, "DefaultArgs", new object[] { 123 });

            reply.Should().NotBeNull();
            reply.Id.Should().Be(31);
            reply.Error.Should().BeNull();
        }

        [Fact]
        public void Call_WithUIntArg_GivenInt()
        {
            Response<object> reply = null;
            Func<IClient, string, bool> process = (client, data) => false;

            var handler = new TestHandlerFake();
            var server = new JsonRpcServer();
            var clientMock = new Mock<JsonRpcServer.ClientConnection>(1, "localhost", new MemoryStream(), process, Encoding.UTF8);
            clientMock.Setup(x => x.WriteAsJson(It.IsAny<object>())).Callback<object>(o => reply = (Response<object>)o);

            var reqOring = new Request() { Id = 81, Method = "UIntTest", Params = new object[] { uint.MaxValue } };
            var json = Serializer.Serialize(reqOring);
            var req = Serializer.Deserialize<Request>(json);

            server.Bind(handler);
            server.ExecuteHandler(clientMock.Object, req.Id, req.Method, req.Params);

            reply.Should().NotBeNull();
            reply.Id.Should().Be(81);
            reply.Error.Should().BeNull();
            reply.Result.Should().Be(uint.MaxValue);
        }

        [Fact]
        public void Call_GivenDateTime()
        {
            Response reply = null;
            Func<IClient, string, bool> process = (client, data) => false;

            var handler = new TestHandlerFake();
            var server = new JsonRpcServer();
            var clientMock = new Mock<JsonRpcServer.ClientConnection>(1, "localhost", new MemoryStream(), process, Encoding.UTF8);
            clientMock.Setup(x => x.WriteAsJson(It.IsAny<object>())).Callback<object>(o => reply = (Response)o);

            var now = DateTime.Now;
            var reqOring = new Request() { Id = 81, Method = "DateTimeTest", Params = new object[] { now } };
            var json = Serializer.Serialize(reqOring);
            var req = Serializer.Deserialize<Request>(json);

            server.Bind(handler);
            server.ExecuteHandler(clientMock.Object, req.Id, req.Method, req.Params);

            reply.Should().NotBeNull();
            reply.Id.Should().Be(81);
            reply.Error.Should().BeNull();
        }

        [Fact]
        public void Call_GivenTimeSpan()
        {
            Response reply = null;
            Func<IClient, string, bool> process = (client, data) => false;

            var handler = new TestHandlerFake();
            var server = new JsonRpcServer();
            var clientMock = new Mock<JsonRpcServer.ClientConnection>(1, "localhost", new MemoryStream(), process, Encoding.UTF8);
            clientMock.Setup(x => x.WriteAsJson(It.IsAny<object>())).Callback<object>(o => reply = (Response)o);

            var reqOring = new Request() { Id = 81, Method = "TimeSpanTest", Params = new object[] { TimeSpan.FromHours(4) } };
            var json = Serializer.Serialize(reqOring);
            var req = Serializer.Deserialize<Request>(json);

            server.Bind(handler);
            server.ExecuteHandler(clientMock.Object, req.Id, req.Method, req.Params);

            reply.Should().NotBeNull();
            reply.Id.Should().Be(81);
            reply.Error.Should().BeNull();
        }

        [Fact]
        public void Call_GivenStringArray()
        {
            Response<object> reply = null;
            Func<IClient, string, bool> process = (client, data) => false;

            var handler = new TestHandlerFake();
            var server = new JsonRpcServer();
            var clientMock = new Mock<JsonRpcServer.ClientConnection>(1, "localhost", new MemoryStream(), process, Encoding.UTF8);
            clientMock.Setup(x => x.WriteAsJson(It.IsAny<object>())).Callback<object>(o => reply = (Response<object>)o);

            var reqOring = new Request() { Id = 3, Method = "StringArrayTest", Params = new object[] { new string[] { "one", "two", "tree", "four" } } };
            var json = Serializer.Serialize(reqOring);
            var req = Serializer.Deserialize<Request>(json);

            server.Bind(handler);
            server.ExecuteHandler(clientMock.Object, req.Id, req.Method, req.Params);

            reply.Should().NotBeNull();
            reply.Id.Should().Be(3);
            reply.Error.Should().BeNull();
            reply.Result.Should().Be(4);
        }

        [Fact]
        public void ReturnPrimitiveArray()
        {
            Response<object> reply = null;
            Func<IClient, string, bool> process = (client, data) => false;

            var handler = new TestHandlerFake();
            var server = new JsonRpcServer();
            var clientMock = new Mock<JsonRpcServer.ClientConnection>(1, "localhost", new MemoryStream(), process, Encoding.UTF8);
            clientMock.Setup(x => x.WriteAsJson(It.IsAny<object>())).Callback<object>(o => reply = (Response<object>)o);

            var reqOring = new Request() { Id = 3, Method = "ReturnPrimitiveArray" };
            var json = Serializer.Serialize(reqOring);
            var req = Serializer.Deserialize<Request>(json);

            server.Bind(handler);
            server.ExecuteHandler(clientMock.Object, req.Id, req.Method, req.Params);

            reply.Should().NotBeNull();
            reply.Id.Should().Be(3);
            reply.Error.Should().BeNull();
            reply.Result.Should().BeEquivalentTo(new int[] { 1, 2, 3, 4, 5 });
        }

        [Fact]
        public void ReturnStringArray()
        {
            Response<object> reply = null;
            Func<IClient, string, bool> process = (client, data) => false;

            var handler = new TestHandlerFake();
            var server = new JsonRpcServer();
            var clientMock = new Mock<JsonRpcServer.ClientConnection>(1, "localhost", new MemoryStream(), process, Encoding.UTF8);
            clientMock.Setup(x => x.WriteAsJson(It.IsAny<object>())).Callback<object>(o => reply = (Response<object>)o);

            var reqOring = new Request() { Id = 3, Method = "ReturnStringArray" };
            var json = Serializer.Serialize(reqOring);
            var req = Serializer.Deserialize<Request>(json);

            server.Bind(handler);
            server.ExecuteHandler(clientMock.Object, req.Id, req.Method, req.Params);

            reply.Should().NotBeNull();
            reply.Id.Should().Be(3);
            reply.Error.Should().BeNull();
            reply.Result.Should().BeEquivalentTo(new string[] { "one", "two", "three" });
        }
    }
}
