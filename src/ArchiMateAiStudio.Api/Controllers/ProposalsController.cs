using ArchiMateAiStudio.Application.Proposals;
using ArchiMateAiStudio.Domain.Patches;
using Microsoft.AspNetCore.Mvc;

namespace ArchiMateAiStudio.Api.Controllers;

[ApiController]
[Route("api/v1/proposals")]
public sealed class ProposalsController : ControllerBase
{
    private readonly ChangeProposalService _proposals;

    public ProposalsController(ChangeProposalService proposals)
    {
        _proposals = proposals;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? modelId,
        CancellationToken cancellationToken)
    {
        var proposals = await _proposals.ListAsync(modelId, cancellationToken);
        return Ok(new { items = proposals.Select(ToDto) });
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateProposalRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null || request.ModelId == Guid.Empty)
        {
            return BadRequest(new { error = "Provide JSON { \"modelId\": \"...\", \"patch\": { ... } }." });
        }

        if (request.Patch is null)
        {
            return BadRequest(new { error = "Patch is required." });
        }

        var created = await _proposals.CreateAsync(
            request.ModelId,
            request.Patch,
            request.Source,
            cancellationToken);

        if (created is null)
        {
            return NotFound(new { error = $"Model '{request.ModelId}' was not found." });
        }

        return CreatedAtAction(
            nameof(List),
            new { modelId = created.ModelId },
            ToDto(created));
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        var result = await _proposals.ApproveAsync(id, cancellationToken);
        return result.Outcome switch
        {
            ApproveProposalOutcome.Success => Ok(ToDto(result.Proposal!)),
            ApproveProposalOutcome.NotFound => NotFound(new { error = result.Message ?? "Proposal not found." }),
            ApproveProposalOutcome.Conflict => Conflict(new { error = result.Message }),
            ApproveProposalOutcome.InvalidPatch => BadRequest(new { errors = result.Errors }),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, CancellationToken cancellationToken)
    {
        var proposal = await _proposals.RejectAsync(id, cancellationToken);
        if (proposal is null)
        {
            return NotFound();
        }

        if (proposal.Status != Domain.Proposals.ChangeProposalStatus.Rejected)
        {
            return Conflict(new { error = $"Proposal is {proposal.Status}, expected Pending." });
        }

        return Ok(ToDto(proposal));
    }

    private static object ToDto(Domain.Proposals.ChangeProposal proposal) => new
    {
        id = proposal.Id,
        modelId = proposal.ModelId,
        status = proposal.Status.ToString(),
        source = proposal.Source,
        patch = proposal.Patch,
        createdAt = proposal.CreatedAt,
    };

    public sealed class CreateProposalRequest
    {
        public Guid ModelId { get; init; }

        public ModelPatch? Patch { get; init; }

        public string? Source { get; init; }
    }
}
