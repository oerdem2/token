// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
var recordId = null;
var securityImageId = null;
var pages = document.getElementsByClassName("login-pages");
for (var i = 0; i < pages.length; i++) {
    if(pages[i].id != "login-page")
        pages[i].style = "display:none"; //second console output
}

function show(id)
{
    var pages = document.getElementsByClassName("login-pages");
for (var i = 0; i < pages.length; i++) {
    if(pages[i].id != id)
        pages[i].style = "display:none"; //second console output
    else
    pages[i].style = "display:block";
}
}
function postTransition(transition,data)
{
    if(recordId == null)
        recordId = crypto.randomUUID();
    
    
    fetch("http://localhost:4900/post-transition/"+recordId+"/"+transition, {
        method: "POST",
        body: JSON.stringify(
            data
        ),
        headers: {
            "Content-type": "application/json; charset=UTF-8",
            "User":crypto.randomUUID(),
            "Behalf-Of-User":crypto.randomUUID()
        }
        }).then((response) => console.log(response));
}

document.getElementById("loginButton").addEventListener('click',function(e){
    e.preventDefault();
    var obj = {};
    obj.EntityData = {};
    obj.EntityData.grant_type = "password";
    obj.EntityData.scopes = ["retail-customer","openId"];
    obj.EntityData.record_id = recordId;
    obj.EntityData.username = document.getElementById("usernameField").value;
    obj.EntityData.password = document.getElementById("passwordField").value;
    obj.EntityData.client_id = "3fa85f64-5717-4562-b3fc-2c963f66afa6";
    obj.EntityData.client_secret = "sercan";
    obj.FormData = null;
    obj.AdditionalData = null;
    obj.GetSignalRHub = true;
    obj.RouteData = null;
    obj.QueryData = null;
    postTransition("amorphie-mobile-login",obj);
    
});

document.getElementById("otpButton").addEventListener('click',function(e){
    e.preventDefault();
    var obj = {};
    obj.EntityData = {};
    obj.EntityData.otpValue = document.getElementById("otpField").value;
    obj.FormData = null;
    obj.AdditionalData = null;
    obj.GetSignalRHub = true;
    obj.RouteData = null;
    obj.QueryData = null;
    postTransition("amorphie-mobile-login-send-otp",obj);
    
});

document.getElementById("resetPasswordButton").addEventListener('click',function(e){
    e.preventDefault();
    var obj = {};
    obj.EntityData = {};
    obj.EntityData.newPassword = document.getElementById("resetPasswordField").value;
    obj.FormData = null;
    obj.AdditionalData = null;
    obj.GetSignalRHub = true;
    obj.RouteData = null;
    obj.QueryData = null;
    postTransition("amorphie-mobile-login-set-new-password",obj);
    
});

// //SignalR
const connection = new signalR.HubConnectionBuilder()
    .withUrl('',{
        skipNegotiation: true,
        transport: signalR.HttpTransportType.WebSockets
    })
    .configureLogging(signalR.LogLevel.Information)
    .build();

async function start() {
    try {
        await connection.start();
        console.log("SignalR Connected.");
    } catch (err) {
        console.log(err);
        setTimeout(start, 5000);
    }
};

connection.onclose(async () => {
    await start();
});

connection.on('SendMessage', (msg)=>{
    console.log("mesaj geldi");
    console.log(msg);
        var parsedMsg = JSON.parse(msg);
        if(parsedMsg.eventInfo == "worker-completed")
        {
            if(parsedMsg.page?.pageRoute?.label == "OTP")
            {
                show("otp-page");
            }

            if(parsedMsg.page?.pageRoute?.label == "SECURITY_IMAGE")
            {
                var html = document.getElementById("security-image-page");
                var appendHtml = "";
                var counter = 1;
                parsedMsg.additionalData.securityImages.forEach((e) => {
                    appendHtml += '<input type="radio" name="securityImageList" id="image'+counter+'" value="'+e.id+'"/><label style="background-image:url('+e.imagePath+')" for="image'+counter+'"></label>'; 
                    counter = counter + 1;
                });
                html.innerHTML = html.innerHTML + appendHtml + '<button type="submit" class="btn btn-primary" id="securityImageButton">Devam Et</button>';
                const radios = document.querySelectorAll('input[name="securityImageList"]');
                radios.forEach(radio => {
                radio.addEventListener('click', function () {
                    securityImageId = radio.value;
                });
                });
                document.getElementById("securityImageButton").addEventListener('click',function(e){
                    e.preventDefault();
                    var obj = {};
                    obj.EntityData = {};
                    obj.EntityData.securityImageId = securityImageId;
                    obj.FormData = null;
                    obj.AdditionalData = null;
                    obj.GetSignalRHub = true;
                    obj.RouteData = null;
                    obj.QueryData = null;
                    postTransition("amorphie-mobile-login-set-new-security-image",obj);
                    
                });
                show("security-image-page");
            }

            if(parsedMsg.page?.pageRoute?.label == "RESET_PASSWORD")
            {
                show("reset-password-page");
            }

            if(parsedMsg.page?.pageRoute?.label == "SECURITY_QUESTION")
            {
                var html = document.getElementById("questions");
                var appendHtml = "";
                var counter = 1;
                parsedMsg.additionalData.securityQuestions.forEach((e) => {
                    appendHtml += '<option value="'+e.id+'">'+e.descriptionEn+'</option>'; 
                    counter = counter + 1;
                });
                html.innerHTML = html.innerHTML + appendHtml;
                document.getElementById("securityQuestionButton").addEventListener('click',function(e){
                    e.preventDefault();
                    var obj = {};
                    obj.EntityData = {};
                    obj.EntityData.securityQuestionId = document.getElementById("questions").value;
                    obj.EntityData.answer = document.getElementById("answerField").value;;
                    obj.FormData = null;
                    obj.AdditionalData = null;
                    obj.GetSignalRHub = true;
                    obj.RouteData = null;
                    obj.QueryData = null;
                    postTransition("amorphie-mobile-login-set-new-security-question",obj);
                    
                });
                show("security-question-page");
            }
        }
    }
  );

  

// Start the connection.
start();
// fetch('/public/StartWorkflow',{
//     method : "POST",
//     headers: {
//         "Content-Type": "application/json",
//     },
//     body:JSON.stringify({
//     transaction_id : transactionId,
//     consent_no : consentNo
//     })
// });

