
namespace YARC.Bus
{
    public interface IBusTimeouts
    {
        int EventQueueTimeout { get; }
        int RpcQueueTimeout { get; }
        int RpcRequestTimeout { get; }
        int ReconnectInterval { get; }
        int ConnectTimeout { get; }
    }
}
