using System;
using System.Security.Cryptography;
using System.Text;

namespace YARC.Bus
{
    public static class BusNames
    {
        public static readonly string EventsExchangeName = "integration.events.exchange";
        public static readonly string CommandsExchangeName = "integration.commands.exchange";
        public static readonly string RpcRequestExchangeName = "integration.rpc.request.exchange";
        public static readonly string RpcResponseExchangeName = "integration.rpc.response.exchange";

        //public static readonly string EventsQueuePrefix = "event.queue";
        //public static readonly string CommandsQueuePrefix = "command.queue";
        public static readonly string RpcRequestQueuePrefix = "rpc.request.queue";
        public static readonly string RpcResponseQueue = "rpc.response.queue";

        public static readonly string ErrorsExchangePrefix = "integration.error.exchange";
        public static readonly string ErrorsQueueName = "integration.error.queue";

        public const string EventKeyPrefix = "event";
        public const string CommandKeyPrefix = "command";
        public const string RpcKeyPrefix = "rpc";

        private static string GetMachineCode()
        {
            using (var md5 = new MD5CryptoServiceProvider())
            {
                var bytes = Encoding.UTF8.GetBytes(Environment.MachineName.ToLower());
                var hash = md5.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }
    }
}
