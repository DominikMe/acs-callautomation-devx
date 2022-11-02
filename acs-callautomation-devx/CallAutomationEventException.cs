using Azure.Communication.CallAutomation;

namespace CallAutomation.DevX
{
	public class CallAutomationEventException<T> : Exception where T : CallAutomationEventBase
	{
		internal CallAutomationEventException(T failureEvent) => FailureEvent = failureEvent;

		public T FailureEvent { get; init; }
	}
}
