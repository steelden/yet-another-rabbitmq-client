using System;
using EasyNetQ;

namespace YARC.Bus
{
    internal sealed class EasyConventions : Conventions
    {
        private ExchangeNameConvention _oldExchangeNamingConvention;
        private QueueNameConvention _oldQueueNamingConvention;
        private RpcRoutingKeyNamingConvention _oldRpcRoutingKeyNamingConvention;
        private TopicNameConvention _oldTopicNamingConvention;

        public EasyConventions()
            : base(new TypeNameSerializer())
        {
            _oldExchangeNamingConvention = ExchangeNamingConvention;
            _oldQueueNamingConvention = QueueNamingConvention;
            _oldRpcRoutingKeyNamingConvention = RpcRoutingKeyNamingConvention;
            _oldTopicNamingConvention = TopicNamingConvention;

            RpcExchangeNamingConvention = () => BusNames.RpcRequestExchangeName;
            RpcReturnQueueNamingConvention = () => string.Format("{0}.{1}", BusNames.RpcResponseQueue, Guid.NewGuid());
            ErrorQueueNamingConvention = () => BusNames.ErrorsQueueName;
            ErrorExchangeNamingConvention = x => string.Format("{0}.{1}", BusNames.ErrorsExchangePrefix, x.RoutingKey);
            ExchangeNamingConvention = GetExchangeName;
            QueueNamingConvention = GetQueueName;
            RpcRoutingKeyNamingConvention = GetRpcRoutingKey;
            TopicNamingConvention = GetTopicName;
        }

        private string GetExchangeName(Type messageType)
        {
            return _oldExchangeNamingConvention(messageType);
        }

        private string GetQueueName(Type messageType, string subscriberId)
        {
            return _oldQueueNamingConvention(messageType, subscriberId);
        }

        private string GetRpcRoutingKey(Type messageType)
        {
            return _oldRpcRoutingKeyNamingConvention(messageType);
        }

        private string GetTopicName(Type messageType)
        {
            return _oldTopicNamingConvention(messageType);
        }
    }
}
