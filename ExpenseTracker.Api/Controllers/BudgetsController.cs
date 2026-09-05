using ExpenseTracker.Application.DTOs.Budgets;
using ExpenseTracker.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class BudgetsController : ControllerBase
{
    private readonly IMonthlyBudgetService _budgetService;

    public BudgetsController(IMonthlyBudgetService budgetService)
    {
        _budgetService = budgetService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MonthlyBudgetResponse>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _budgetService.ListAsync(cancellationToken));
    }

    [HttpGet("{year:int}/{month:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<MonthlyBudgetSummaryResponse>> GetByMonth(int year, int month, CancellationToken cancellationToken)
    {
        var summary = await _budgetService.GetAsync(year, month, cancellationToken);

        // No budget for that month yet is an empty result, not a missing resource.
        return summary is null ? NoContent() : Ok(summary);
    }

    [HttpPost]
    public async Task<ActionResult<MonthlyBudgetResponse>> Create(CreateMonthlyBudgetRequest request, CancellationToken cancellationToken)
    {
        var budget = await _budgetService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetByMonth), new { year = budget.Year, month = budget.Month }, budget);
    }

    [HttpPut("{year:int}/{month:int}")]
    public async Task<ActionResult<MonthlyBudgetResponse>> Update(int year, int month, UpdateMonthlyBudgetRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _budgetService.UpdateAsync(year, month, request, cancellationToken));
    }

    [HttpDelete("{year:int}/{month:int}")]
    public async Task<IActionResult> Delete(int year, int month, CancellationToken cancellationToken)
    {
        await _budgetService.DeleteAsync(year, month, cancellationToken);
        return NoContent();
    }
}
