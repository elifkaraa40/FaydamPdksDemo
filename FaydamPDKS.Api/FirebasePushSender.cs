using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;

namespace FaydamPDKS.Api;

public sealed record PushMessage(
    string Token,
    string Title,
    string Body,
    IReadOnlyDictionary<string, string> Data);

public sealed record PushSendResult(bool Success, string? ErrorCode = null, bool InvalidToken = false);

public interface IFirebasePushSender
{
    bool IsAvailable { get; }
    Task<PushSendResult> SendAsync(PushMessage message, CancellationToken cancellationToken);
}

public sealed class FirebasePushSender : IFirebasePushSender, IDisposable
{
    private readonly FirebaseApp? _app;
    private readonly FirebaseMessaging? _messaging;
    private readonly ILogger<FirebasePushSender> _logger;

    public FirebasePushSender(
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<FirebasePushSender> logger)
    {
        _logger = logger;
        if (!configuration.GetValue("Firebase:Enabled", false)) return;

        try
        {
            var json = configuration["Firebase:ServiceAccountJson"];
            var path = configuration["Firebase:CredentialsPath"];
            GoogleCredential credential;
            if (!string.IsNullOrWhiteSpace(json))
            {
                credential = CredentialFactory
                    .FromJson<ServiceAccountCredential>(json)
                    .ToGoogleCredential();
            }
            else if (!string.IsNullOrWhiteSpace(path))
            {
                var resolvedPath = Path.IsPathRooted(path)
                    ? path
                    : Path.Combine(environment.ContentRootPath, path);
                credential = CredentialFactory
                    .FromFile<ServiceAccountCredential>(resolvedPath)
                    .ToGoogleCredential();
            }
            else
            {
                credential = GoogleCredential.GetApplicationDefault();
            }

            _app = FirebaseApp.Create(
                new AppOptions { Credential = credential },
                $"faydam-push-{Guid.NewGuid():N}");
            _messaging = FirebaseMessaging.GetMessaging(_app);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Firebase push servisi başlatılamadı. Push gönderimi devre dışı bırakıldı.");
        }
    }

    public bool IsAvailable => _messaging is not null;

    public async Task<PushSendResult> SendAsync(
        PushMessage message,
        CancellationToken cancellationToken)
    {
        if (_messaging is null) return new(false, "FIREBASE_NOT_CONFIGURED");
        try
        {
            var firebaseMessage = new Message
            {
#pragma warning disable CS0618 // FlutterFire getToken currently returns an FCM registration token.
                Token = message.Token,
#pragma warning restore CS0618
                Notification = new FirebaseAdmin.Messaging.Notification
                {
                    Title = message.Title,
                    Body = message.Body
                },
                Data = message.Data,
                Android = new AndroidConfig
                {
                    Priority = FirebaseAdmin.Messaging.Priority.High,
                    Notification = new AndroidNotification
                    {
                        ChannelId = "faydam_updates",
                        Sound = "default"
                    }
                }
            };
            await _messaging.SendAsync(firebaseMessage, cancellationToken);
            return new(true);
        }
        catch (FirebaseMessagingException exception)
        {
            var code = exception.MessagingErrorCode?.ToString() ?? exception.ErrorCode.ToString();
            var invalidToken = code.Equals("Unregistered", StringComparison.OrdinalIgnoreCase)
                || code.Equals("InvalidArgument", StringComparison.OrdinalIgnoreCase)
                || code.Equals("SenderIdMismatch", StringComparison.OrdinalIgnoreCase);
            _logger.LogWarning("Push bildirimi gönderilemedi. Kod: {ErrorCode}", code);
            return new(false, code, invalidToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Push bildirimi gönderilirken beklenmeyen hata oluştu.");
            return new(false, "PUSH_SEND_FAILED");
        }
    }

    public void Dispose() => _app?.Delete();
}
