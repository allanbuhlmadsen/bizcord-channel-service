using ChannelService.Contracts;
using ChannelService.Service;
using ChannelService.Shared;
using Microsoft.AspNetCore.Mvc;

namespace ChannelService.Controllers;

[ApiController]
[Route("channels")]
public sealed class ChannelsController : ControllerBase
{
    private readonly IChannelAppService _service;

    public ChannelsController(IChannelAppService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ChannelDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var channels = await _service.GetAllAsync(cancellationToken);
        return Ok(channels);
    }

    [HttpGet("{id:guid}", Name = "GetChannelById")]
    public async Task<ActionResult<ChannelDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var channel = await _service.GetByIdAsync(id, cancellationToken);
        return channel is null ? NotFound() : Ok(channel);
    }

    [HttpPost]
    public async Task<ActionResult<ChannelDto>> Create(
        [FromBody] CreateChannelRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await _service.CreateAsync(request, cancellationToken);
            return CreatedAtRoute(
                "GetChannelById",
                new { id = created.Id },
                created);
        }
        catch (InvalidChannelNameException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (ChannelNameAlreadyExistsException exception)
        {
            return Conflict(new { error = exception.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ChannelDto>> Update(
        Guid id,
        [FromBody] UpdateChannelRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _service.UpdateAsync(id, request, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (InvalidChannelNameException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (ChannelNameAlreadyExistsException exception)
        {
            return Conflict(new { error = exception.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var deleted = await _service.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}