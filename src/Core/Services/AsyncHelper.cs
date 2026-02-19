using System.Threading;
using System.Threading.Tasks;

namespace AzureExplorer.Core.Services
{
    /// <summary>
    /// Provides helper methods for async operations with timeout support.
    /// </summary>
    internal static class AsyncHelper
    {
        /// <summary>
        /// Default timeout for Azure operations (30 seconds).
        /// </summary>
        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Executes an async operation with a timeout. Throws <see cref="TimeoutException"/>
        /// if the operation doesn't complete within the specified timeout.
        /// </summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="operation">The async operation to execute.</param>
        /// <param name="timeout">The timeout duration. If null, uses <see cref="DefaultTimeout"/>.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>The result of the operation.</returns>
        /// <exception cref="TimeoutException">Thrown when the operation times out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
        public static async Task<T> WithTimeoutAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            TimeSpan effectiveTimeout = timeout ?? DefaultTimeout;

            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                cts.CancelAfter(effectiveTimeout);

                try
                {
                    return await operation(cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    throw new TimeoutException(
                        $"The operation timed out after {effectiveTimeout.TotalSeconds:F0} seconds. " +
                        "Check your network connection and try again.");
                }
            }
        }

        /// <summary>
        /// Executes an async operation with a timeout. Throws <see cref="TimeoutException"/>
        /// if the operation doesn't complete within the specified timeout.
        /// </summary>
        /// <param name="operation">The async operation to execute.</param>
        /// <param name="timeout">The timeout duration. If null, uses <see cref="DefaultTimeout"/>.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <exception cref="TimeoutException">Thrown when the operation times out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
        public static async Task WithTimeoutAsync(
            Func<CancellationToken, Task> operation,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            TimeSpan effectiveTimeout = timeout ?? DefaultTimeout;

            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                cts.CancelAfter(effectiveTimeout);

                try
                {
                    await operation(cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    throw new TimeoutException(
                        $"The operation timed out after {effectiveTimeout.TotalSeconds:F0} seconds. " +
                        "Check your network connection and try again.");
                }
            }
        }

        /// <summary>
        /// Wraps a task with a timeout, returning a default value if the operation times out
        /// instead of throwing an exception.
        /// </summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="operation">The async operation to execute.</param>
        /// <param name="defaultValue">The value to return on timeout.</param>
        /// <param name="timeout">The timeout duration. If null, uses <see cref="DefaultTimeout"/>.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>The result of the operation, or <paramref name="defaultValue"/> on timeout.</returns>
        public static async Task<T> WithTimeoutOrDefaultAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            T defaultValue,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await WithTimeoutAsync(operation, timeout, cancellationToken).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                return defaultValue;
            }
        }
    }
}
