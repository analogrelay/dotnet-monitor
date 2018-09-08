using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Trace
{
    public static class CancellationTokenExtensions
    {
        public static Task WaitForCancellationAsync(this CancellationTokenSource cancellationTokenSource) => cancellationTokenSource.Token.WaitForCancellationAsync();
        public static Task WaitForCancellationAsync(this CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<object>();
            cancellationToken.Register(() => tcs.TrySetResult(null));
            return tcs.Task;
        }
    }
}
