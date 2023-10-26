using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using amorphie.token.core.Models.Profile;
using Microsoft.AspNetCore.Mvc;
using Refit;

namespace amorphie.token.Services.Profile
{
    public interface IProfile
    {
        [Get("/fullprofile/{reference}")]
        Task<ProfileResponse> GetProfile(string reference, [Header("User")] string user, [Header("Channel")] string channel, [Header("Branch")] string branch);
    }
}