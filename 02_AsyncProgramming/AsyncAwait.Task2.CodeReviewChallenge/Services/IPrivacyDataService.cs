using System.Threading;
using System.Threading.Tasks;

namespace AsyncAwait.Task2.CodeReviewChallenge.Services;

public interface IPrivacyDataService
{
    Task<string> GetPrivacyDataAsync(CancellationToken cancellationToken);
}
