using System;
using System.Threading.Tasks;
using YARC.Bus;
using NLog;
using YARC.Messages.Bus;
using YARC.Messages.ExtApi;

namespace YARC.ExtApi
{
    public class ExtApiIntegrationModule : IIntegrationModule
    {
        private readonly IExtApiConfiguration _config;
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public ExtApiIntegrationModule(IExtApiConfiguration config)
        {
            _config = config;
        }

        public void RegisterEndpoints(IBusConnection connection)
        {
            connection.ReceiveRpcRequest(ExtApiNames.RpcGetData, x => x.On<ExtApiDataRequest>(HandleDataRequestAsync));
        }

        private bool Validate(object message)
        {
            if (message == null) return false;
            var validator = message as ICanValidate;
            return validator == null || validator.Validate();
        }

        private void ErrorResponse(string requestId, Action<object> replyAction, bool isNotFound, string message, params object[] parameters)
        {
            string s = string.Format(message, parameters);
            _logger.Error(s);
            replyAction(new ExtApiStatusResponse()
            {
                RequestId = requestId,
                Status = isNotFound ? ExtApiRequestStatus.NotFound : ExtApiRequestStatus.Error,
                StatusMessage = s
            });
        }

        private void HandleError(Exception ex, string requestId, Action<object> replyAction, bool isNotFound, string message, params object[] args)
        {
            if (ex != null)
            {
                var aex = ex as AggregateException;
                var errorMessage = aex != null ? aex.InnerException.Message : ex.Message;
                var s = string.Format(message, args);
                _logger.Error(s, ex);
                ErrorResponse(requestId, replyAction, isNotFound, "{0}", errorMessage);
            }
            else
            {
                var s = string.Format(message, args);
                _logger.Error(s);
                ErrorResponse(requestId, replyAction, isNotFound, "{0}", s);
            }
        }

        private async Task HandleDataRequestAsync(ExtApiDataRequest message, Action<object> replyAction)
        {
            if (!Validate(message)) return;

            _logger.Info("Received ExtApi data request from [{0}] (id '{1}', provider name '{2}', object name '{3}', action '{4}').",
                message.RequestOrigin, message.RequestId, message.ProviderName, message.ObjectName, message.Action);

            var requestId = message.RequestId;

            var provider = _config.FindProvider(message.ProviderName, message.ObjectName, message.Action);
            if (provider == null)
            {
                HandleError(null, requestId, replyAction, true, "ExtApi data provider '{0}' for object name '{1}' and action '{2}' not found.",
                    message.ProviderName, message.ObjectName, message.Action);
                return;
            }

            IExtApiDataGenerator generator = null;
            try
            {
                generator = await provider(message.Id, message.Params);
            }
            catch (Exception ex)
            {
                HandleError(ex, requestId, replyAction, false, "ExtApi data provider '{0}' for object name '{1}' and action '{2}' failed:",
                    message.ProviderName, message.ObjectName, message.Action);
                return;
            }
            if (generator == null) return;

            try
            {
                int totalParts = generator.TotalParts;

                var statusResponse = new ExtApiStatusResponse()
                {
                    RequestId = requestId,
                    Status = ExtApiRequestStatus.Created,
                    TotalParts = totalParts
                };
                replyAction(statusResponse);

                for (int i = 0; i < totalParts; ++i)
                {
                    string pageData = null;
                    try
                    {
                        pageData = await generator.GetPart(i);
                    }
                    catch (Exception ex)
                    {
                        HandleError(ex, requestId, replyAction, false, "ExtApi data generator GetPart({0}) in provider '{1}' for object name '{2}' and action '{3}' failed:",
                            i, message.ProviderName, message.ObjectName, message.Action);
                        return;
                    }
                    replyAction(new ExtApiDataResponse()
                    {
                        RequestId = requestId,
                        Part = i,
                        TotalParts = totalParts,
                        Data = pageData,
                        ErrorFlag = false
                    });
                }
                statusResponse.Status = ExtApiRequestStatus.Ready;
                replyAction(statusResponse);
            }
            catch (Exception ex)
            {
                HandleError(ex, requestId, replyAction, false, "ExtApi request handler for provider '{0}' for object name '{1}' and action '{2}' failed:",
                    message.ProviderName, message.ObjectName, message.Action);
            }
            finally
            {
                generator.Dispose();
            }
        }
    }
}
