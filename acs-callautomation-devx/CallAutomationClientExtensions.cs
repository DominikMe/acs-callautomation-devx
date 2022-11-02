using Azure.Communication.CallAutomation;
using Azure.Messaging;

namespace CallAutomation.DevX
{
	public static class CallAutomationClientExtensions
	{
		private volatile static EventProcessor eventProcessor = new EventProcessor();

		public static async Task<EventAwaiter<CallConnected, CallDisconnected>> CreateCallAsync_(this CallAutomationClient client, CreateCallOptions options, CancellationToken cancellationToken = default)
		{
			var operationContext = options.OperationContext ?? Guid.NewGuid().ToString();
			options.OperationContext = operationContext;

			var response = await client.CreateCallAsync(options, cancellationToken);
			return new EventAwaiter<CallConnected, CallDisconnected>(operationContext, response.Value.CallConnection.CallConnectionId, eventProcessor, TimeSpan.FromMinutes(1));
		}

		public static async Task<EventAwaiter<RecognizeCompleted, RecognizeFailed>> StartRecognizingAsync_(this CallMedia callMedia, CallMediaRecognizeOptions recognizeOptions, CancellationToken cancellationToken = default)
		{
			var operationContext = recognizeOptions.OperationContext ?? Guid.NewGuid().ToString();
			recognizeOptions.OperationContext = operationContext;

			await callMedia.StartRecognizingAsync(recognizeOptions, cancellationToken);
			return new EventAwaiter<RecognizeCompleted, RecognizeFailed>(operationContext, callMedia.CallConnectionId, eventProcessor, TimeSpan.FromMinutes(5));
		}

		public static async Task<EventAwaiter<PlayCompleted, PlayFailed>> PlayToAllAsync_(this CallMedia callMedia, PlaySource playSource, PlayOptions playOptions = default, CancellationToken cancellationToken = default)
		{
			var operationContext = playOptions?.OperationContext ?? Guid.NewGuid().ToString();
			playOptions = playOptions ?? new PlayOptions();
			playOptions.OperationContext = operationContext;

			await callMedia.PlayToAllAsync(playSource, playOptions, cancellationToken);
			return new EventAwaiter<PlayCompleted, PlayFailed>(operationContext, callMedia.CallConnectionId, eventProcessor);
		}

		// Hang up doesn't take an operation context!
		public static async Task<EventAwaiter<CallDisconnected, CallConnected>> HangUpAsync_(this CallConnection callConnection, bool forEveryone, CancellationToken cancellationToken = default)
		{
			await callConnection.HangUpAsync(forEveryone, cancellationToken);
			return new EventAwaiter<CallDisconnected, CallConnected>(x => x is CallDisconnected disconnect && disconnect.CallConnectionId == callConnection.CallConnectionId, eventProcessor);
		}

		public static void ProcessEvents(this CallAutomationClient client, IEnumerable<CloudEvent> events)
			=> eventProcessor.ProcessEvents(events);

		internal static IReadOnlyList<CallAutomationEventBase> GetEvents(this CallAutomationClient client)
			=> eventProcessor.Events;
	}
}
