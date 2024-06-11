using System.Text;
using amorphie.token.core;
using amorphie.token.core.Models.Profile;
using amorphie.token.Services.Profile;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Net;


namespace amorphie.token;

public class EkycService : ServiceBase, IEkycService
{
    private readonly IEkycProvider _ekycProvider;
    private readonly IProfileService _profileService;
    private readonly IHttpClientFactory _httpClientFactory;
    public EkycService(ILogger<EkycService> logger,
                              IConfiguration configuration,
                              IEkycProvider ekycProvider,
                              IProfileService profileService,
                              IHttpClientFactory httpClientFactory
                              ) : base(logger, configuration)
    {
        _ekycProvider = ekycProvider;
        _profileService = profileService;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<EkycCreateSessionResultModel> CreateSession(Guid instanceId, string citizenshipNumber, string callType)
    {

        var customerInfoResult = await _profileService.GetCustomerSimpleProfile(citizenshipNumber);
        var optimizedCallType = GetCallType(callType);
        bool isSuccess = true;

        // bool isSuccess = optimizedCallType == EkycCallTypeConstants.Mevduat_ON;

        // dont need create session for mevduat_ON. 
        //Because,The main workflow will send a instanceId that started a session, and we use it.
        // if (optimizedCallType != EkycCallTypeConstants.Mevduat_ON || optimizedCallType != EkycCallTypeConstants.Mevduat_HEPSIBURADA)
        // if(!hasWfId)
        // {
        var request = await SetRegisterRequest(instanceId, citizenshipNumber, customerInfoResult.Response);

        request.CallType = optimizedCallType;
        Dictionary<string, string> data = new Dictionary<string, string>();
        data["Tckn"] = citizenshipNumber;
        data["Başvuru nedeni"] = request.CallType;
        data["Doğum tarihi"] = request.IDRegistration.BirthDate;
        data["Anne adı"] = request.IDRegistration.MotherName;
        data["Baba adı"] = request.IDRegistration.FatherName;
        data["Nüfusa kayıtlı olduğu il"] = request.IDRegistration.RegistrationPlace;

        if (customerInfoResult.Response is not null)
        {

            var phones = customerInfoResult.Response?.data?.phones;

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



        }

        request.Data = Newtonsoft.Json.JsonConvert.SerializeObject(data);
        var response = await _ekycProvider.RegisterAsync(request);
        isSuccess = response.IsSuccessful;
        // }

        var result = new EkycCreateSessionResultModel
        {
            // Name = customerInfoResult?.Response?.data?.profile?.name!,
            // Surname = customerInfoResult?.Response?.data?.profile?.surname!,
            IsSuccessful = isSuccess
        };

        return result;
    }

    public async Task<GetIntegrationInfoModels.Data> GetSessionByIntegrationReferenceAsync(Guid referenceId)
    {
        var response = await _ekycProvider.GetIntegrationInfoAsync(referenceId);
        if (!response.IsSuccessful || !response.Data.Any())
        {
            throw new ArgumentException($"{referenceId} li ekyc integrationa ait session bulunamadı!");
        }

        return response.Data.First();
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



    private async Task<EkycRegisterModels.Request> SetRegisterRequest(Guid instanceId, string citizenshipNumber, SimpleProfileResponse? profileResponse)
    {


        var email = profileResponse?.data?.emails?.FirstOrDefault(e => e.type == "personal");
        var phone = profileResponse?.data?.phones?.FirstOrDefault(e => e.type == "mobile"); // bunu sor type enum olmalı string değil !

        long tckn = Convert.ToInt64(citizenshipNumber);

        // Get kps data
        KpsIdentity kpsResult = await _ekycProvider.GetKpsIdentityInfoAsync(tckn, profileResponse?.data?.profile?.birthDate);

        var result = new EkycRegisterModels.Request
        {
            Type = EkycConstants.Session,
            Reference = instanceId.ToString(),
            Email = email?.address!,
            Phone = $"{phone?.prefix}{phone?.number}",
            IDRegistration = new IdRegistration(kpsResult)

        };

        return result;
    }
    public string GetCallType(string callType) => callType.ToLower() switch
    {
        "ibsifre_brgn" => EkycCallTypeConstants.IBSifre_BRGN,
        "ibsifre_on" => EkycCallTypeConstants.IBSifre_ON,
        "ibsimblock_brgn" => EkycCallTypeConstants.IBSimBlock_BRGN,
        "ibsimblock_on" => EkycCallTypeConstants.IBSimBlock_ON,
        "mevduat_on" => EkycCallTypeConstants.Mevduat_ON,
        "mevduat_brgn" => EkycCallTypeConstants.Mevduat_BRGN,
        "kredi_on" => EkycCallTypeConstants.Kredi_ON,
        "kredi_brgn" => EkycCallTypeConstants.Kredi_BRGN,
        "mevduat_hepsiburada" => EkycCallTypeConstants.Mevduat_HEPSIBURADA,
        _ => EkycCallTypeConstants.None

    };

    public async Task<ServiceResponse<EkycMevduatStatusCheckModels.Response>> CheckCallStatusForMevduat(EkycMevduatStatusCheckModels.Request request)
    {

        var resp = await GetTokenResponse();

        try
        {

            var header = new AuthenticationHeaderValue("Bearer", resp.Response.AccessToken);
            var httpClientFactory = _httpClientFactory.CreateClient("MevduatStatusCheck");
            httpClientFactory.DefaultRequestHeaders.Authorization = header;
            StringContent statusCheckRequest = new(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            var statusCheckResponse = await httpClientFactory.PostAsync("", statusCheckRequest);

            var result = await statusCheckResponse.Content.ReadFromJsonAsync<EkycMevduatStatusCheckModels.Response>();

            Logger.LogInformation($"Ekyc status check service result: CallTransactionType = {result?.CallTransactionsType} ReferenceType: {result?.ReferenceType}");

            if (result.ReferenceType == EkycMevduatStatusCheckModels.EkycProcessRedirectType.Retry)
            {
                request.Counter++;
                return await CheckCallStatusForMevduat(request);
            }


            return new ServiceResponse<EkycMevduatStatusCheckModels.Response> { Response = result };
        }
        catch (Exception ex)
        {
            Logger.LogError("Ekyc Status Check Service Error", ex.Message);
            return new ServiceResponse<EkycMevduatStatusCheckModels.Response> { StatusCode = 500, Detail = ex.Message, Response = null };
        }


    }


    private async Task<ServiceResponse<TokenResponse>> GetTokenResponse()
    {
        using var httpClient = new HttpClient();
        StringContent req = new(JsonSerializer.Serialize(new CardValidationOptions
        {
            ClientId = Configuration["CardValidationClientId"],
            ClientSecret = Configuration["CardValidationClientSecret"],
            GrantType = "client_credentials",
            Scopes = new List<string>() { "retail-customer" }
        }), Encoding.UTF8, "application/json");
        var response = new ServiceResponse<TokenResponse>();
        var httpResponse = await httpClient.PostAsync(Configuration["CardValidationTokenBaseAddress"], req);
        if (!httpResponse.IsSuccessStatusCode)
        {
            response.StatusCode = 500;
            response.Detail = "Couldn't Get Token For Using Status Check Service";
            response.Response = null;
            Logger.LogError($"Get token service status code: 500  {response.Detail} ");
            return response;
        }
        var resp = await httpResponse.Content.ReadFromJsonAsync<TokenResponse>();
        return new ServiceResponse<TokenResponse> { Response = resp };
    }



}
