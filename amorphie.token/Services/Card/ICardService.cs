using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using amorphie.token.core.Models.Card;
using amorphie.token.core.Models.Profile;
using Microsoft.AspNetCore.Mvc;
using Refit;

namespace amorphie.token.Services.Card
{
    public interface ICardService
    {
        [Get("/{reference}/{cardNo}/{cvv}/{pin}")]
        Task<ValidateCardResponse> ValidateCard(string reference, string cardNo, string cvv, string pin, [Header("Authorization")] string token);
    }
}