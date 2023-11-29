﻿// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.




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
        var el = document.getElementById("response");
        el.value = msg;
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

