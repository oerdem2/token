using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using amorphie.token.core.Models.Profile;
using Microsoft.AspNetCore.Mvc;
using Refit;

namespace amorphie.token.Services.Profile
{
    public interface ISimpleProfile
    {
        [Get("/{reference}/simple-profile")]
        Task<SimpleProfileResponse> GetProfile(string reference, [Header("Authorization")] string password);
    }
}