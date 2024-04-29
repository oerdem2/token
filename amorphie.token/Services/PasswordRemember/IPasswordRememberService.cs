namespace amorphie.token;

public interface IPasswordRememberService
{
    /// <summary>
    /// control card or cards info using citizenShipNo
    /// </summary>
    /// <param name="citizenshipNo"></param>
    /// <returns>Service response bool</returns>
    Task<ServiceResponse<bool>> HasCardAsync(string citizenshipNo);
}
