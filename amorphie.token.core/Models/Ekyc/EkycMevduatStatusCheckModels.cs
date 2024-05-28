using System.ComponentModel.DataAnnotations;

namespace amorphie.token.core;

public class EkycMevduatStatusCheckModels
{
    public enum EkycProcessRedirectType
    {
        /// <summary>
        /// Tekrar Dene
        /// </summary>
        Retry = 10,
        /// <summary>
        /// İşlemi Bitir
        /// </summary>
        Exit = 20,
        /// <summary>
        /// Kurye Akışı Başlat
        /// </summary>
        ToCourier = 30,
        /// <summary>
        /// Devam Et
        /// </summary>
        Continue = 40,
        /// <summary>
        /// Akışı İptal Et
        /// </summary>
        Cancel = 50,
    }

    public class EkycProcess
    {
        public enum ProcessType
        {
            IbPasswordRenew = 10,
            IbUnblockSim = 20
        }
    }


    public class Request
    {
        [Required(ErrorMessage = "CallType boş olamaz")]
        public string CallType { get; set; }
        [Required(ErrorMessage = "Kimlik bilgisi boş olamaz")]
        public long CitizenshipNumber { get; set; }
        [Required(ErrorMessage = "Counter boş olamaz")]
        public int Counter { get; set; }
    }

    public class Response
    {
        public EkycProcessRedirectType ReferenceType { get; set; }
        public EkycPostCallTransactionsType CallTransactionsType { get; set; }
    }


    public enum EkycPostCallTransactionsType
    {
        /// <summary>
        /// İşlem Yapma
        /// </summary>
        None = 0,
        /// <summary>
        /// Anket
        /// </summary>
        Survey = 1,
        /// <summary>
        /// Kredi Kullanımı
        /// </summary>
        CreditUsage = 2,
        /// <summary>
        /// Anket ve Kredi Kullanımı
        /// </summary>
        SurveyAndCreditUsage = 3
    }
}
