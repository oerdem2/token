// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

var generateTokenButton = document.getElementById("genToken");
generateTokenButton.addEventListener('click',function(e){
    e.preventDefault();
    fetchToken();
});

var refreshTokenButton = document.getElementById("refToken");
refreshTokenButton.addEventListener('click',function(e){
    e.preventDefault();
    fetchRefreshToken();
});

var callUrlButton = document.getElementById("callUrl");
callUrlButton.addEventListener('click',function(e){
    e.preventDefault();
    callUrl();
});

async function callUrl()
{
    var jwt = document.getElementById("jwt").value;
    const response = await fetch(securedUrl, {
        method: "GET",
        headers: {
            "Content-type": "application/json; charset=UTF-8",
            "Authorization":"Bearer "+jwt
        }
    });
    document.getElementById("statusCode").value = response.status;
}

async function fetchToken()
{
    var req = {
        client_id: document.getElementById("clientId").value,
        client_secret: document.getElementById("clientSecret").value,
        username: document.getElementById("username").value,
        password: document.getElementById("password").value,
        scopes:document.getElementById("scopes").value.split(","),
        grant_type:"password"
    };
    const response = await fetch(tokenUrl, {
        method: "GET"
    });

    var t = await response.json();
    document.getElementById("accessToken").value = t.access_token;
    document.getElementById("refreshToken").value = t.refresh_token;

    document.getElementById("url").value = response.url;
    document.getElementById("request").value = JSON.stringify(req);    
    document.getElementById("response").value = JSON.stringify(t);

    document.getElementById("reqres").style.display = 'block';
}

async function fetchRefreshToken()
{
    var req = {
        refresh_token: document.getElementById("refreshToken").value,
        grant_type:"refresh_token"
    };

    const response = await fetch(tokenUrl, {
        method: "POST",
        body: JSON.stringify(req),
        headers: {
            "Content-type": "application/json; charset=UTF-8"
        }
    });
    var t = await response.json();
    document.getElementById("accessToken").value = t.access_token;
    document.getElementById("refreshToken").value = t.refresh_token;

    document.getElementById("url").value = response.url;
    document.getElementById("request").value = JSON.stringify(req);    
    document.getElementById("response").value = JSON.stringify(t);

    document.getElementById("reqres").style.display = 'block';
}