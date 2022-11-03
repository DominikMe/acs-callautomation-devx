using Azure.Communication.CallAutomation;
using Azure.Communication;
using Azure.Messaging;

namespace CallAutomation.DevX
{
	public static class WithEventsImplicit
	{
		public static void AddImplicitEventHandling(this WebApplication app)
		{
			Uri question = new("https://storageaccountacsbob523.blob.core.windows.net/public-files/question.wav");
			Uri rightAnswer = new("https://storageaccountacsbob523.blob.core.windows.net/public-files/rightAnswer.wav");
			Uri wrongAnswer = new("https://storageaccountacsbob523.blob.core.windows.net/public-files/wrongAnswer.wav");
			var appNumber = app.Configuration["OwnedPhoneNumber"];
			var baseUrl = app.Configuration["BaseUrl"];
			var applicationId = app.Configuration["CommunicationIdentity"];

			app.MapPost("/implicitEvents/events", (CloudEvent[] cloudEvents, CallAutomationClient callAutomationClient) =>
				callAutomationClient.ProcessEvents(cloudEvents));

			app.MapPost("/implicitEvents/run", async (CallAutomationClient callAutomationClient, string targetNumber) =>
			{
				var callConnected = await callAutomationClient.CreateCallAsync_(
					new CreateCallOptions(
						new CallSource(
							new CommunicationUserIdentifier(applicationId))
						{
							CallerId = new PhoneNumberIdentifier(appNumber)
						},
						new CommunicationIdentifier[] { new PhoneNumberIdentifier(targetNumber) },
						new Uri($"{baseUrl}/implicitEvents/events")));

				var callMedia = callAutomationClient
							.GetCallConnection(callConnected.CallConnectionId)
							.GetCallMedia();

				var success = false;
				try
				{
					var recognizeCompleted = await callMedia.StartRecognizingAsync_(new CallMediaRecognizeDtmfOptions(new PhoneNumberIdentifier(targetNumber), 10)
					{
						InitialSilenceTimeout = TimeSpan.FromSeconds(10),
						InterruptPrompt = true,
						InterruptCallMediaOperation = true,
						Prompt = new FileSource(question)
					});
					var expectedTones = new[] { DtmfTone.Four, DtmfTone.Two, DtmfTone.Pound };

					if (expectedTones.SequenceEqual(recognizeCompleted.CollectTonesResult.Tones))
					{
						success = true;
					}
				}
				catch (CallAutomationEventException<RecognizeFailed>)
				{
					success = false;
				}
				finally
				{
					var playCompleted = await callMedia.PlayToAllAsync_(success ? new FileSource(rightAnswer) : new FileSource(wrongAnswer));
					var callDisconnected = await callAutomationClient
							.GetCallConnection(callConnected.CallConnectionId)
							.HangUpAsync_(true);
					Console.WriteLine($"Finished flow for phone number {targetNumber}");
				}
			});
		}
	}
}
