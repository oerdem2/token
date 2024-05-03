
using System.Collections.Frozen;
using amorphie.token.core;
using amorphie.token.core.Models.Profile;
using amorphie.token.Services.Profile;
using Newtonsoft.Json;

namespace amorphie.token;

public class EkycService : ServiceBase, IEkycService
{
    private readonly IEkycProvider _ekycProvider;
    private readonly IProfileService _profileService;
    public EkycService(ILogger<EkycService> logger,
                              IConfiguration configuration,
                              IEkycProvider ekycProvider,
                              IProfileService profileService) : base(logger, configuration)
    {
        _ekycProvider = ekycProvider;
        _profileService = profileService;
    }

    public async Task<EkycCreateSessionResultModel> CreateSession(Guid instanceId, string reference, EkycProcess.ProcessType processType)
    {


        // var customerProfile = await _profileService.GetCustomerProfile(reference);

        var customerProfile = new ProfileResponse
        {
            fatherName = "Ali",
            motherName = "Ayşe",
            customerName = "Mehmet",
            surname = "Yılmaz",
            birthDate = new DateTime(1985, 12, 25),
            phones = new List<core.Models.Profile.Phone>{
                new core.Models.Profile.Phone { countryCode="+90", prefix="553", number="5778388", type="Mobil"}
            },
            addresses = new List<Address>{
                new Address{ cityCode=33, cityName="Mersin" }
            },
            birthPlace = "Mersin",
            citizenshipNumber = reference,
            emails = new List<Email>{
                new Email{ address="mguven@burgan.com.tr", isVerified=true, type="main" }
            },
            identityNo = reference

        };
        var request = SetRequest(instanceId, customerProfile);

        request.CallType = processType is EkycProcess.ProcessType.IbUnblockSim ? EkycCallTypeConstants.IBSimBlock_ON : EkycCallTypeConstants.IBSifre_ON;
        Dictionary<string, string> data = new Dictionary<string, string>();
        data["Tckn"] = reference;
        data["Başvuru nedeni"] = request.CallType;
        data["Doğum tarihi"] = request.IDRegistration.BirthDate;
        data["Anne adı"] = request.IDRegistration.MotherName;
        data["Baba adı"] = request.IDRegistration.FatherName;
        data["Nüfusa kayıtlı olduğu il"] = request.IDRegistration.RegistrationPlace;
        var phones = customerProfile.phones;

        for (int i = 0; i < phones?.Count; i++)
        {
            if (i == 0)
            {
                data["Telefon numarası"] = $"{phones[i].prefix}{phones[i].number}";
            }
            else
            {
                data["Telefon numarası " + i] = $"{phones[i].prefix}{phones[i].number}";
            }
        }
        request.Data = JsonConvert.SerializeObject(data);
        var response = await _ekycProvider.RegisterAsync(request);

        var result = new EkycCreateSessionResultModel
        {
            Name = customerProfile.customerName,
            Surname = customerProfile.surname,
            CallType = request.CallType,
            ReferenceId = response.ReferenceId,
            IsSuccessful = response.IsSuccessful

        };
        return result;
    }

    public async Task<GetIntegrationInfoModels.Response> GetSessionByIntegrationReferenceAsync(Guid referenceId)
    {
        var response = await _ekycProvider.GetIntegrationInfoAsync(referenceId);
        if (!response.IsSuccessful && !response.Data.Any())
        {
            throw new ArgumentException($"{referenceId} li ekyc integrationa ait session bulunamadı!");
        }

        return response;
    }

    public async Task<GetSessionInfoModels.Response> GetSessionInfoAsync(Guid sessionId, bool loadContent = true, bool loadDetails = true)
    {
        GetSessionInfoModels.Response response = null;
        try
        {
            response = await _ekycProvider.GetSessionInfoAsync(sessionId, loadContent, loadDetails);
        }
        catch (Exception ex)
        {
            Logger.LogError($"{sessionId} session info error. error: {ex.Message}");
            // throw new ArgumentException($"{ sessionId } could not get session info from the enqura service.");
        }
        return response;
    }

   

    private EkycRegisterModels.Request SetRequest(Guid instanceId, ProfileResponse? profileResponse)
    {
        // TODO: Add Kps validation ....

        var email = profileResponse.emails.Where(e => e.isVerified).FirstOrDefault();
        var phone = profileResponse.phones.FirstOrDefault();
        var result = new EkycRegisterModels.Request
        {
            Type = EkycConstants.Session,
            Reference = instanceId.ToString(),
            Email = email.address,
            Phone = $"{phone.prefix}{phone.number}",
            IDRegistration = new IdRegistration(profileResponse)

        };

        return result;
    }
}
