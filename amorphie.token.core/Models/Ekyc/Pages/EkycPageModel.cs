namespace amorphie.token.core;

public class EkycPageModel
{
    public string type { get; set; }
    public string image { get; set; }
    public string navText { get; set; }
    public string title { get; set; }
    public bool isInVideoCall {get; set;}

    
    public List<string> subTexts { get; set; }
    public EkycPopUpModel popUp { get; set; }
    public EkycPopUpModel popUpVideoCall { get; set; }
    public List<EkycButtonModel> buttons { get; set; }
}

public class EkycPopUpModel
{
    
    public string image { get; set; }
    public string title { get; set; }
    public List<string> subTexts {get; set;}

    public List<EkycButtonModel> buttons {get; set;}
}

public class EkycButtonModel
{
    public string type { get; set; }
    public int itemNo { get; set; }
    public string text { get; set; }
    public string action { get; set; }
    public string transition {get; set;}

}