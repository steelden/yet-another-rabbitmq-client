using System;
using YARC.Messages.Bus;

namespace YARC.Messages.ExtApi
{
    [Serializable]
    public class ExtApiDataRequest : IMessage, ICanValidate
    {
        public string RequestId { get; set; }
        public string ProviderName { get; set; }
        public string ObjectName { get; set; }
        public string Action { get; set; }
        public string Id { get; set; }
        public string Params { get; set; }

        public string RequestOrigin { get; set; }
        public string AuthToken { get; set; }

        public bool Validate()
        {
            return RequestId.NotNullOrEmpty() &&
                ProviderName.NotNullOrEmpty() &&
                ObjectName.NotNullOrEmpty() &&
                Action.NotNullOrEmpty();
        }
    }
}
