
namespace YARC.Bus
{
    public sealed class BusTimeouts : IBusTimeouts
    {
        public const int DEFAULT_EVENT_QUEUE_TIMEOUT = 5 * 60;
        public const int DEFAULT_RPC_QUEUE_TIMEOUT = 5 * 60;
        public const int DEFAULT_RPC_REQUEST_TIMEOUT = 60;
        public const int DEFAULT_RECONNECT_INTERVAL = 3;
        public const int DEFAULT_CONNECT_TIMEOUT = 0;

        public int EventQueueTimeout { get; set; }
        public int RpcQueueTimeout { get; set; }
        public int RpcRequestTimeout { get; set; }
        public int ReconnectInterval { get; set; }
        public int ConnectTimeout { get; set; }

        public BusTimeouts()
        {
            EventQueueTimeout = DEFAULT_EVENT_QUEUE_TIMEOUT;
            RpcQueueTimeout = DEFAULT_RPC_QUEUE_TIMEOUT;
            RpcRequestTimeout = DEFAULT_RPC_REQUEST_TIMEOUT;
            ReconnectInterval = DEFAULT_RECONNECT_INTERVAL;
            ConnectTimeout = DEFAULT_CONNECT_TIMEOUT;
        }
    }
}
