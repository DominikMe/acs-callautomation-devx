using Azure.Communication.CallAutomation;
using Azure.Communication;
using Azure.Messaging;

namespace CallAutomation.DevX
{
	public static class WithEventAwaiter
	{
		public static void AddEventAwaiter(this WebApplication app)
		{
			Uri question = new("https://storageaccountacsbob523.blob.core.windows.net/public-files/question.wav");
			Uri rightAnswer = new("https://storageaccountacsbob523.blob.core.windows.net/public-files/rightAnswer.wav");
			Uri wrongAnswer = new("https://storageaccountacsbob523.blob.core.windows.net/public-files/wrongAnswer.wav");
			var appNumber = app.Configuration["OwnedPhoneNumber"];
			var baseUrl = app.Configuration["BaseUrl"];
			var applicationId = app.Configuration["CommunicationIdentity"];

			app.MapPost("/eventAwaiter/events", (CloudEvent[] cloudEvents, CallAutomationClient callAutomationClient) =>
				callAutomationClient.ProcessEvents(cloudEvents));

			app.MapPost("/eventAwaiter/run", async (CallAutomationClient callAutomationClient, string targetNumber) =>
			{
				var createCall = await callAutomationClient.CreateCallAsync__(
					new CreateCallOptions(
						new CallSource(
							new CommunicationUserIdentifier(applicationId))
						{
							CallerId = new PhoneNumberIdentifier(appNumber)
						},
						new CommunicationIdentifier[] { new PhoneNumberIdentifier(targetNumber) },
						new Uri($"{baseUrl}/eventAwaiter/events")));
				var callConnected = await createCall.WaitForEventAsync();

				var callMedia = callAutomationClient
							.GetCallConnection(callConnected.CallConnectionId)
							.GetCallMedia();

				var recognize = await callMedia.StartRecognizingAsync__(new CallMediaRecognizeDtmfOptions(new PhoneNumberIdentifier(targetNumber), 10)
				{
					InitialSilenceTimeout = TimeSpan.FromSeconds(10),
					InterruptPrompt = true,
					InterruptCallMediaOperation = true,
					Prompt = new FileSource(question)
				});

				var success = false;
				try
				{
					var recognizeCompleted = await recognize.WaitForEventAsync();
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
					var play = await callMedia.PlayToAllAsync__(success ? new FileSource(rightAnswer) : new FileSource(wrongAnswer));
					var playCompleted = await play.WaitForEventAsync();
					var callDisconnected = await (await callAutomationClient
							.GetCallConnection(callConnected.CallConnectionId)
							.HangUpAsync__(true))
							.WaitForEventAsync();
					Console.WriteLine($"Finished flow for phone number {targetNumber}");
				}
			});
		}
	}
}
