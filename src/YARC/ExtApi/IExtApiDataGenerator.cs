using System;
using System.Threading.Tasks;

namespace YARC.ExtApi
{
    public interface IExtApiDataGenerator : IDisposable
    {
        int RecordsPerPart { get; }
        int TotalParts { get; }
        Task<string> GetPart(int partNo);
    }
}
