using ExpenseTracker.Application.DTOs.Expenses;
using ExpenseTracker.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ExpensesController : ControllerBase
{
    private readonly IExpenseService _expenseService;

    public ExpensesController(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ExpenseResponse>>> GetAll(
        int? year,
        int? month,
        Guid? categoryId,
        CancellationToken cancellationToken)
    {
        return Ok(await _expenseService.ListAsync(year, month, categoryId, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ExpenseResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _expenseService.GetAsync(id, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<ExpenseResponse>> Create(ExpenseRequest request, CancellationToken cancellationToken)
    {
        var expense = await _expenseService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = expense.Id }, expense);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ExpenseResponse>> Update(Guid id, ExpenseRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _expenseService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _expenseService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
