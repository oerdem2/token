
namespace amorphie.token.Services.Card
{
    public interface ICardHandler
    {
        public Task<ServiceResponse> ValidateCard(string reference,string cardNo,string cvv,string pin);
    }
}