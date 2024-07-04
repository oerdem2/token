using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using amorphie.token.core;
using Refit;

namespace amorphie.token;

public class EkycProvider : BaseEkycProvider, IEkycProvider
{

    private readonly ServiceCaller _serviceCaller;
    private string _kpsUrl;
    public EkycProvider(IHttpClientFactory httpClientFactory,
                        ILogger<BaseEkycProvider> logger,
                        IConfiguration configuration,
                        DaprClient daprClient,
                        ServiceCaller serviceCaller) : base(httpClientFactory, logger, configuration, daprClient)
    {
        _serviceCaller = serviceCaller;
        _kpsUrl = configuration["KpsBaseAddress"];
    }


    #region Kps Processes
    public async Task<KpsIdentity> GetKpsIdentityInfoAsync(long citizenshipNumber, DateTime? BirthDate, int localDayCount = 1)
    {
        var request = new List<KpsServiceReference.BilesikKutukSorgulaKimlikNoSorguKriteri>();
        var criteria = new KpsServiceReference.BilesikKutukSorgulaKimlikNoSorguKriteri
        {
            KimlikNo = citizenshipNumber,
            DogumGun = BirthDate.HasValue ? BirthDate.Value.Day : 0,
            DogumAy = BirthDate.HasValue ? BirthDate.Value.Month : 0,
            DogumYil = BirthDate.HasValue ? BirthDate.Value.Year : 0,
            KimlikNoSpecified = true,
            DogumGunSpecified = true,
            DogumAySpecified = true,
            DogumYilSpecified = true
        };
        request.Add(criteria);
        var paramLog = new
        {
            tckn = citizenshipNumber
        };
        KpsIdentity entity = null;

        if (localDayCount == 0)
        {
            //PROD
            KpsServiceReference.KimlikNoileBilesikKutukSorgulaResponse kkbResult;
            var prodRequest = new KpsServiceReference.KimlikNoileBilesikKutukSorgulaRequest
            {
                list = request.ToArray(),
                aimOfQuery = KpsServiceReference.AimOfQueryType.DigerSistemSorgusu,
            };

            try
            {
                kkbResult = await _serviceCaller.CallAsync<KpsServiceReference.KPSWrapperNewSoap, KpsServiceReference.KimlikNoileBilesikKutukSorgulaResponse>(_kpsUrl, async (proxy) =>
                {
                    return await proxy.KimlikNoileBilesikKutukSorgulaAsync(prodRequest);
                }, false, paramLog);

                entity = GetProductionKpsResponse(kkbResult);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, $"KPS service returned no identity for {citizenshipNumber} birthdate: {criteria.DogumYil}-{criteria.DogumAy}-{criteria.DogumGun} localDayCount: {localDayCount}");

                throw new Exception($"KPS service returned no identity for {citizenshipNumber} birthdate: {criteria.DogumYil}-{criteria.DogumAy}-{criteria.DogumGun} localDayCount: {localDayCount}");
            }



        }
        else
        {
            KpsServiceReference.KimlikNoileBilesikKutukSorgulaLokalResponse kkbResult;
            try
            {
                var localRequest = new KpsServiceReference.KimlikNoileBilesikKutukSorgulaLokalRequest
                {
                    localDayCount = localDayCount,
                    list = request.ToArray(),
                    aimOfQuery = KpsServiceReference.AimOfQueryType.DigerSistemSorgusu
                };

                kkbResult = await _serviceCaller.CallAsync<KpsServiceReference.KPSWrapperNewSoap, KpsServiceReference.KimlikNoileBilesikKutukSorgulaLokalResponse>(_kpsUrl, async (proxy) =>
                {
                    return await proxy.KimlikNoileBilesikKutukSorgulaLokalAsync(localRequest);
                }, false, paramLog);

                entity = GetLocalKpsResponse(kkbResult);

            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, $"KPS service returned no identity for {citizenshipNumber} birthdate: {criteria.DogumYil}-{criteria.DogumAy}-{criteria.DogumGun} localDayCount: {localDayCount}");

                throw new Exception($"KPS service returned no identity for {citizenshipNumber} birthdate: {criteria.DogumYil}-{criteria.DogumAy}-{criteria.DogumGun} localDayCount: {localDayCount}");
            }
        }


        return entity;
    }


    private KpsIdentity GetProductionKpsResponse(KpsServiceReference.KimlikNoileBilesikKutukSorgulaResponse prodResponse)
    {
        if (prodResponse != null && prodResponse.KimlikNoileBilesikKutukSorgulaResult?.Length > 0 && prodResponse?.KimlikNoileBilesikKutukSorgulaResult[0] != null)
        {
            var record = prodResponse.KimlikNoileBilesikKutukSorgulaResult[0];
            return HandleKpsEntity(record);
        }
        return default;
    }

    private KpsIdentity GetLocalKpsResponse(KpsServiceReference.KimlikNoileBilesikKutukSorgulaLokalResponse localResponse)
    {
        if (localResponse != null && localResponse.KimlikNoileBilesikKutukSorgulaLokalResult?.Length > 0 && localResponse?.KimlikNoileBilesikKutukSorgulaLokalResult[0] != null)
        {
            var record = localResponse?.KimlikNoileBilesikKutukSorgulaLokalResult[0];
            return HandleKpsEntity(record);
        }
        return default;
    }

    private KpsIdentity HandleKpsEntity(KpsServiceReference.KPSEntityNew record)
    {
        DateTime? deliveryDate = null;
        if (record.VerilmeTarihYil != 0)
        {
            deliveryDate = new DateTime(record.VerilmeTarihYil, record.VerilmeTarihAy, record.VerilmeTarihGun);
        }
        else if (record.TeslimTarihYil != 0)
        {
            deliveryDate = new DateTime(record.TeslimTarihYil, record.TeslimTarihAy, record.TeslimTarihGun);
        }

        var identity = new KpsIdentity()
        {
            Name = record.Ad,
            MotherName = record.AnneAd,
            MotherCitizenshipNumber = record.AnneKimlikNo,
            FatherName = record.BabaAd,
            FatherCitizenshipNumber = record.BabaKimlikNo,
            ApplicationReasonDescription = record.BasvuruNedenAciklama,
            ApplicationReasonCode = record.BasvuruNedenKod,
            DocumentNumber = record.BelgeNo,
            Office = record.Birim,
            UnclearEndDateReasonDescription = record.BitisTarihiBelirsizOlmaNedenAciklama,
            UnclearEndDateReasonCode = record.BitisTarihiBelirsizOlmaNedenKod,
            VolumeDescription = record.CiltAciklama,
            VolumeCode = record.CiltKod,
            GenderDescription = record.CinsiyetAciklama,
            GenderCode = record.CinsiyetKod,
            IdentificationCertificateRecordNumber = record.CuzdanKayitNo,
            IdentificationCertificateGivingReasonDescription = record.CuzdanVerilmeNedenAciklama,
            IdentificationCertificateGivingReasonCode = record.CuzdanVerilmeNedenKod,
            //ReligionDescription = record.DinAciklama,
            //ReligionCode = record.DinKod,
            BirthDate = (record.DogumTarihYil == 0) ? (DateTime?)null : new DateTime(record.DogumTarihYil, record.DogumTarihAy, record.DogumTarihGun),
            BirthPlace = record.DogumYer,
            BirthPlaceCode = record.DogumYerKod,
            StatusDescription = record.DurumAciklama,
            StatusCode = record.DurumKod,
            IssueDate = (record.DuzenlenmeTarihYil == 0) ? (DateTime?)null : new DateTime(record.DuzenlenmeTarihYil, record.DuzenlenmeTarihAy, record.DuzenlenmeTarihGun),
            IssuerTownName = record.DuzenleyenIlceAciklama,
            IssuerTownCode = record.DuzenleyenIlceKod,
            SpouseCitizenshipNumber = record.EsKimlikNo,
            RealPersonCitizenshipNumber = record.GercekKisiKimlikNo,
            CityName = record.IlAciklama,
            TownName = record.IlceAciklama,
            TownCode = record.IlceKod,
            CityCode = record.IlKod,
            PermissionStartDate = record.IzinBaslangicTarih,
            PermissionEndDate = record.IzinBitisTarih,
            PermitterCityName = record.IzinDuzenlenenIlAciklama,
            PermitterCityCode = record.IzinDuzenlenenIlKod,
            PermissionNumber = record.IzinNo,
            IdentificationCardRecordNumber = record.KartKayitNo,
            RecordNumber = record.KayitNo,
            RegistrationPlaceFamilyRowNumber = record.KayitYeriBilgisiAileSiraNo,
            RegistrationPlacePersonalRowNumber = record.KayitYeriBilgisiBireySiraNo,
            SourceOfficeDescription = record.KaynakBirimAciklama,
            SourceOfficeCode = record.KaynakBirimKod,
            ReceivedCitizenshipNumber = record.KazanilanTCKimlikNo,
            CitizenshipNumber = record.KimlikNo,
            CertificationType = (KpsCertificationType)((int)record.KimlikTip),
            MaritalStatusDescription = record.MedeniHalAciklama,
            MaritalStatusCode = record.MedeniHalKod,
            DeathDate = (record.OlumTarihYil == 0) ? (DateTime?)null : new DateTime(record.OlumTarihYil, record.OlumTarihAy, record.OlumTarihGun),
            PreviousSurname = record.OncekiSoyad,
            SerialNumber = record.SeriNo,
            RowNumber = record.SiraNo,
            ValidTillDate = (record.SonGecerlilikTarihYil == 0) ? (DateTime?)null : new DateTime(record.SonGecerlilikTarihYil, record.SonGecerlilikTarihAy, record.SonGecerlilikTarihGun),
            Surname = record.Soyad,
            IdentificationCardSerialNumber = record.TCKKseriNo,
            DelivererOfficeDescription = record.TeslimEdenBirimAciklama,
            DelivererOfficeCode = record.TeslimEdenBirimKod,
            DeliveryDate = deliveryDate,
            CountryName = record.UlkeAciklama,
            CountryCode = record.UlkeKod,
            NationalityName = record.UyrukAciklama,
            NationalityCode = record.UyrukKod,
            GivingAuthority = record.VerenMakam,
            GivingTownName = record.VerildigiIlceAciklama,
            GivingTownCode = record.VerildigiIlceKod,
            GivingReasonDescription = record.VerilisNedenAciklama,
            GivingReasonCode = record.VerilisNedenKod,
            KpsErrorCode = record.HataKodu,
            KpsErrorDescription = record.HataBilgisi,
        };

        return identity;
    }

    #endregion
    public async Task<GetIntegrationInfoModels.Response> GetIntegrationInfoAsync(Guid reference)
    {
        var resourceUrl = $"Verify/Integration/Get";
        var headers = await GetEnquraHeadersAsync();
        var request = new GetIntegrationInfoModels.Request
        {
            Types = new List<string> { EkycConstants.Session },
            Reference = reference,
            IdentityType = string.Empty,
            IdentityNo = string.Empty,
            SessionUId = string.Empty,
            Statuses = new List<string>()
        };
        var httpClient = _httpClientFactory.CreateClient("Enqura");
        httpClient.DefaultRequestHeaders.Authorization = headers;
        StringContent jsonRequest = new(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        var resp = await httpClient.PostAsync(resourceUrl, jsonRequest); //( resourceUrl, request, headers);
        var response = await resp.Content.ReadFromJsonAsync<GetIntegrationInfoModels.Response>();
        if (response is null || !response.IsSuccessful)
        {
            throw new ServiceException((int)resp.StatusCode, "enqura entegrasyon session get hatası");
        }

        if (response.Data is null)
        {
            throw new ServiceException((int)resp.StatusCode, "enqura entegrasyon session data boş");
        }

        return response;
    }

    public async Task<GetSessionInfoModels.Response> GetSessionInfoAsync(Guid sessionId, bool loadContent = true, bool loadDetails = true)
    {

        var resourceUrl = $"Verify/Session/Get";
        var headers = await GetEnquraHeadersAsync();
        var request = new GetSessionInfoModels.Request
        {
            SessionUId = sessionId.ToString(),
            LoadContent = loadContent,
            LoadDetails = loadDetails
        };

        var httpClient = _httpClientFactory.CreateClient("Enqura");
        
        httpClient.DefaultRequestHeaders.Authorization = headers;
        StringContent jsonRequest = new(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var resp = await httpClient.PostAsync(resourceUrl, jsonRequest); //( resourceUrl, request, headers);
        
        var response = await resp.Content.ReadFromJsonAsync<GetSessionInfoModels.Response>();

        if (response is null || !response.IsSuccessful)
        {
            _logger.LogError($"Enqura Service GetSessionInfo Error: response is null or response is not success!");
            throw new ServiceException((int)resp.StatusCode, "enqura entegrasyon session get hatası");
        }

        if (response.Data is null)
        {
            _logger.LogError($"Enqura Service GetSessionInfo Error: Response's data is null!");
            throw new ServiceException((int)resp.StatusCode, "enqura entegrasyon session data boş");
        }

        return response;

    }

    public async Task<EkycRegisterModels.Response> RegisterAsync(EkycRegisterModels.Request request)
    {
        var resourceUrl = $"Verify/Integration/Add";
        var headers = await GetEnquraHeadersAsync();

        var httpClient = _httpClientFactory.CreateClient("Enqura");
        httpClient.DefaultRequestHeaders.Authorization = headers;
        StringContent jsonRequest = new(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var resp = await httpClient.PostAsync(resourceUrl, jsonRequest); //( resourceUrl, request, headers);
        var response = await resp.Content.ReadFromJsonAsync<EkycRegisterModels.Response>();
        if (response is null || !response.IsSuccessful)
        {
            _logger.LogError($"Enqura Service RegisterAsync Error: session could not create or register");
            throw new ServiceException((int)resp.StatusCode, "enqura entegrasyon session register hatası");
        }
        return response;
    }

    public async Task TestEnqura()
    {
        var header = await GetEnquraHeadersAsync();
    }
}
