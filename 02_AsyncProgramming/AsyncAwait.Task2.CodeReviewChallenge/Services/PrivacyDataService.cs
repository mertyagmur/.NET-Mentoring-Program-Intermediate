using System.Threading;
using System.Threading.Tasks;

namespace AsyncAwait.Task2.CodeReviewChallenge.Services;

public class PrivacyDataService : IPrivacyDataService
{
    public Task<string> GetPrivacyDataAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        const string message = "This Policy describes how async/await processes your personal data," +
                               " but it may not address all possible data processing scenarios.";

        return Task.FromResult(message);
    }
}
