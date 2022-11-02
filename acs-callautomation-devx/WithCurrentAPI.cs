using Azure.Communication.CallAutomation;
using Azure.Communication;
using Azure.Messaging;

namespace CallAutomation.DevX
{
	public static class WithCurrentAPI
	{
		public record OpContext(string phoneNumber, string context);

		public static void AddCurrentApproach(this WebApplication app)
		{
			Uri question = new("https://storageaccountacsbob523.blob.core.windows.net/public-files/question.wav");
			Uri rightAnswer = new("https://storageaccountacsbob523.blob.core.windows.net/public-files/rightAnswer.wav");
			Uri wrongAnswer = new("https://storageaccountacsbob523.blob.core.windows.net/public-files/wrongAnswer.wav");
			var appNumber = app.Configuration["OwnedPhoneNumber"];
			var baseUrl = app.Configuration["BaseUrl"];
			var applicationId = app.Configuration["CommunicationIdentity"];

			// need to encode some data to not forget
			static string CreateOperationContext(string phoneNumber) => $"{phoneNumber}_{Guid.NewGuid()}";
			static (PhoneNumberIdentifier phoneNumber, Guid guid) ParseOperationContext(string operationContext)
			{
				var segments = operationContext.Split("_");
				return (new PhoneNumberIdentifier(segments[0]), Guid.Parse(segments[1]));
			}

			app.MapPost("/current/run", async (CallAutomationClient callAutomationClient, string targetNumber) =>
			{
				await callAutomationClient.CreateCallAsync(
					new CreateCallOptions(
						new CallSource(
							new CommunicationUserIdentifier(applicationId))
						{
							CallerId = new PhoneNumberIdentifier(appNumber)
						},
						new CommunicationIdentifier[] { new PhoneNumberIdentifier(targetNumber) },
						new Uri($"{baseUrl}/current/{CreateOperationContext(targetNumber)}")));
			});

			app.MapPost("/current/{contextId}", async (CloudEvent[] cloudEvents, string contextId, CallAutomationClient callAutomationClient, ILogger<Program> logger, IConfiguration configuration) =>
			{
				foreach (var cloudEvent in cloudEvents)
				{
					var @event = CallAutomationEventParser.Parse(cloudEvent);
					logger.LogInformation($"CorrelationId: {@event.CorrelationId} | CallConnectionId: {@event.CallConnectionId}");

					// 1 Question
					if (@event is CallConnected)
					{
						// need to get targetNumber from operationContext :-(
						var (targetNumber, _) = ParseOperationContext(contextId);

						await callAutomationClient
							.GetCallConnection(@event.CallConnectionId)
							.GetCallMedia()
							.StartRecognizingAsync(new CallMediaRecognizeDtmfOptions(targetNumber, 10)
							{
								InitialSilenceTimeout = TimeSpan.FromSeconds(10),
								InterruptPrompt = true,
								InterruptCallMediaOperation = true,
								Prompt = new FileSource(question),
								OperationContext = contextId,
							});
					}

					// 2a Answer
					if (@event is RecognizeCompleted recognizeCompleted)
					{
						// need to get targetNumber from operationContext :-(
						var (targetNumber, _) = ParseOperationContext(contextId);

						// a defensive check
						if (recognizeCompleted.OperationContext != contextId)
							throw new Exception("Why would this ever happen?");

						var tones = recognizeCompleted.CollectTonesResult.Tones;
						var expectedTones = new[] { DtmfTone.Four, DtmfTone.Two, DtmfTone.Pound };

						var callMedia = callAutomationClient
								.GetCallConnection(@event.CallConnectionId)
								.GetCallMedia();

						var fileSource = tones.SequenceEqual(expectedTones) ? new FileSource(rightAnswer) : new FileSource(wrongAnswer);

						// need to pipe through hint to end call after media playback
						await callMedia.PlayToAllAsync(fileSource, new PlayOptions { OperationContext = "EndCall" });
					}

					// 2b Failed to get answer
					if (@event is RecognizeFailed recognizeFailed)
					{
						await callAutomationClient
							.GetCallConnection(@event.CallConnectionId)
								.GetCallMedia()
								.PlayToAllAsync(new FileSource(wrongAnswer), new PlayOptions { OperationContext = "EndCall" });
					}

					// 3 end call
					if (@event is PlayCompleted playCompleted && playCompleted.OperationContext == "EndCall")
					{
						// there is no way to set an OperationContext with HangUp?
						await callAutomationClient
							.GetCallConnection(@event.CallConnectionId)
							.HangUpAsync(true);
					}

					// 4 after the call
					if (@event is CallDisconnected callDisconnected)
					{
						// I can't determine here if this event belongs to my hang up action
						// without storing a mapping from phoneNumber to connection id ...

						Console.WriteLine($"Connection {callDisconnected.CallConnectionId} ended");
					}
				}
			});
		}
	}
}
