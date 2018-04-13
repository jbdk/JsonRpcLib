using System;
using System.IO;
using System.Text;
using FluentAssertions;
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
    }
}
