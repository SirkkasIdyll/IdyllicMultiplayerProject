using System.Threading.Tasks;
using Grpc.Core;
using Resources.ProtocolBuffers;
using static Resources.ProtocolBuffers.Greeter;

namespace IdyllicMultiplayerProject.Resources.ProtocolBuffers;

public class GreeterService : GreeterBase
{
    public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
    {
        return Task.FromResult(new HelloReply { Message = "Hello " + request.Name });
    }
}