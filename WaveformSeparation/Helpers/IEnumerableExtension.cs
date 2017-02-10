using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace HirosakiUniversity.Aldente.AES.WaveformSeparation.Helpers
{
	public static class IEnumerableExtension
	{
		// http://neue.cc/2014/03/14_448.html から拝借．

		public static async Task<R[]> ForEachAsync<T, R>(
			this IEnumerable<T> source,
			Func<T, Task<R>> action,
			int concurrency,
			CancellationToken cancellationToken = default(CancellationToken), 
			bool configureAwait = false)
		{
			if (source == null)
			{
				throw new ArgumentNullException("source");
			}
			if (action == null)
			{
				throw new ArgumentNullException("action");
			}
			if (concurrency <= 0)
			{
				throw new ArgumentOutOfRangeException("concurrencyには正の整数を与えて下さい．");
			}

			using (var semaphore = new SemaphoreSlim(initialCount: concurrency, maxCount: concurrency))
			{
				var exceptionCount = 0;
				var tasks = new List<Task<R>>();

				foreach (var item in source)
				{
					if (exceptionCount > 0)
					{
						break;
					}
					cancellationToken.ThrowIfCancellationRequested();

					await semaphore.WaitAsync(cancellationToken).ConfigureAwait(configureAwait);
					var task = action(item).ContinueWith<R>(t =>
					{
						semaphore.Release();
						if (t.IsFaulted)
						{
							Interlocked.Increment(ref exceptionCount);
							throw t.Exception;
						}
						return t.Result;
					});
					tasks.Add(task);
				}
		
				return await Task.WhenAll<R>(tasks.ToArray()).ConfigureAwait(configureAwait);
			}


		}
	}
}
