using System.Runtime.CompilerServices;
using System.Threading.Channels;
using MesCopilot.Domain.Enums;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;

namespace MesCopilot.Infrastructure.Devices.OpcUa;

public sealed class OpcUaDeviceConnector : IDeviceConnector
{
    private readonly DeviceConnectionSettings _settings;
    private readonly int _equipmentId;
    private readonly Channel<DeviceReading> _readings = Channel.CreateBounded<DeviceReading>(100);
    private Session? _session;
    private Subscription? _subscription;

    public OpcUaDeviceConnector(DeviceConnectionSettings settings, int equipmentId)
    {
        _settings = settings;
        _equipmentId = equipmentId;
    }

    public DeviceProtocol Protocol => DeviceProtocol.OpcUa;

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        if (!_settings.AllowInsecure && string.IsNullOrWhiteSpace(_settings.CertificateThumbprint))
        {
            throw new InvalidOperationException("Secure OPC UA requires a trusted certificate thumbprint.");
        }

        string pkiPath = Path.Combine(AppContext.BaseDirectory, "pki-client");
        ApplicationConfiguration configuration = new()
        {
            ApplicationName = "MES Copilot OPC UA Client",
            ApplicationUri = $"urn:{Utils.GetHostName()}:MesCopilot:OpcUaClient",
            ApplicationType = ApplicationType.Client,
            SecurityConfiguration = new SecurityConfiguration
            {
                ApplicationCertificate = new CertificateIdentifier
                {
                    StoreType = "Directory",
                    StorePath = Path.Combine(pkiPath, "own"),
                    SubjectName = "CN=MES Copilot OPC UA Client"
                },
                TrustedPeerCertificates = new CertificateTrustList { StoreType = "Directory", StorePath = Path.Combine(pkiPath, "trusted") },
                TrustedIssuerCertificates = new CertificateTrustList { StoreType = "Directory", StorePath = Path.Combine(pkiPath, "issuer") },
                TrustedUserCertificates = new CertificateTrustList { StoreType = "Directory", StorePath = Path.Combine(pkiPath, "trustedUser") },
                UserIssuerCertificates = new CertificateTrustList { StoreType = "Directory", StorePath = Path.Combine(pkiPath, "userIssuer") },
                RejectedCertificateStore = new CertificateTrustList { StoreType = "Directory", StorePath = Path.Combine(pkiPath, "rejected") },
                AutoAcceptUntrustedCertificates = false,
                RejectSHA1SignedCertificates = true,
                MinimumCertificateKeySize = 2048
            },
            TransportConfigurations = new TransportConfigurationCollection(),
            TransportQuotas = new TransportQuotas { OperationTimeout = 10_000 },
            ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60_000 }
        };
        await configuration.Validate(ApplicationType.Client);
        configuration.CertificateValidator.CertificateValidation += (_, args) =>
        {
            string? trustedThumbprint = NormalizeThumbprint(_settings.CertificateThumbprint);
            string? presentedThumbprint = NormalizeThumbprint(args.Certificate?.Thumbprint);
            args.Accept = !string.IsNullOrWhiteSpace(trustedThumbprint) &&
                string.Equals(trustedThumbprint, presentedThumbprint, StringComparison.OrdinalIgnoreCase);
        };

        string endpointUrl = $"opc.tcp://{_settings.Host}:{_settings.Port}";
        bool useSecurity = !_settings.AllowInsecure;
        EndpointDescription selectedEndpoint = CoreClientUtils.SelectEndpoint(configuration, endpointUrl, useSecurity, 10_000);
        ConfiguredEndpoint endpoint = new(null, selectedEndpoint, EndpointConfiguration.Create(configuration));
        IUserIdentity identity = string.IsNullOrWhiteSpace(_settings.Username)
            ? new UserIdentity(new AnonymousIdentityToken())
            : new UserIdentity(_settings.Username, _settings.Password);
        _session = await Session.Create(
            configuration,
            endpoint,
            false,
            false,
            $"mes-copilot-{_equipmentId}",
            60_000,
            identity,
            null,
            cancellationToken);

        _subscription = new Subscription(_session.DefaultSubscription)
        {
            PublishingInterval = Math.Max(100, _settings.PollIntervalMilliseconds)
        };
        MonitoredItem item = new(_subscription.DefaultItem)
        {
            DisplayName = "EquipmentState",
            StartNodeId = NodeId.Parse(_settings.Endpoint),
            AttributeId = Attributes.Value,
            SamplingInterval = Math.Max(100, _settings.PollIntervalMilliseconds),
            QueueSize = 10,
            DiscardOldest = true
        };
        item.Notification += OnNotification;
        _subscription.AddItem(item);
        _session.AddSubscription(_subscription);
        _subscription.Create();
    }

    public async IAsyncEnumerable<DeviceReading> SubscribeAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (DeviceReading reading in _readings.Reader.ReadAllAsync(cancellationToken))
        {
            yield return reading;
        }
    }

    public Task DisconnectAsync(CancellationToken cancellationToken)
    {
        _subscription?.Delete(true);
        _subscription?.Dispose();
        _subscription = null;
        _session?.Close();
        _session?.Dispose();
        _session = null;
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync(CancellationToken.None);
        _readings.Writer.TryComplete();
    }

    private void OnNotification(MonitoredItem item, MonitoredItemNotificationEventArgs args)
    {
        foreach (DataValue value in item.DequeueValues())
        {
            string state = value.Value?.ToString() ?? EquipmentState.Offline.ToString();
            _readings.Writer.TryWrite(new DeviceReading(
                _equipmentId,
                state,
                value.SourceTimestamp == DateTime.MinValue ? DateTime.UtcNow : value.SourceTimestamp.ToUniversalTime()));
        }
    }

    private static string? NormalizeThumbprint(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
}
