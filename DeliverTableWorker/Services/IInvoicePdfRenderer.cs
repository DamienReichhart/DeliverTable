using DeliverTableInfrastructure.Models;

namespace DeliverTableWorker.Services;

public interface IInvoicePdfRenderer
{
    byte[] Render(Invoice invoice);
}
