using Azure.Communication.CallAutomation;
using Azure.Messaging;

namespace CallAutomation.DevX
{
	internal class EventProcessor
	{
		private List<CallAutomationEventBase> _events = new();
		public IReadOnlyList<CallAutomationEventBase> Events => _events.AsReadOnly();

		public EventHandler EventsReceived;

		public void ProcessEvents(IEnumerable<CloudEvent> events)
		{
			_events.AddRange(CallAutomationEventParser.ParseMany(events.ToArray()));
			var handlers = Interlocked.CompareExchange(ref EventsReceived, null, null);
			handlers?.Invoke(this, EventArgs.Empty);
		}
	}
}
