using System;
using YARC.Messages.Bus;

namespace YARC.Messages.ExtApi
{
    [Serializable]
    public class ExtApiDataResponse : IMessage, ICanValidate
    {
        public string RequestId { get; set; }
        public int Part { get; set; }
        public int TotalParts { get; set; }
        public object Data { get; set; }

        public bool ErrorFlag { get; set; }
        public string ErrorMessage { get; set; }

        public bool Validate()
        {
            return RequestId.NotNullOrEmpty() &&
                Part.GreaterThanOrEqual(0) &&
                TotalParts.GreaterThanOrEqual(0);
        }
    }
}
