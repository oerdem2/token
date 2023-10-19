using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace amorphie.token.core.Models.Token
{
    public class TokenInfoDetail
    {
        public Guid AccessTokenId { get; set; }
        public Guid RefreshTokenId { get; set; }
        public Guid IdTokenId { get; set; }
        public int AccessTokenDuration { get; set; }
        public int RefreshTokenDuration { get; set; }
        public List<TokenInfo> TokenList { get; set; } = new();
    }
}