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
    public class Notify
    {
        [Fact]
        public void NoReturnOnNotify()
        {
            string reply = null;
            Func<IClient, string, bool> process = (client, data) => false;

            var handler = new TestHandlerFake();
            var server = new JsonRpcServer();
            var clientMock = new Mock<JsonRpcServer.ClientConnection>(1, "localhost", new MemoryStream(), process, Encoding.UTF8);
            clientMock.Setup(x => x.Write(It.IsAny<string>())).Callback<string>(s => reply = s);

            server.Bind(handler);
            server.ExecuteHandler(clientMock.Object, -1, "ReturnStringArray", null);

            reply.Should().BeNull();
        }
    }
}
