using MesCopilot.Domain.Enums;
using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Server;

namespace MesCopilot.DeviceSimulator.Workers;

public sealed class OpcUaEquipmentServer : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpcUaEquipmentServer> _logger;
    private EquipmentStandardServer? _server;

    public OpcUaEquipmentServer(IConfiguration configuration, ILogger<OpcUaEquipmentServer> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        int port = _configuration.GetValue("OpcUa:Port", 4840);
        string pkiPath = Path.Combine(AppContext.BaseDirectory, "pki");
        ApplicationConfiguration configuration = new()
        {
            ApplicationName = "MES Copilot OPC UA Simulator",
            ApplicationUri = $"urn:{Utils.GetHostName()}:MesCopilot:Simulator",
            ProductUri = "urn:MesCopilot:Simulator",
            ApplicationType = ApplicationType.Server,
            SecurityConfiguration = new SecurityConfiguration
            {
                ApplicationCertificate = new CertificateIdentifier
                {
                    StoreType = "Directory",
                    StorePath = Path.Combine(pkiPath, "own"),
                    SubjectName = "CN=MES Copilot OPC UA Simulator"
                },
                TrustedPeerCertificates = new CertificateTrustList { StoreType = "Directory", StorePath = Path.Combine(pkiPath, "trusted") },
                TrustedIssuerCertificates = new CertificateTrustList { StoreType = "Directory", StorePath = Path.Combine(pkiPath, "issuer") },
                TrustedUserCertificates = new CertificateTrustList { StoreType = "Directory", StorePath = Path.Combine(pkiPath, "trustedUser") },
                UserIssuerCertificates = new CertificateTrustList { StoreType = "Directory", StorePath = Path.Combine(pkiPath, "userIssuer") },
                RejectedCertificateStore = new CertificateTrustList { StoreType = "Directory", StorePath = Path.Combine(pkiPath, "rejected") },
                AutoAcceptUntrustedCertificates = false
            },
            TransportConfigurations = new TransportConfigurationCollection(),
            TransportQuotas = new TransportQuotas { OperationTimeout = 10_000 },
            ServerConfiguration = new ServerConfiguration
            {
                BaseAddresses = new StringCollection { $"opc.tcp://0.0.0.0:{port}" },
                SecurityPolicies = new ServerSecurityPolicyCollection
                {
                    new() { SecurityMode = MessageSecurityMode.None, SecurityPolicyUri = SecurityPolicies.None }
                },
                UserTokenPolicies = new UserTokenPolicyCollection
                {
                    new(UserTokenType.Anonymous)
                }
            }
        };
        await configuration.Validate(ApplicationType.Server);
        ApplicationInstance application = new()
        {
            ApplicationName = configuration.ApplicationName,
            ApplicationType = ApplicationType.Server,
            ApplicationConfiguration = configuration
        };
        await application.CheckApplicationInstanceCertificates(false, 2048, stoppingToken);
        _server = new EquipmentStandardServer();
        await application.Start(_server);
        _logger.LogInformation("OPC UA simulator listening on port {Port}; state node is ns=2;s=Equipment/State.", port);

        using PeriodicTimer timer = new(TimeSpan.FromSeconds(5));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            _server.SetState(EquipmentState.Running.ToString());
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_server is not null)
        {
            _server.Stop();
            _server.Dispose();
        }
        await base.StopAsync(cancellationToken);
    }

    private sealed class EquipmentStandardServer : StandardServer
    {
        private EquipmentNodeManager? _nodeManager;

        protected override MasterNodeManager CreateMasterNodeManager(IServerInternal server, ApplicationConfiguration configuration)
        {
            _nodeManager = new EquipmentNodeManager(server, configuration);
            return new MasterNodeManager(server, configuration, null, _nodeManager);
        }

        public void SetState(string state) => _nodeManager?.SetState(state);
    }

    private sealed class EquipmentNodeManager : CustomNodeManager2
    {
        private BaseDataVariableState<string>? _state;

        public EquipmentNodeManager(IServerInternal server, ApplicationConfiguration configuration)
            : base(server, configuration, "urn:MesCopilot:Equipment")
        {
            SystemContext.NodeIdFactory = this;
        }

        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            FolderState folder = new(null)
            {
                SymbolicName = "Equipment",
                ReferenceTypeId = ReferenceTypes.Organizes,
                TypeDefinitionId = ObjectTypeIds.FolderType,
                NodeId = new NodeId("Equipment", NamespaceIndex),
                BrowseName = new QualifiedName("Equipment", NamespaceIndex),
                DisplayName = "Equipment"
            };
            if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out IList<IReference>? references))
            {
                externalReferences[ObjectIds.ObjectsFolder] = references = new List<IReference>();
            }
            references.Add(new NodeStateReference(ReferenceTypes.Organizes, false, folder.NodeId));
            folder.AddReference(ReferenceTypes.Organizes, true, ObjectIds.ObjectsFolder);

            _state = new BaseDataVariableState<string>(folder)
            {
                SymbolicName = "State",
                ReferenceTypeId = ReferenceTypes.HasComponent,
                TypeDefinitionId = VariableTypeIds.BaseDataVariableType,
                NodeId = new NodeId("Equipment/State", NamespaceIndex),
                BrowseName = new QualifiedName("State", NamespaceIndex),
                DisplayName = "State",
                DataType = DataTypeIds.String,
                ValueRank = ValueRanks.Scalar,
                AccessLevel = AccessLevels.CurrentRead,
                UserAccessLevel = AccessLevels.CurrentRead,
                Value = EquipmentState.Running.ToString(),
                StatusCode = StatusCodes.Good,
                Timestamp = DateTime.UtcNow
            };
            folder.AddChild(_state);
            AddPredefinedNode(SystemContext, folder);
        }

        public void SetState(string state)
        {
            if (_state is null) return;
            lock (Lock)
            {
                _state.Value = state;
                _state.Timestamp = DateTime.UtcNow;
                _state.ClearChangeMasks(SystemContext, false);
            }
        }
    }
}
