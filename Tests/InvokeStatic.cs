using System;
using System.IO;
using System.Text;
using FluentAssertions;
using FluentAssertions.Json;
using JsonRpcLib;
using JsonRpcLib.Server;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Tests.Fakes;
using Xunit;

namespace Tests
{
    public class InvokeStatic
    {
        readonly Func<IClient, RentedBuffer, bool> _process = (client, data) => false;

        class StaticHandler
        {
            public static void Function1()
            {
            }
        }

        [Fact]
        public void Call_StaticFunction()
        {
            Response reply = default;

            var server = new JsonRpcServer();
            var clientMock = new Mock<JsonRpcServer.ClientConnection>(1, "localhost", new MemoryStream(), _process, Encoding.UTF8);
            clientMock.Setup(x => x.WriteAsJson(It.IsAny<object>())).Callback<object>(o => reply = (Response)o);

            server.Bind<StaticHandler>();
            server.ExecuteHandler(clientMock.Object, 43, "Function1", null);

            reply.Should().NotBeNull();
            reply.Error.Should().BeNull();
        }
    }
}
