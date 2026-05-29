using DeliverTableInfrastructure.Models;

namespace DeliverTableWorker.Services;

public interface ICommissionStatementPdfRenderer
{
    byte[] Render(CommissionStatement statement);
}
