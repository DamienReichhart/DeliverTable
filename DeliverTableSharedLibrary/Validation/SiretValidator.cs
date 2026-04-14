namespace DeliverTableSharedLibrary.Validation;

public static class SiretValidator
{
    public static bool IsValid(string? siret)
    {
        if (string.IsNullOrWhiteSpace(siret)) return false;
        if (siret.Length != 14) return false;
        if (!siret.All(char.IsDigit)) return false;

        int sum = 0;
        for (int i = 0; i < 14; i++)
        {
            int d = siret[i] - '0';
            // SIRET Luhn: double the digits at 0-indexed even positions
            bool doubled = (i % 2 == 0);
            int contribution = doubled ? d * 2 : d;
            if (contribution > 9) contribution -= 9;
            sum += contribution;
        }
        return sum % 10 == 0;
    }
}
