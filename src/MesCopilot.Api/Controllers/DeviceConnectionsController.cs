using MesCopilot.Api.Dtos.Devices;
using MesCopilot.Api.HostedServices;
using MesCopilot.Application.Dtos.Devices;
using MesCopilot.Application.Dtos.Realtime;
using MesCopilot.Application.Services.Devices;
using MesCopilot.Infrastructure.Caching;
using MesCopilot.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MesCopilot.Api.Controllers;

[ApiController]
[Route("api/device-connections")]
[Authorize(Policy = "RequireAdmin")]
public sealed class DeviceConnectionsController : ControllerBase
{
    private readonly IDeviceConnectionService _service;
    private readonly DeviceCollectionCoordinator _coordinator;
    private readonly IDeviceStatusCache _cache;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<DeviceConnectionsController> _logger;
    private readonly IHostEnvironment _environment;

    public DeviceConnectionsController(
        IDeviceConnectionService service,
        DeviceCollectionCoordinator coordinator,
        IDeviceStatusCache cache,
        ITenantContext tenantContext,
        ILogger<DeviceConnectionsController> logger,
        IHostEnvironment environment)
    {
        _service = service;
        _coordinator = coordinator;
        _cache = cache;
        _tenantContext = tenantContext;
        _logger = logger;
        _environment = environment;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DeviceConnectionDto>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await _service.GetAllAsync(cancellationToken));

    [HttpPost]
    public async Task<ActionResult<DeviceConnectionDto>> Create(DeviceConnectionRequest request, CancellationToken cancellationToken)
    {
        if (_environment.IsProduction() && request.AllowInsecure)
        {
            return BadRequest(new { code = "INSECURE_DEVICE_TRANSPORT_DISABLED" });
        }
        DeviceConnectionDto result = await _service.CreateAsync(ToInput(request), cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<DeviceConnectionDto>> Update(int id, DeviceConnectionRequest request, CancellationToken cancellationToken)
    {
        if (_environment.IsProduction() && request.AllowInsecure)
        {
            return BadRequest(new { code = "INSECURE_DEVICE_TRANSPORT_DISABLED" });
        }
        DeviceConnectionDto? result = await _service.UpdateAsync(id, ToInput(request), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _coordinator.StopAsync(id);
        return await _service.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();
    }

    [HttpPost("{id:int}/test")]
    public async Task<IActionResult> Test(int id, CancellationToken cancellationToken)
    {
        try
        {
            return await _service.TestAsync(id, cancellationToken) ? Ok(new { connected = true }) : NotFound();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Device connection test {ConnectionId} failed.", id);
            return Ok(new { connected = false });
        }
    }

    [HttpPost("{id:int}/start")]
    public async Task<IActionResult> Start(int id, CancellationToken cancellationToken)
    {
        if (!await _service.SetEnabledAsync(id, true, cancellationToken)) return NotFound();
        await _coordinator.StartAsync(id, _tenantContext.TenantId!);
        return NoContent();
    }

    [HttpPost("{id:int}/stop")]
    public async Task<IActionResult> Stop(int id, CancellationToken cancellationToken)
    {
        if (!await _service.SetEnabledAsync(id, false, cancellationToken)) return NotFound();
        await _coordinator.StopAsync(id);
        return NoContent();
    }

    [HttpGet("{id:int}/realtime")]
    public async Task<ActionResult<EquipmentStatusUpdate>> Realtime(int id, CancellationToken cancellationToken)
    {
        DeviceConnectionRuntime? runtime = await _service.GetRuntimeAsync(id, cancellationToken);
        if (runtime is null) return NotFound();
        EquipmentStatusUpdate? status = await _cache.GetAsync<EquipmentStatusUpdate>(_tenantContext.TenantId!, runtime.EquipmentId, cancellationToken);
        return status is null ? NoContent() : Ok(status);
    }

    private static DeviceConnectionInput ToInput(DeviceConnectionRequest request) => new(
        request.Name, request.EquipmentId, request.Protocol, request.Host, request.Port, request.Endpoint,
        request.Username, request.Password, request.CertificateThumbprint, request.UnitId,
        request.RegisterAddress, request.RegisterCount, request.PollIntervalMilliseconds,
        request.UseTls, request.AllowInsecure);
}
