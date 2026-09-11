using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TicketVoucherSystem.Data.Exceptions;
using TicketVoucherSystem.Data.Models;
using TicketVoucherSystem.Data.Repositories;

namespace TicketVoucherSystemApp.Pages.Admin.Outlets;

public sealed class IndexModel(IOutletData outlets) : PageModel
{
    [BindProperty]
    public OutletInput Input { get; set; } = new();

    public IReadOnlyList<Outlet> Outlets { get; private set; } = [];

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync(int? edit, CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
        var outlet = Outlets.FirstOrDefault(item => item.Id == edit);
        if (outlet is not null)
        {
            Input = new OutletInput
            {
                Id = outlet.Id,
                Name = outlet.Name,
                Code = outlet.Code,
                DisplayOrder = outlet.DisplayOrder
            };
        }
    }

    public async Task<IActionResult> OnPostSaveAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadAsync(cancellationToken);
            return Page();
        }

        try
        {
            await outlets.SaveAsync(Input.Id, Input.Name, Input.Code, Input.DisplayOrder, cancellationToken);
            StatusMessage = Input.Id.HasValue ? "Outlet updated." : "Outlet added.";
            return RedirectToPage();
        }
        catch (VoucherOperationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            await LoadAsync(cancellationToken);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostActiveAsync(int id, bool isActive, CancellationToken cancellationToken)
    {
        await outlets.SetActiveAsync(id, isActive, cancellationToken);
        StatusMessage = isActive ? "Outlet activated." : "Outlet deactivated.";
        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken cancellationToken) =>
        Outlets = await outlets.GetAllAsync(cancellationToken);

    public sealed class OutletInput
    {
        public int? Id { get; set; }

        [Required, StringLength(120)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(20)]
        public string Code { get; set; } = string.Empty;

        [Range(0, 999)]
        public int DisplayOrder { get; set; }
    }
}
