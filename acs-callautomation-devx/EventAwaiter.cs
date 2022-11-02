using Azure.Communication.CallAutomation;

namespace CallAutomation.DevX
{
	public class EventAwaiter<T, TFailure> where T : CallAutomationEventBase where TFailure : CallAutomationEventBase
	{
		private readonly Func<CallAutomationEventBase, bool> filter;
		private readonly EventProcessor eventProcessor;
		private readonly TaskCompletionSource<T> taskCompletionSource;
		private readonly Timer timer;

		internal EventAwaiter(string operationContext, string callConnectionId, EventProcessor eventProcessor, TimeSpan? timeout = null)
			: this(x => (x is T || x is TFailure) && x?.CallConnectionId == callConnectionId && x?.OperationContext == operationContext, eventProcessor, timeout) { }

		internal EventAwaiter(Func<CallAutomationEventBase, bool> filter, EventProcessor eventProcessor, TimeSpan? timeout = null)
		{
			this.filter = filter;
			this.eventProcessor = eventProcessor;
			taskCompletionSource = new TaskCompletionSource<T>();
			eventProcessor.EventsReceived += OnEventsReceived;
			timer = new Timer(new TimerCallback(TimerProc));
			timer.Change((int)(timeout?.TotalMilliseconds ?? TimeSpan.FromMinutes(1).TotalMilliseconds), 0);
		}


		private void TimerProc(object? state)
		{
			Timer t = (Timer)state;
			taskCompletionSource.SetException(new[] { new TimeoutException() });
			t.Dispose();
			eventProcessor.EventsReceived -= OnEventsReceived;
		}

		private void OnEventsReceived(object? sender, EventArgs e)
		{
			var theEvent = eventProcessor.Events.FirstOrDefault(filter);
			if (theEvent is null)
				return;

			if (theEvent is T tEvent)
			{
				taskCompletionSource.SetResult(tEvent);
			}
			else if (theEvent is TFailure tFailureEvent)
			{
				taskCompletionSource.SetException(new[] { new CallAutomationEventException<TFailure>(tFailureEvent) });
			}
			timer.Dispose();
			eventProcessor.EventsReceived -= OnEventsReceived;
		}

		public Task<T> WaitForEventAsync() => taskCompletionSource.Task;
	}
}
