using System;
using YARC.Messages.Bus;

namespace YARC.Messages.ExtApi
{
    [Serializable]
    public class ExtApiStatusResponse : IMessage, ICanValidate
    {
        public string RequestId { get; set; }
        public ExtApiRequestStatus Status { get; set; }
        public string StatusMessage { get; set; }
        public int TotalParts { get; set; }

        public bool IsCompleted()
        {
            return Status == ExtApiRequestStatus.Ready || Status == ExtApiRequestStatus.Error || Status == ExtApiRequestStatus.NotFound;
        }

        public bool IsFaulted()
        {
            return Status == ExtApiRequestStatus.Error || Status == ExtApiRequestStatus.NotFound;
        }

        public bool Validate()
        {
            return true;
        }
    }
}
