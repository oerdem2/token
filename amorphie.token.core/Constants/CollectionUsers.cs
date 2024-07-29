using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using amorphie.token.core.Models.Collection;
using amorphie.token.core.Models.User;

namespace amorphie.token.core.Constants
{
    public class CollectionUsers
    {
        public static readonly IEnumerable<User> Users = new List<User>()
        {
            {new User{
                Name = "ATILAY",
                Surname = "KARAKABAK",
                CitizenshipNo = "32867085234",
                LoginUser = "U04263",
                Role = Role.Manager,
                DepartmentCode = "2200470"
            }},
            {new User{
                Name = "AYSUN",
                Surname = "DUMAN İLASLAN",
                CitizenshipNo = "36122202930",
                LoginUser = "U04935",
                Role = Role.TeamLeader,
                DepartmentCode = "2200460"
            }},
            {new User{
                Name = "BELMA",
                Surname = "GÖREN",
                CitizenshipNo = "18419774078",
                LoginUser = "U04806",
                Role = Role.Admin,
                DepartmentCode = "UNDEFINED"
            }},
            {new User{
                Name = "BURCU",
                Surname = "ETİ",
                CitizenshipNo = "30583790128",
                LoginUser = "U06231",
                Role = Role.Agent,
                DepartmentCode = "2200491"
            }},
            {new User{
                Name = "EDA",
                Surname = "GÖVLER",
                CitizenshipNo = "28996918338",
                LoginUser = "U05062",
                Role = Role.Agent,
                DepartmentCode = "2200490"
            }},
            {new User{
                Name = "GÜL",
                Surname = "ALPAY",
                CitizenshipNo = "23356670642",
                LoginUser = "U05444",
                Role = Role.Admin,
                DepartmentCode = "2200400"
            }},
            {new User{
                Name = "GÜLÇİN",
                Surname = "GÜL",
                CitizenshipNo = "68692219452",
                LoginUser = "U05203",
                Role = Role.Agent,
                DepartmentCode = "2200490"
            }},
            {new User{
                Name = "İDRİS",
                Surname = "SANCAK",
                CitizenshipNo = "52309628454",
                LoginUser = "U05600",
                Role = Role.Admin,
                DepartmentCode = "2200440"
            }},
            {new User{
                Name = "MERT",
                Surname = "DEMİRARSLAN",
                CitizenshipNo = "14095028054",
                LoginUser = "U05489",
                Role = Role.Admin,
                DepartmentCode = "2200440"
            }},
            {new User{
                Name = "ÖZGÜR",
                Surname = "TEKİNER",
                CitizenshipNo = "47101408106",
                LoginUser = "U04777",
                Role = Role.Manager,
                DepartmentCode = "2200440"
            }},
            {new User{
                Name = "SELDA",
                Surname = "IŞIK",
                CitizenshipNo = "39886225092",
                LoginUser = "U05036",
                Role = Role.Agent,
                DepartmentCode = "2200491"
            }},
            {new User{
                Name = "SELİM",
                Surname = "KABA",
                CitizenshipNo = "36652755920",
                LoginUser = "U04972",
                Role = Role.Agent,
                DepartmentCode = "2200480"
            }},
            {new User{
                Name = "SEVGİ",
                Surname = "KAVAK ÖZNERSES",
                CitizenshipNo = "50941025310",
                LoginUser = "U04811",
                Role = Role.TeamLeader,
                DepartmentCode = "2200420"
            }},
            {new User{
                Name = "TUBA",
                Surname = "ŞENTÜRK",
                CitizenshipNo = "68071040882",
                LoginUser = "U04645",
                Role = Role.Manager,
                DepartmentCode = "2200460"
            }
            },
            {new User{
                Name = "TUĞBA",
                Surname = "IŞIK",
                CitizenshipNo = "13045514586",
                LoginUser = "U04273",
                Role = Role.TeamLeader,
                DepartmentCode = "2200470"
            }
            },
            {new User{
                Name = "UMUT",
                Surname = "İŞ",
                CitizenshipNo = "42190908766",
                LoginUser = "U06007",
                Role = Role.Agent,
                DepartmentCode = "2200490"
            }
            },
            {new User{
                Name = "VEDAT",
                Surname = "YÜKSEL",
                CitizenshipNo = "50494498036",
                LoginUser = "U05179",
                Role = Role.Agent,
                DepartmentCode = "2200480"
            }
            },
            {new User{
                Name = "YASEMİN",
                Surname = "SÜSLÜ",
                CitizenshipNo = "11216422302",
                LoginUser = "U05013",
                Role = Role.Agent,
                DepartmentCode = "2200490"
            }
            }
        };
    }
}